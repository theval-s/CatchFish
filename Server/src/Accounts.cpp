#include "Accounts.h"

#include "ApiError.h"
#include "Hash.h"
#include "Random.h"

namespace
{
    const char* const Guest = "guest";
    const char* const Google = "google";
    constexpr int GoogleLoginMinutes = 10;
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

std::string Accounts::BeginGoogle(const std::string& loginSecret, std::int64_t linkPlayerId, Clock::time_point now)
{
    if (loginSecret.size() < 32)
    {
        throw ApiError(400, "bad_request", "loginSecret must be at least 32 characters");
    }

    _storage.DeleteGoogleLoginsExpiredBy(FormatTime(now));

    Storage::GoogleLogin login;
    login.id = RandomHex(16);
    login.secretHash = HashSecret(loginSecret);
    login.linkPlayerId = linkPlayerId;
    login.expiresAt = FormatTime(now + std::chrono::minutes(GoogleLoginMinutes));
    _storage.AddGoogleLogin(login, FormatTime(now));
    return login.id;
}

Storage::GoogleLogin Accounts::PendingGoogleLogin(const std::string& loginId, Clock::time_point now)
{
    const auto login = _storage.FindGoogleLogin(loginId);
    if (!login || login->status != "pending" || ParseTime(login->expiresAt) <= now)
    {
        throw ApiError(400, "login_expired", "this sign-in is over; start it again from the game");
    }

    return *login;
}

Storage::GoogleLogin Accounts::CompleteGoogle(Storage::GoogleLogin login, const std::string& subject, const std::string& email,
    const GameState& newProfile, Clock::time_point now)
{
    Storage::Transaction transaction(_storage);
    const auto owner = _storage.FindIdentity(Google, subject);
    login.email = email;
    if (login.linkPlayerId == 0)
    {
        login.status = "done";
        if (owner)
        {
            login.playerId = *owner;
        }
        else
        {
            login.playerId = _storage.CreatePlayer(newProfile);
            _storage.AddIdentity(Google, subject, login.playerId, FormatTime(now), email);
        }
    }
    else if (owner && *owner != login.linkPlayerId)
    {
        login.status = "taken";
        login.playerId = *owner;
    }
    else if (!owner && !GoogleEmail(login.linkPlayerId).empty())
    {
        login.status = "failed";
        login.error = "this player already has another Google account";
    }
    else
    {
        login.status = "done";
        login.playerId = login.linkPlayerId;
        if (!owner)
        {
            _storage.AddIdentity(Google, subject, login.playerId, FormatTime(now), email);
        }
    }

    _storage.UpdateGoogleLogin(login);
    transaction.Commit();
    return login;
}

void Accounts::FailGoogle(Storage::GoogleLogin login, const std::string& error)
{
    login.status = "failed";
    login.error = error;
    _storage.UpdateGoogleLogin(login);
}

std::optional<Accounts::GoogleSignIn> Accounts::FinishGoogle(const std::string& loginId,
    const std::string& loginSecret, bool switchPlayer, Clock::time_point now)
{
    auto login = _storage.FindGoogleLogin(loginId);
    if (!login || login->secretHash != HashSecret(loginSecret))
    {
        throw ApiError(404, "not_found", "no such sign-in");
    }
    if (login->status == "pending")
    {
        if (ParseTime(login->expiresAt) <= now)
        {
            _storage.DeleteGoogleLogin(loginId);
            throw ApiError(400, "login_expired", "Google didn't answer in time; start again");
        }

        return std::nullopt;
    }
    if (login->status == "failed")
    {
        _storage.DeleteGoogleLogin(loginId);
        throw ApiError(401, "google_failed", login->error);
    }
    if (login->status == "taken" && !switchPlayer)
    {
        throw ApiError(409, "identity_taken", login->email);
    }

    Storage::Transaction transaction(_storage);
    GoogleSignIn result;
    result.email = login->email;
    // Linking keeps the device's own guest; otherwise the device becomes one of the Google player's guests.
    if (login->playerId != login->linkPlayerId)
    {
        result.guestSecret = RandomHex(32);
        _storage.AddIdentity(Guest, HashSecret(result.guestSecret), login->playerId, FormatTime(now));
    }

    result.session = OpenSession(login->playerId, now);
    _storage.DeleteGoogleLogin(loginId);
    transaction.Commit();
    return result;
}

std::string Accounts::GoogleEmail(std::int64_t playerId)
{
    for (const auto& identity : _storage.ListIdentities(playerId))
    {
        if (identity.provider == Google)
        {
            return identity.label.empty() ? "(linked)" : identity.label;
        }
    }

    return "";
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
