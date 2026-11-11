#pragma once

#include <cstdint>
#include <optional>
#include <string>
#include <vector>

#include "Model.h"

struct sqlite3;

/// Everything the server keeps, in SQLite: players with their profiles (as JSON), the ways each player
/// signs in (identities), sessions, and the live game config. Not thread-safe; callers hold App::lock.
class Storage
{
public:
    struct Session
    {
        std::int64_t playerId;
        std::string expiresAt;
    };

    struct PlayerRow
    {
        std::int64_t id;
        std::string createdAt;
        std::string updatedAt;
        GameState profile;
    };

    struct ContentRow
    {
        std::string config;
        int version;
    };

    /// BEGIN now, ROLLBACK when it goes out of scope unless Commit() was called.
    class Transaction
    {
    public:
        explicit Transaction(Storage& storage);
        ~Transaction();

        Transaction(const Transaction&) = delete;
        Transaction& operator=(const Transaction&) = delete;

        void Commit();

    private:
        Storage& _storage;
        bool _done = false;
    };

    /// Opens the database and brings its schema up to date (see Migrate in Storage.cpp).
    explicit Storage(const std::string& path);
    ~Storage();

    Storage(const Storage&) = delete;
    Storage& operator=(const Storage&) = delete;

    std::int64_t CreatePlayer(const GameState& profile);
    std::optional<GameState> LoadProfile(std::int64_t playerId);
    /// False if there's no such player.
    bool SaveProfile(std::int64_t playerId, const GameState& profile);
    /// Most recently active first.
    std::vector<PlayerRow> ListPlayers(int limit);

    /// provider = "guest" (later "google"…), subject = who they are to that provider.
    void AddIdentity(const std::string& provider, const std::string& subject, std::int64_t playerId,
        const std::string& createdAt);
    std::optional<std::int64_t> FindIdentity(const std::string& provider, const std::string& subject);

    void AddSession(const std::string& tokenHash, std::int64_t playerId, const std::string& createdAt,
        const std::string& expiresAt);
    std::optional<Session> FindSession(const std::string& tokenHash);
    void DeleteSession(const std::string& tokenHash);
    void DeleteSessionsExpiredBy(const std::string& now);

    std::optional<ContentRow> LoadContent();
    void SaveContent(const std::string& config, int version, const std::string& updatedAt);

private:
    void Exec(const char* sql);
    void Migrate();

    sqlite3* _db = nullptr;
};
