#include <csignal>
#include <cstdio>
#include <cstdlib>
#include <exception>
#include <fstream>
#include <functional>
#include <iostream>
#include <mutex>
#include <sstream>
#include <string>

#include <httplib.h>
#include <nlohmann/json.hpp>

#include "ApiError.h"
#include "GameRules.h"
#include "Random.h"
#include "Storage.h"
#include "TimeFormat.h"

// The CatchFish backend: HTTP + JSON, API in Server/README.md.

using nlohmann::json;

namespace
{
    struct Options
    {
        // Only this machine by default: on the internet the server sits behind a TLS proxy (Caddy).
        std::string host = "127.0.0.1";
        int port = 8080;
        std::string configPath = CATCHFISH_DEFAULT_CONFIG;
        std::string dbPath = CATCHFISH_DEFAULT_DB;
    };

    Options ReadOptions(int argc, char** argv)
    {
        Options options;
        for (int i = 1; i + 1 < argc; i += 2)
        {
            const std::string name = argv[i];
            const std::string value = argv[i + 1];
            if (name == "--host")
            {
                options.host = value;
            }
            else if (name == "--port")
            {
                options.port = std::stoi(value);
            }
            else if (name == "--config")
            {
                options.configPath = value;
            }
            else if (name == "--db")
            {
                options.dbPath = value;
            }
            else
            {
                throw std::invalid_argument("Unknown option " + name);
            }
        }

        return options;
    }

    json ReadJsonFile(const std::string& path)
    {
        std::ifstream file(path);
        if (!file)
        {
            throw std::runtime_error("Can't read " + path);
        }

        std::stringstream text;
        text << file.rdbuf();
        return json::parse(text.str());
    }

    void SendJson(httplib::Response& response, const json& body, int status = 200)
    {
        response.status = status;
        response.set_content(body.dump(), "application/json");
    }

    void SendError(httplib::Response& response, int status, const std::string& code, const std::string& message)
    {
        SendJson(response, {{"error", code}, {"message", message}}, status);
    }

    /// Runs a handler and turns whatever it throws into an error response.
    void Respond(httplib::Response& response, const std::function<void()>& handler)
    {
        try
        {
            handler();
        }
        catch (const ApiError& error)
        {
            SendError(response, error.status, error.code, error.what());
        }
        catch (const json::exception& error)
        {
            SendError(response, 400, "bad_request", error.what());
        }
        catch (const std::exception& error)
        {
            std::cerr << "Internal error: " << error.what() << std::endl;
            SendError(response, 500, "internal_error", error.what());
        }
    }

    void ExitNow(int)
    {
        std::_Exit(0);
    }

    std::string BearerToken(const httplib::Request& request)
    {
        const std::string prefix = "Bearer ";
        const auto header = request.get_header_value("Authorization");
        if (header.compare(0, prefix.size(), prefix) != 0 || header.size() == prefix.size())
        {
            throw ApiError(401, "unauthorized", "no token");
        }

        return header.substr(prefix.size());
    }
}

int Run(int argc, char** argv)
{
    const auto options = ReadOptions(argc, argv);
    const auto rawConfig = ReadJsonFile(options.configPath);
    const GameRules rules(rawConfig.get<GameConfig>());
    Storage storage(options.dbPath);

    // One request at a time reads and writes profiles. Simple, can't lose an update,
    // and fast enough for a handful of players.
    std::mutex gameLock;

    // A profile action: find the player by token, change their profile, save it, send it back.
    // If the change throws, nothing is saved.
    const auto profileAction = [&](std::function<void(GameState&, const json&)> change)
    {
        return [&, change](const httplib::Request& request, httplib::Response& response)
        {
            Respond(response, [&]
            {
                const auto body = request.body.empty() ? json::object() : json::parse(request.body);
                const auto token = BearerToken(request);

                std::lock_guard<std::mutex> lock(gameLock);
                auto state = storage.LoadProfile(token);
                if (!state)
                {
                    throw ApiError(401, "unauthorized", "unknown token");
                }

                change(*state, body);
                state->savedAt = FormatTime(Clock::now());
                storage.SaveProfile(token, *state);
                SendJson(response, *state);
            });
        };
    };

    httplib::Server server;

    // Every request body is a few ids; anything bigger isn't from the game.
    server.set_payload_max_length(64 * 1024);

    server.Get("/config", [&](const httplib::Request&, httplib::Response& response)
    {
        SendJson(response, rawConfig);
    });

    server.Get("/time", [&](const httplib::Request&, httplib::Response& response)
    {
        SendJson(response, {{"now", FormatTime(Clock::now())}});
    });

    server.Post("/auth/guest", [&](const httplib::Request&, httplib::Response& response)
    {
        Respond(response, [&]
        {
            const auto token = RandomHex(16);
            std::lock_guard<std::mutex> lock(gameLock);
            storage.CreatePlayer(token, rules.CreateProfile(Clock::now()));
            SendJson(response, {{"token", token}});
        });
    });

    server.Get("/profile", [&](const httplib::Request& request, httplib::Response& response)
    {
        Respond(response, [&]
        {
            const auto token = BearerToken(request);
            std::lock_guard<std::mutex> lock(gameLock);
            const auto state = storage.LoadProfile(token);
            if (!state)
            {
                throw ApiError(401, "unauthorized", "unknown token");
            }

            SendJson(response, *state);
        });
    });

    server.Post("/gear/buy", profileAction([&](GameState& state, const json& body)
    {
        rules.BuyGear(state, body.at("gearId").get<std::string>());
    }));

    server.Post("/quests/complete", profileAction([&](GameState& state, const json& body)
    {
        rules.CompleteQuest(state, body.at("questId").get<int>());
    }));

    server.Post("/runs/start", profileAction([&](GameState& state, const json&)
    {
        rules.StartRun(state, Clock::now());
    }));

    server.Post("/runs/complete", profileAction([&](GameState& state, const json& body)
    {
        rules.CompleteRun(state, body.get<RunResult>(), Clock::now());
    }));

    // Errors httplib produces itself (unknown path, body too large) get the same format as ours.
    server.set_error_handler([](const httplib::Request& request, httplib::Response& response)
    {
        if (response.body.empty())
        {
            const auto code = response.status == 404 ? "not_found" : "http_" + std::to_string(response.status);
            SendError(response, response.status, code, request.method + " " + request.path);
        }
    });

    server.set_logger([](const httplib::Request& request, const httplib::Response& response)
    {
        std::printf("%s %s -> %d\n", request.method.c_str(), request.path.c_str(), response.status);
        std::fflush(stdout);
    });

    // In a container the server is process 1, which ignores SIGTERM and SIGINT without a handler,
    // so `docker stop` would wait and then kill it. Exit right away instead: SQLite keeps the
    // database consistent even if a write was in progress.
    std::signal(SIGTERM, ExitNow);
    std::signal(SIGINT, ExitNow);

    std::printf("CatchFish server on http://%s:%d\n  config: %s\n  database: %s\n",
        options.host.c_str(), options.port, options.configPath.c_str(), options.dbPath.c_str());
    std::fflush(stdout);

    if (!server.listen(options.host, options.port))
    {
        std::cerr << "Can't listen on " << options.host << ":" << options.port << std::endl;
        return 1;
    }

    return 0;
}

int main(int argc, char** argv)
{
    // Startup problems (bad options, a missing or broken config, an unopenable database) end up here.
    try
    {
        return Run(argc, argv);
    }
    catch (const std::exception& error)
    {
        std::cerr << error.what() << std::endl;
        return 1;
    }
}
