#include "Storage.h"

#include <stdexcept>

#include <sqlite3.h>

#include "Hash.h"

namespace
{
    void Check(sqlite3* db, int code, const std::string& what)
    {
        if (code != SQLITE_OK && code != SQLITE_ROW && code != SQLITE_DONE)
        {
            throw std::runtime_error("SQLite, " + what + ": " + sqlite3_errmsg(db));
        }
    }

    /// A prepared statement, finalized when it goes out of scope.
    class Statement
    {
    public:
        Statement(sqlite3* db, const char* sql)
            : _db(db)
        {
            Check(_db, sqlite3_prepare_v2(_db, sql, -1, &_statement, nullptr), sql);
        }

        ~Statement()
        {
            sqlite3_finalize(_statement);
        }

        Statement(const Statement&) = delete;
        Statement& operator=(const Statement&) = delete;

        void Bind(int index, const std::string& value)
        {
            Check(_db, sqlite3_bind_text(_statement, index, value.c_str(), -1, SQLITE_TRANSIENT), "bind");
        }

        void Bind(int index, std::int64_t value)
        {
            Check(_db, sqlite3_bind_int64(_statement, index, value), "bind");
        }

        /// True while there's a row to read.
        bool Step()
        {
            const auto code = sqlite3_step(_statement);
            Check(_db, code, "step");
            return code == SQLITE_ROW;
        }

        std::string Text(int column)
        {
            const auto* text = sqlite3_column_text(_statement, column);
            return text != nullptr ? reinterpret_cast<const char*>(text) : "";
        }

        std::int64_t Int64(int column)
        {
            return sqlite3_column_int64(_statement, column);
        }

    private:
        sqlite3* _db;
        sqlite3_stmt* _statement = nullptr;
    };

    GameState ParseProfile(const std::string& json)
    {
        return nlohmann::json::parse(json).get<GameState>();
    }
}

// ---------- Transaction ----------

Storage::Transaction::Transaction(Storage& storage)
    : _storage(storage)
{
    _storage.Exec("BEGIN");
}

Storage::Transaction::~Transaction()
{
    if (!_done)
    {
        sqlite3_exec(_storage._db, "ROLLBACK", nullptr, nullptr, nullptr);
    }
}

void Storage::Transaction::Commit()
{
    _storage.Exec("COMMIT");
    _done = true;
}

// ---------- Storage ----------

Storage::Storage(const std::string& path)
{
    const auto code = sqlite3_open(path.c_str(), &_db);
    if (code != SQLITE_OK)
    {
        const std::string message = _db != nullptr ? sqlite3_errmsg(_db) : "out of memory";
        sqlite3_close(_db);
        throw std::runtime_error("Can't open the database '" + path + "': " + message);
    }

    Migrate();
}

Storage::~Storage()
{
    sqlite3_close(_db);
}

// Schema versions, kept in PRAGMA user_version:
//   0  the first server: players(id, token, profile, created_at, updated_at), the token was the account.
//   1  players without the token, plus identities, sessions and content. Each old token became the
//      secret of a guest identity, so clients that kept it can still sign in.
void Storage::Migrate()
{
    std::int64_t version;
    {
        Statement pragma(_db, "PRAGMA user_version");
        pragma.Step();
        version = pragma.Int64(0);
    }

    if (version >= 1)
    {
        return;
    }

    Transaction transaction(*this);

    Exec("CREATE TABLE identities ("
         "  provider TEXT NOT NULL,"
         "  subject TEXT NOT NULL,"
         "  player_id INTEGER NOT NULL,"
         "  created_at TEXT NOT NULL,"
         "  PRIMARY KEY (provider, subject))");
    Exec("CREATE TABLE sessions ("
         "  token_hash TEXT PRIMARY KEY,"
         "  player_id INTEGER NOT NULL,"
         "  created_at TEXT NOT NULL,"
         "  expires_at TEXT NOT NULL)");
    Exec("CREATE TABLE content ("
         "  id INTEGER PRIMARY KEY CHECK (id = 1),"
         "  config TEXT NOT NULL,"
         "  version INTEGER NOT NULL,"
         "  updated_at TEXT NOT NULL)");

    bool hasTokens;
    {
        Statement column(_db, "SELECT 1 FROM pragma_table_info('players') WHERE name = 'token'");
        hasTokens = column.Step();
    }

    const char* createPlayers =
        "CREATE TABLE players ("
        "  id INTEGER PRIMARY KEY AUTOINCREMENT,"
        "  profile TEXT NOT NULL,"
        "  created_at TEXT NOT NULL,"
        "  updated_at TEXT NOT NULL)";

    if (hasTokens)
    {
        {
            Statement select(_db, "SELECT id, token, created_at FROM players");
            while (select.Step())
            {
                AddIdentity("guest", HashSecret(select.Text(1)), select.Int64(0), select.Text(2));
            }
        }

        // SQLite can't drop a UNIQUE column, so the table is rebuilt without it.
        Exec("ALTER TABLE players RENAME TO players_v0");
        Exec(createPlayers);
        Exec("INSERT INTO players (id, profile, created_at, updated_at)"
             " SELECT id, profile, created_at, updated_at FROM players_v0");
        Exec("DROP TABLE players_v0");
    }
    else
    {
        Exec(createPlayers);
    }

    Exec("PRAGMA user_version = 1");
    transaction.Commit();
}

