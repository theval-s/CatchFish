#include "Storage.h"

#include <stdexcept>

#include <sqlite3.h>

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

    private:
        sqlite3* _db;
        sqlite3_stmt* _statement = nullptr;
    };
}

Storage::Storage(const std::string& path)
{
    const auto code = sqlite3_open(path.c_str(), &_db);
    if (code != SQLITE_OK)
    {
        const std::string message = _db != nullptr ? sqlite3_errmsg(_db) : "out of memory";
        sqlite3_close(_db);
        throw std::runtime_error("Can't open the database '" + path + "': " + message);
    }

    Check(_db, sqlite3_exec(_db,
        "CREATE TABLE IF NOT EXISTS players ("
        "  id INTEGER PRIMARY KEY AUTOINCREMENT,"
        "  token TEXT NOT NULL UNIQUE,"
        "  profile TEXT NOT NULL,"
        "  created_at TEXT NOT NULL,"
        "  updated_at TEXT NOT NULL)",
        nullptr, nullptr, nullptr), "create tables");
}

Storage::~Storage()
{
    sqlite3_close(_db);
}

void Storage::CreatePlayer(const std::string& token, const GameState& state)
{
    Statement insert(_db, "INSERT INTO players (token, profile, created_at, updated_at) VALUES (?, ?, ?, ?)");
    insert.Bind(1, token);
    insert.Bind(2, nlohmann::json(state).dump());
    insert.Bind(3, state.savedAt);
    insert.Bind(4, state.savedAt);
    insert.Step();
}

std::optional<GameState> Storage::LoadProfile(const std::string& token)
{
    Statement select(_db, "SELECT profile FROM players WHERE token = ?");
    select.Bind(1, token);
    if (!select.Step())
    {
        return std::nullopt;
    }

    return nlohmann::json::parse(select.Text(0)).get<GameState>();
}

void Storage::SaveProfile(const std::string& token, const GameState& state)
{
    Statement update(_db, "UPDATE players SET profile = ?, updated_at = ? WHERE token = ?");
    update.Bind(1, nlohmann::json(state).dump());
    update.Bind(2, state.savedAt);
    update.Bind(3, token);
    update.Step();
}
