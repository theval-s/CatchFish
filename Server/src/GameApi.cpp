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
                const auto refreshed = rules.Refresh(*state);
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
}
