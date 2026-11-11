#pragma once

#include <optional>
#include <string>

#include <nlohmann/json.hpp>

#include "GameRules.h"
#include "Storage.h"

/// The live game config: the JSON clients get from /config and the rules built from it. It lives in
/// the database so the dev API can change it without a restart; each change bumps the version.
class Content
{
public:
    /// Loads the config from the database. An empty database is seeded from seedPath.
    Content(Storage& storage, const std::string& seedPath, Clock::time_point now);

    const GameRules& Rules() const;
    int Version() const;

    /// The config as stored, with "version" added.
    nlohmann::json Json() const;

    /// Checks the whole config (GameRules) and saves it as the next version.
    /// Throws ApiError invalid_config and keeps the current one if it isn't valid.
    void Replace(nlohmann::json config, Clock::time_point now);

private:
    Storage& _storage;
    nlohmann::json _config;
    std::optional<GameRules> _rules;
    int _version = 0;
};
