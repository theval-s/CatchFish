#include "Routes.h"

#include "ApiError.h"
#include "Http.h"

using nlohmann::json;

namespace
{
    json SessionJson(const Accounts::Session& session)
    {
        return {{"playerId", session.playerId}, {"sessionToken", session.token}, {"expiresAt", session.expiresAt}};
    }

    using Change = std::function<void(GameState&, const json& body, const GameRules&, Clock::time_point now)>;

    std::string HtmlEscape(const std::string& text)
    {
        std::string escaped;
        for (const auto c : text)
        {
            switch (c)
            {
                case '<': escaped += "&lt;"; break;
                case '>': escaped += "&gt;"; break;
                case '&': escaped += "&amp;"; break;
                case '"': escaped += "&quot;"; break;
                default: escaped += c;
            }
        }

        return escaped;
    }

    // What the player's browser shows after Google sends it back.
    void SendPage(httplib::Response& response, int status, const std::string& title, const std::string& message)
    {
        response.status = status;
        response.set_content(
            "<!doctype html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\">"
            "<title>CatchFish</title></head><body style=\"font-family:sans-serif;text-align:center;padding:3em 1em\">"
            "<h1>" + HtmlEscape(title) + "</h1><p>" + HtmlEscape(message) + "</p></body></html>",
            "text/html; charset=utf-8");
    }
}

