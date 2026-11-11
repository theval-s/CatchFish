#include "Routes.h"

#include <algorithm>

#include "ApiError.h"
#include "Http.h"

using nlohmann::json;

namespace
{
    // Takes as long for a wrong key as for a right one, so the key can't be guessed letter by letter.
    bool SameKey(const std::string& given, const std::string& expected)
    {
        if (given.size() != expected.size())
        {
            return false;
        }

        unsigned char difference = 0;
        for (std::size_t i = 0; i < given.size(); i++)
        {
            difference |= static_cast<unsigned char>(given[i] ^ expected[i]);
        }

        return difference == 0;
    }

    int IntParam(const httplib::Request& request, const std::string& name, int fallback, int max)
    {
        try
        {
            const auto value = request.has_param(name) ? std::stoi(request.get_param_value(name)) : fallback;
            return std::clamp(value, 1, max);
        }
        catch (const std::exception&)
        {
            throw ApiError(400, "bad_request", name + " must be a number");
        }
    }

    using Handler = std::function<void(const httplib::Request&, httplib::Response&)>;
}

void AddDevRoutes(httplib::Server& server, App& app)
{
    // Every dev route: check the key, then run under the lock.
    const auto dev = [&app](Handler handler)
    {
        return [&app, handler](const httplib::Request& request, httplib::Response& response)
        {
            Respond(response, [&]
            {
                if (app.devKey.empty())
                {
                    throw ApiError(404, "not_found", "the dev API is off: start the server with CATCHFISH_DEV_KEY set");
                }
                if (!SameKey(request.get_header_value("X-Dev-Key"), app.devKey))
                {
                    throw ApiError(403, "forbidden", "wrong or missing X-Dev-Key");
                }

                std::lock_guard<std::mutex> lock(app.lock);
                handler(request, response);
            });
        };
    };

    // ---------- Live config: every change is checked whole and bumps the version ----------

    server.Get("/dev/config", dev([&app](const httplib::Request&, httplib::Response& response)
    {
        SendJson(response, app.content.Json());
    }));

    server.Put("/dev/config", dev([&app](const httplib::Request& request, httplib::Response& response)
    {
        app.content.Replace(JsonBody(request), Clock::now());
        SendJson(response, app.content.Json());
    }));

    server.Put("/dev/config/daily", dev([&app](const httplib::Request& request, httplib::Response& response)
    {
        auto config = app.content.Json();
        config["dailyRewards"] = JsonBody(request);
        app.content.Replace(config, Clock::now());
        SendJson(response, app.content.Json());
    }));

    // Adds the quest, or replaces the one with this id.
    server.Put(R"(/dev/config/quests/(\d+))", dev([&app](const httplib::Request& request, httplib::Response& response)
    {
        const auto id = PathId(request);
        auto quest = JsonBody(request);
        quest["id"] = id;

        auto config = app.content.Json();
        auto& quests = config["quests"];
        const auto existing = std::find_if(quests.begin(), quests.end(),
            [id](const json& each) { return each.value("id", -1LL) == id; });
        if (existing != quests.end())
        {
            *existing = quest;
        }
        else
        {
            quests.push_back(quest);
        }

        app.content.Replace(config, Clock::now());
        SendJson(response, app.content.Json());
    }));

    // Refused while startingQuests or another quest's unlocksId still points at it.
    server.Delete(R"(/dev/config/quests/(\d+))", dev([&app](const httplib::Request& request, httplib::Response& response)
    {
        const auto id = PathId(request);
        auto config = app.content.Json();
        auto& quests = config["quests"];
        const auto removed = std::remove_if(quests.begin(), quests.end(),
            [id](const json& each) { return each.value("id", -1LL) == id; });
        if (removed == quests.end())
        {
            throw ApiError(404, "not_found", "no quest " + std::to_string(id));
        }

        quests.erase(removed, quests.end());
        app.content.Replace(config, Clock::now());
        SendJson(response, app.content.Json());
    }));

    // ---------- Players ----------

    server.Get("/dev/players", dev([&app](const httplib::Request& request, httplib::Response& response)
    {
        auto list = json::array();
        for (const auto& row : app.storage.ListPlayers(IntParam(request, "limit", 50, 500)))
        {
            list.push_back({
                {"playerId", row.id},
                {"createdAt", row.createdAt},
                {"updatedAt", row.updatedAt},
                {"runsCompleted", row.profile.player.runsCompleted},
                {"dirtyRubles", row.profile.player.dirtyRubles},
            });
        }

        SendJson(response, list);
    }));

    server.Get(R"(/dev/players/(\d+))", dev([&app](const httplib::Request& request, httplib::Response& response)
    {
        const auto profile = app.storage.LoadProfile(PathId(request));
        if (!profile)
        {
            throw ApiError(404, "not_found", "no such player");
        }

        SendJson(response, *profile);
    }));

    // Replaces the whole profile: GET it, edit it, PUT it back.
    server.Put(R"(/dev/players/(\d+))", dev([&app](const httplib::Request& request, httplib::Response& response)
    {
        auto profile = JsonBody(request).get<GameState>();
        profile.savedAt = FormatTime(Clock::now());
        if (!app.storage.SaveProfile(PathId(request), profile))
        {
            throw ApiError(404, "not_found", "no such player");
        }

        SendJson(response, profile);
    }));
}
