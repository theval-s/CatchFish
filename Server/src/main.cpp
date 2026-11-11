#include <csignal>
#include <cstdio>
#include <cstdlib>
#include <exception>
#include <iostream>
#include <stdexcept>
#include <string>

#include <httplib.h>

#include "App.h"
#include "Http.h"
#include "Routes.h"

// The CatchFish backend: HTTP + JSON, API in Server/README.md.

namespace
{
    struct Options
    {
        // Only this machine by default: on the internet the server sits behind a TLS proxy (Caddy).
        std::string host = "127.0.0.1";
        int port = 8080;
        // Only read when the database has no config yet; after that the dev API changes it.
        std::string seedConfigPath = CATCHFISH_DEFAULT_CONFIG;
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
                options.seedConfigPath = value;
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

    // From the environment rather than an option, so it doesn't show up in `ps` or shell history.
    std::string ReadDevKey()
    {
        const char* key = std::getenv("CATCHFISH_DEV_KEY");
        if (key == nullptr || *key == '\0')
        {
            return "";
        }
        if (std::string(key).size() < 16)
        {
            throw std::invalid_argument("CATCHFISH_DEV_KEY must be at least 16 characters");
        }

        return key;
    }

    void ExitNow(int)
    {
        std::_Exit(0);
    }
}

int Run(int argc, char** argv)
{
    const auto options = ReadOptions(argc, argv);
    App app(options.dbPath, options.seedConfigPath, ReadDevKey());

    httplib::Server server;

    // Every request body is a few ids or one config; anything bigger isn't ours.
    server.set_payload_max_length(256 * 1024);

    AddGameRoutes(server, app);
    AddDevRoutes(server, app);

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

    std::printf("CatchFish server on http://%s:%d\n  database: %s\n  config: version %d\n  dev API: %s\n",
        options.host.c_str(), options.port, options.dbPath.c_str(), app.content.Version(),
        app.devKey.empty() ? "off (set CATCHFISH_DEV_KEY to turn it on)" : "on");
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
    // Startup problems (bad options, a missing or broken seed config, an unopenable database) end up here.
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