void Storage::Exec(const char* sql)
{
    Check(_db, sqlite3_exec(_db, sql, nullptr, nullptr, nullptr), sql);
}

std::int64_t Storage::CreatePlayer(const GameState& profile)
{
    Statement insert(_db, "INSERT INTO players (profile, created_at, updated_at) VALUES (?, ?, ?)");
    insert.Bind(1, nlohmann::json(profile).dump());
    insert.Bind(2, profile.savedAt);
    insert.Bind(3, profile.savedAt);
    insert.Step();
    return sqlite3_last_insert_rowid(_db);
}

std::optional<GameState> Storage::LoadProfile(std::int64_t playerId)
{
    Statement select(_db, "SELECT profile FROM players WHERE id = ?");
    select.Bind(1, playerId);
    if (!select.Step())
    {
        return std::nullopt;
    }

    return ParseProfile(select.Text(0));
}

bool Storage::SaveProfile(std::int64_t playerId, const GameState& profile)
{
    Statement update(_db, "UPDATE players SET profile = ?, updated_at = ? WHERE id = ?");
    update.Bind(1, nlohmann::json(profile).dump());
    update.Bind(2, profile.savedAt);
    update.Bind(3, playerId);
    update.Step();
    return sqlite3_changes(_db) > 0;
}

std::vector<Storage::PlayerRow> Storage::ListPlayers(int limit)
{
    Statement select(_db, "SELECT id, created_at, updated_at, profile FROM players ORDER BY updated_at DESC LIMIT ?");
    select.Bind(1, static_cast<std::int64_t>(limit));

    std::vector<PlayerRow> rows;
    while (select.Step())
    {
        rows.push_back({select.Int64(0), select.Text(1), select.Text(2), ParseProfile(select.Text(3))});
    }

    return rows;
}

void Storage::AddIdentity(const std::string& provider, const std::string& subject, std::int64_t playerId,
    const std::string& createdAt)
{
    Statement insert(_db, "INSERT INTO identities (provider, subject, player_id, created_at) VALUES (?, ?, ?, ?)");
    insert.Bind(1, provider);
    insert.Bind(2, subject);
    insert.Bind(3, playerId);
    insert.Bind(4, createdAt);
    insert.Step();
}

std::optional<std::int64_t> Storage::FindIdentity(const std::string& provider, const std::string& subject)
{
    Statement select(_db, "SELECT player_id FROM identities WHERE provider = ? AND subject = ?");
    select.Bind(1, provider);
    select.Bind(2, subject);
    if (!select.Step())
    {
        return std::nullopt;
    }

    return select.Int64(0);
}

void Storage::AddSession(const std::string& tokenHash, std::int64_t playerId, const std::string& createdAt,
    const std::string& expiresAt)
{
    Statement insert(_db, "INSERT INTO sessions (token_hash, player_id, created_at, expires_at) VALUES (?, ?, ?, ?)");
    insert.Bind(1, tokenHash);
    insert.Bind(2, playerId);
    insert.Bind(3, createdAt);
    insert.Bind(4, expiresAt);
    insert.Step();
}

std::optional<Storage::Session> Storage::FindSession(const std::string& tokenHash)
{
    Statement select(_db, "SELECT player_id, expires_at FROM sessions WHERE token_hash = ?");
    select.Bind(1, tokenHash);
    if (!select.Step())
    {
        return std::nullopt;
    }

    return Session{select.Int64(0), select.Text(1)};
}

void Storage::DeleteSession(const std::string& tokenHash)
{
    Statement remove(_db, "DELETE FROM sessions WHERE token_hash = ?");
    remove.Bind(1, tokenHash);
    remove.Step();
}

void Storage::DeleteSessionsExpiredBy(const std::string& now)
{
    // ISO times in one format sort as text.
    Statement remove(_db, "DELETE FROM sessions WHERE expires_at <= ?");
    remove.Bind(1, now);
    remove.Step();
}

std::optional<Storage::ContentRow> Storage::LoadContent()
{
    Statement select(_db, "SELECT config, version FROM content WHERE id = 1");
    if (!select.Step())
    {
        return std::nullopt;
    }

    return ContentRow{select.Text(0), static_cast<int>(select.Int64(1))};
}

void Storage::SaveContent(const std::string& config, int version, const std::string& updatedAt)
{
    Statement upsert(_db,
        "INSERT INTO content (id, config, version, updated_at) VALUES (1, ?, ?, ?)"
        " ON CONFLICT (id) DO UPDATE SET config = excluded.config, version = excluded.version,"
        " updated_at = excluded.updated_at");
    upsert.Bind(1, config);
    upsert.Bind(2, static_cast<std::int64_t>(version));
    upsert.Bind(3, updatedAt);
    upsert.Step();
}
