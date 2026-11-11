#include "Accounts.h"

#include "ApiError.h"
#include "Hash.h"
#include "Random.h"

namespace
{
    const char* const Guest = "guest";
}

Accounts::Accounts(Storage& storage)
    : _storage(storage)
{
}

Accounts::NewGuest Accounts::CreateGuest(const GameState& profile, Clock::time_point now)
{
    const auto secret = RandomHex(32);

    Storage::Transaction transaction(_storage);
    const auto playerId = _storage.CreatePlayer(profile);
    _storage.AddIdentity(Guest, HashSecret(secret), playerId, FormatTime(now));
    auto session = OpenSession(playerId, now);
    transaction.Commit();

    return {secret, session};
}

Accounts::Session Accounts::SignInGuest(const std::string& guestSecret, Clock::time_point now)
{
    const auto playerId = _storage.FindIdentity(Guest, HashSecret(guestSecret));
    if (!playerId)
    {
        throw ApiError(401, "invalid_credentials", "no player has this guest secret");
    }

    // Sign-ins are rare, so this is a good moment to forget old sessions.
    _storage.DeleteSessionsExpiredBy(FormatTime(now));
    return OpenSession(*playerId, now);
}

void Accounts::SignOut(const std::string& token)
{
    _storage.DeleteSession(HashSecret(token));
}

std::int64_t Accounts::Authenticate(const std::string& token, Clock::time_point now)
{
    const auto session = _storage.FindSession(HashSecret(token));
    if (!session)
    {
        throw ApiError(401, "unauthorized", "unknown session");
    }

    if (ParseTime(session->expiresAt) <= now)
    {
        throw ApiError(401, "session_expired", "sign in again");
    }

    return session->playerId;
}

Accounts::Session Accounts::OpenSession(std::int64_t playerId, Clock::time_point now)
{
    const auto token = RandomHex(32);
    const auto expiresAt = FormatTime(now + std::chrono::hours(24 * SessionDays));
    _storage.AddSession(HashSecret(token), playerId, FormatTime(now), expiresAt);
    return {playerId, token, expiresAt};
}
