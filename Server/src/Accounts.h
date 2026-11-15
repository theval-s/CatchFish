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

    /// What a finished Google sign-in gives the game: a session, and, when the device now plays as
    /// another player, a guest secret of that player's to sign in with from now on.
    struct GoogleSignIn
    {
        Session session;
        std::string guestSecret;
        std::string email;
    };

    explicit Accounts(Storage& storage);

    /// A new player with this profile and a guest identity. The client keeps the secret to sign in again.
    NewGuest CreateGuest(const GameState& profile, Clock::time_point now);

    /// A new session for the guest with this secret. ApiError 401 invalid_credentials if there's none.
    Session SignInGuest(const std::string& guestSecret, Clock::time_point now);

    void SignOut(const std::string& token);

    /// Records a Google sign-in for the game to collect with `loginSecret`; links the account to
    /// `linkPlayerId` if it isn't 0. Returns the login id, which is also the OAuth state.
    std::string BeginGoogle(const std::string& loginSecret, std::int64_t linkPlayerId, Clock::time_point now);

    /// The login Google's callback is about, if it's still waiting. ApiError 400 otherwise.
    Storage::GoogleLogin PendingGoogleLogin(const std::string& loginId, Clock::time_point now);

    /// Google confirmed the account: finds, creates or links the player and marks the login done
    /// (or "taken" when linking an account another player has). `newProfile` is for a new player.
    /// Returns the login as saved.
    Storage::GoogleLogin CompleteGoogle(Storage::GoogleLogin login, const std::string& subject, const std::string& email,
        const GameState& newProfile, Clock::time_point now);

    void FailGoogle(Storage::GoogleLogin login, const std::string& error);

    /// Collects a login: nullopt while Google hasn't answered. ApiError 409 identity_taken when the
    /// account is another player's, unless `switchPlayer`, which signs this device in as that player.
    std::optional<GoogleSignIn> FinishGoogle(const std::string& loginId, const std::string& loginSecret,
        bool switchPlayer, Clock::time_point now);

    /// The Google account's email, or empty if the player has none linked.
    std::string GoogleEmail(std::int64_t playerId);

    /// The player a session token belongs to. ApiError 401 unauthorized or session_expired.
    std::int64_t Authenticate(const std::string& token, Clock::time_point now);

private:
    Session OpenSession(std::int64_t playerId, Clock::time_point now);

    Storage& _storage;
};