void AddGameRoutes(httplib::Server& server, App& app)
{
    // A profile action: find the player by session, bring their profile up to date with the config,
    // change it, save it, send it back. If the change throws, nothing is saved.
    const auto profileAction = [&app](Change change)
    {
        return [&app, change](const httplib::Request& request, httplib::Response& response)
        {
            Respond(response, [&]
            {
                const auto body = JsonBody(request);
                const auto token = BearerToken(request);
                const auto now = Clock::now();

                std::lock_guard<std::mutex> lock(app.lock);
                const auto playerId = app.accounts.Authenticate(token, now);
                auto state = app.storage.LoadProfile(playerId);
                if (!state)
                {
                    throw ApiError(401, "unauthorized", "this session's player is gone");
                }

                const auto& rules = app.content.Rules();
                const auto refreshed = rules.Refresh(*state, now);
                if (change)
                {
                    change(*state, body, rules, now);
                }

                if (change || refreshed)
                {
                    state->savedAt = FormatTime(now);
                    app.storage.SaveProfile(playerId, *state);
                }

                SendJson(response, *state);
            });
        };
    };

    // ---------- Config and time: no session needed ----------

    server.Get("/config", [&app](const httplib::Request&, httplib::Response& response)
    {
        std::lock_guard<std::mutex> lock(app.lock);
        SendJson(response, app.content.Json());
    });

    server.Get("/time", [](const httplib::Request&, httplib::Response& response)
    {
        SendJson(response, {{"now", FormatTime(Clock::now())}});
    });

    // ---------- Sign-in ----------

    server.Post("/auth/guest", [&app](const httplib::Request&, httplib::Response& response)
    {
        Respond(response, [&]
        {
            const auto now = Clock::now();
            std::lock_guard<std::mutex> lock(app.lock);
            const auto guest = app.accounts.CreateGuest(app.content.Rules().CreateProfile(now), now);

            auto body = SessionJson(guest.session);
            body["guestSecret"] = guest.guestSecret;
            SendJson(response, body);
        });
    });

    server.Post("/auth/guest/login", [&app](const httplib::Request& request, httplib::Response& response)
    {
        Respond(response, [&]
        {
            const auto secret = JsonBody(request).at("guestSecret").get<std::string>();
            std::lock_guard<std::mutex> lock(app.lock);
            SendJson(response, SessionJson(app.accounts.SignInGuest(secret, Clock::now())));
        });
    });

    // ---------- Google sign-in: begin, Google's callback, finish (see GoogleAuth.h) ----------

    server.Post("/auth/google/begin", [&app](const httplib::Request& request, httplib::Response& response)
    {
        Respond(response, [&]
        {
            if (!app.google.Enabled())
            {
                throw ApiError(503, "google_off", "this server has no Google client configured");
            }

            const auto secret = JsonBody(request).at("loginSecret").get<std::string>();
            const auto now = Clock::now();
            std::lock_guard<std::mutex> lock(app.lock);
            // With a session, the Google account is added to that player.
            const auto linkPlayerId = request.has_header("Authorization")
                ? app.accounts.Authenticate(BearerToken(request), now) : 0;
            const auto loginId = app.accounts.BeginGoogle(secret, linkPlayerId, now);
            SendJson(response, {{"loginId", loginId}, {"url", app.google.AuthorizationUrl(loginId)}});
        });
    });

    server.Get("/auth/google/callback", [&app](const httplib::Request& request, httplib::Response& response)
    {
        const auto now = Clock::now();
        try
        {
            Storage::GoogleLogin login;
            {
                std::lock_guard<std::mutex> lock(app.lock);
                login = app.accounts.PendingGoogleLogin(request.get_param_value("state"), now);
            }

            const auto code = request.get_param_value("code");
            if (code.empty())
            {
                const auto reason = request.get_param_value("error");
                std::lock_guard<std::mutex> lock(app.lock);
                app.accounts.FailGoogle(login, reason.empty() ? "Google sent no code" : "Google: " + reason);
                SendPage(response, 400, "Not signed in", "Google sign-in was cancelled. You can go back to CatchFish.");
                return;
            }

            // The call to Google can take a while: other requests go on meanwhile.
            GoogleAuth::Account account;
            try
            {
                account = app.google.ExchangeCode(code, now);
            }
            catch (const ApiError& error)
            {
                std::lock_guard<std::mutex> lock(app.lock);
                app.accounts.FailGoogle(login, error.what());
                throw;
            }

            std::lock_guard<std::mutex> lock(app.lock);
            const auto done = app.accounts.CompleteGoogle(login, account.subject, account.email,
                app.content.Rules().CreateProfile(now), now);
            if (done.status == "failed")
            {
                SendPage(response, 400, "Not signed in", done.error + ". Go back to CatchFish.");
            }
            else if (done.status == "taken")
            {
                SendPage(response, 200, "Account in use", account.email
                    + " already has a CatchFish player. Go back to CatchFish to choose which one to keep playing.");
            }
            else
            {
                SendPage(response, 200, "Signed in", "Signed in as " + account.email + ". You can go back to CatchFish.");
            }
        }
        catch (const std::exception& error)
        {
            SendPage(response, 400, "Not signed in", std::string(error.what()) + ". Go back to CatchFish and try again.");
        }
    });

    server.Post("/auth/google/finish", [&app](const httplib::Request& request, httplib::Response& response)
    {
        Respond(response, [&]
        {
            const auto body = JsonBody(request);
            const auto loginId = body.at("loginId").get<std::string>();
            const auto secret = body.at("loginSecret").get<std::string>();
            const auto switchPlayer = body.value("switchPlayer", false);

            std::lock_guard<std::mutex> lock(app.lock);
            const auto result = app.accounts.FinishGoogle(loginId, secret, switchPlayer, Clock::now());
            if (!result)
            {
                SendJson(response, {{"status", "pending"}});
                return;
            }

            auto reply = SessionJson(result->session);
            reply["status"] = "done";
            reply["email"] = result->email;
            reply["guestSecret"] = result->guestSecret.empty() ? json(nullptr) : json(result->guestSecret);
            SendJson(response, reply);
        });
    });

    server.Get("/account", [&app](const httplib::Request& request, httplib::Response& response)
    {
        Respond(response, [&]
        {
            const auto token = BearerToken(request);
            std::lock_guard<std::mutex> lock(app.lock);
            const auto playerId = app.accounts.Authenticate(token, Clock::now());
            const auto email = app.accounts.GoogleEmail(playerId);
            SendJson(response, {{"playerId", playerId}, {"google", email.empty() ? json(nullptr) : json(email)}});
        });
    });

    server.Post("/auth/logout", [&app](const httplib::Request& request, httplib::Response& response)
    {
        Respond(response, [&]
        {
            const auto token = BearerToken(request);
            std::lock_guard<std::mutex> lock(app.lock);
            app.accounts.SignOut(token);
            SendJson(response, json::object());
        });
    });

    // ---------- The profile and player actions ----------

    server.Get("/profile", profileAction(nullptr));

    server.Post("/gear/buy", profileAction([](GameState& state, const json& body, const GameRules& rules, Clock::time_point)
    {
        rules.BuyGear(state, body.at("gearId").get<std::string>());
    }));

    server.Post("/quests/complete", profileAction([](GameState& state, const json& body, const GameRules& rules, Clock::time_point)
    {
        rules.CompleteQuest(state, body.at("questId").get<int>());
    }));

    server.Post("/runs/start", profileAction([](GameState& state, const json&, const GameRules& rules, Clock::time_point now)
    {
        rules.StartRun(state, now);
    }));

    server.Post("/runs/complete", profileAction([](GameState& state, const json& body, const GameRules& rules, Clock::time_point now)
    {
        rules.CompleteRun(state, body.get<RunResult>(), now);
    }));

    server.Post("/daily/claim", profileAction([](GameState& state, const json&, const GameRules& rules, Clock::time_point now)
    {
        rules.ClaimDailyReward(state, now);
    }));

    server.Post("/energy/refill", profileAction([](GameState& state, const json&, const GameRules& rules, Clock::time_point now)
    {
        rules.RefillEnergy(state, now);
    }));

    server.Post("/school/hire", profileAction([](GameState& state, const json& body, const GameRules& rules, Clock::time_point)
    {
        rules.HireTutor(state, body.at("classId").get<std::string>());
    }));

    server.Post("/school/student", profileAction([](GameState& state, const json& body, const GameRules& rules, Clock::time_point)
    {
        rules.AddStudent(state, body.at("classId").get<std::string>());
    }));

    server.Post("/home/upgrade", profileAction([](GameState& state, const json& body, const GameRules& rules, Clock::time_point)
    {
        rules.UpgradeHome(state, body.at("objectId").get<std::string>(), body.value("useGems", false));
    }));

    server.Post("/home/levelup", profileAction([](GameState& state, const json& body, const GameRules& rules, Clock::time_point)
    {
        rules.LevelUpHome(state, body.at("objectId").get<std::string>());
    }));
}
