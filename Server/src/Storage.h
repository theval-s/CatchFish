#pragma once

#include <optional>
#include <string>

#include "Model.h"

struct sqlite3;

/// Players and their profiles in SQLite: one row per player, the profile stored as JSON.
/// Not thread-safe; main.cpp calls it under one lock.
class Storage
{
public:
    explicit Storage(const std::string& path);
    ~Storage();

    Storage(const Storage&) = delete;
    Storage& operator=(const Storage&) = delete;

    void CreatePlayer(const std::string& token, const GameState& state);

    /// Empty if no player has this token.
    std::optional<GameState> LoadProfile(const std::string& token);

    void SaveProfile(const std::string& token, const GameState& state);

private:
    sqlite3* _db = nullptr;
};
