#include "Content.h"

#include <fstream>
#include <sstream>
#include <stdexcept>

namespace
{
    nlohmann::json ReadJsonFile(const std::string& path)
    {
        std::ifstream file(path);
        if (!file)
        {
            throw std::runtime_error("Can't read " + path);
        }

        std::stringstream text;
        text << file.rdbuf();
        return nlohmann::json::parse(text.str());
    }
}

Content::Content(Storage& storage, const std::string& seedPath, Clock::time_point now)
    : _storage(storage)
{
    const auto stored = _storage.LoadContent();
    if (!stored)
    {
        Replace(ReadJsonFile(seedPath), now);
        return;
    }

    _config = nlohmann::json::parse(stored->config);
    _rules.emplace(_config.get<GameConfig>());
    _version = stored->version;
}

const GameRules& Content::Rules() const
{
    return *_rules;
}

int Content::Version() const
{
    return _version;
}

nlohmann::json Content::Json() const
{
    auto json = _config;
    json["version"] = _version;
    return json;
}

void Content::Replace(nlohmann::json config, Clock::time_point now)
{
    // The version is the server's to count, whatever the body says.
    config.erase("version");

    // Built before anything is stored: a config the rules refuse never goes live.
    GameRules rules(config.get<GameConfig>());
    _storage.SaveContent(config.dump(), _version + 1, FormatTime(now));

    _rules.emplace(std::move(rules));
    _config = std::move(config);
    _version++;
}
