#pragma once

#include <string>

#include "TimeFormat.h"

/// Google sign-in, done by the server: it sends the player's browser to Google, and trades the code
/// Google sends back for the player's Google account id. The client secret never leaves the server.
class GoogleAuth
{
public:
    struct Config
    {
        std::string clientId;
        std::string clientSecret;
        // Where players' browsers reach this server, e.g. https://fish.thevals.ru:1337.
        std::string publicUrl;
        std::string authUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        std::string tokenUrl = "https://oauth2.googleapis.com/token";
    };

    struct Account
    {
        // Google's id for the account: stays the same even if the email changes.
        std::string subject;
        std::string email;
    };

    explicit GoogleAuth(Config config);

    bool Enabled() const;

    /// The Google page to send the player's browser to; Google comes back to the callback with `state`.
    std::string AuthorizationUrl(const std::string& state) const;

    /// Trades the code from the callback for the account. ApiError 502 google_error if Google refuses.
    Account ExchangeCode(const std::string& code, Clock::time_point now) const;

    std::string RedirectUri() const;

private:
    Config _config;
};
