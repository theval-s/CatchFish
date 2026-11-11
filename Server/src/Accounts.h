#pragma once

#include <cstdint>
#include <string>

#include "Storage.h"
#include "TimeFormat.h"

/// Who a request comes from. A player has identities (ways to sign in: a guest secret now, Google
/// later) and sessions (the bearer token every game request carries, valid for SessionDays).
class Accounts
{
public:
    static constexpr int SessionDays = 30;

    struct Session
    {
        std::int64_t playerId;
        std::string token;
        std::string expiresAt;
    };

    struct NewGuest
    {
        std::string guestSecret;
        Session session;
    };

    explicit Accounts(Storage& storage);

    /// A new player with this profile and a guest identity. The client keeps the secret to sign in again.
    NewGuest CreateGuest(const GameState& profile, Clock::time_point now);

    /// A new session for the guest with this secret. ApiError 401 invalid_credentials if there's none.
    Session SignInGuest(const std::string& guestSecret, Clock::time_point now);

    void SignOut(const std::string& token);

    /// The player a session token belongs to. ApiError 401 unauthorized or session_expired.
    std::int64_t Authenticate(const std::string& token, Clock::time_point now);

private:
    Session OpenSession(std::int64_t playerId, Clock::time_point now);

    Storage& _storage;
};
