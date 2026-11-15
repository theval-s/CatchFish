#include "GoogleAuth.h"

#include <cctype>
#include <chrono>
#include <utility>

#include <httplib.h>
#include <nlohmann/json.hpp>

#include "ApiError.h"

namespace
{
    std::string UrlEncode(const std::string& text)
    {
        static const char Digits[] = "0123456789ABCDEF";
        std::string encoded;
        for (const auto c : text)
        {
            const auto byte = static_cast<unsigned char>(c);
            if (std::isalnum(byte) || c == '-' || c == '_' || c == '.' || c == '~')
            {
                encoded += c;
            }
            else
            {
                encoded += '%';
                encoded += Digits[byte >> 4];
                encoded += Digits[byte & 0x0F];
            }
        }

        return encoded;
    }

    std::string Base64UrlDecode(const std::string& text)
    {
        std::string decoded;
        int buffer = 0;
        int bits = 0;
        for (const auto c : text)
        {
            int value;
            if (c >= 'A' && c <= 'Z')
            {
                value = c - 'A';
            }
            else if (c >= 'a' && c <= 'z')
            {
                value = c - 'a' + 26;
            }
            else if (c >= '0' && c <= '9')
            {
                value = c - '0' + 52;
            }
            else if (c == '-' || c == '+')
            {
                value = 62;
            }
            else if (c == '_' || c == '/')
            {
                value = 63;
            }
            else
            {
                break;
            }

            buffer = (buffer << 6) | value;
            bits += 6;
            if (bits >= 8)
            {
                bits -= 8;
                decoded += static_cast<char>((buffer >> bits) & 0xFF);
            }
        }

        return decoded;
    }

    [[noreturn]] void GoogleError(const std::string& message)
    {
        throw ApiError(502, "google_error", message);
    }
}

GoogleAuth::GoogleAuth(Config config)
    : _config(std::move(config))
{
}

bool GoogleAuth::Enabled() const
{
    return !_config.clientId.empty() && !_config.clientSecret.empty() && !_config.publicUrl.empty();
}

std::string GoogleAuth::RedirectUri() const
{
    return _config.publicUrl + "/auth/google/callback";
}

std::string GoogleAuth::AuthorizationUrl(const std::string& state) const
{
    return _config.authUrl + "?response_type=code&scope=openid%20email&prompt=select_account"
        + "&client_id=" + UrlEncode(_config.clientId)
        + "&redirect_uri=" + UrlEncode(RedirectUri())
        + "&state=" + UrlEncode(state);
}

GoogleAuth::Account GoogleAuth::ExchangeCode(const std::string& code, Clock::time_point now) const
{
    // "https://host/path" → client for "https://host", request to "/path".
    const auto pathStart = _config.tokenUrl.find('/', _config.tokenUrl.find("//") + 2);
    httplib::Client client(_config.tokenUrl.substr(0, pathStart));
    client.set_connection_timeout(10);
    client.set_read_timeout(10);

    const httplib::Params form{
        {"grant_type", "authorization_code"},
        {"code", code},
        {"client_id", _config.clientId},
        {"client_secret", _config.clientSecret},
        {"redirect_uri", RedirectUri()},
    };
    const auto response = client.Post(pathStart == std::string::npos ? "/" : _config.tokenUrl.substr(pathStart), form);
    if (!response)
    {
        GoogleError("can't reach Google: " + httplib::to_string(response.error()));
    }

    const auto body = nlohmann::json::parse(response->body, nullptr, false);
    if (response->status != 200 || body.is_discarded() || !body.contains("id_token"))
    {
        GoogleError("Google refused the code: " + response->body.substr(0, 200));
    }

    // The ID token came straight from Google over HTTPS, so its signature needn't be checked;
    // its claims still must be meant for us and current.
    const auto token = body.at("id_token").get<std::string>();
    const auto first = token.find('.');
    const auto second = token.find('.', first + 1);
    if (first == std::string::npos || second == std::string::npos)
    {
        GoogleError("Google sent a malformed ID token");
    }

    const auto claims = nlohmann::json::parse(Base64UrlDecode(token.substr(first + 1, second - first - 1)), nullptr, false);
    if (claims.is_discarded() || !claims.contains("sub"))
    {
        GoogleError("Google sent an unreadable ID token");
    }

    const auto issuer = claims.value("iss", "");
    const auto expires = claims.value("exp", 0LL);
    const auto nowSeconds = std::chrono::duration_cast<std::chrono::seconds>(now.time_since_epoch()).count();
    if (claims.value("aud", "") != _config.clientId
        || (issuer != "https://accounts.google.com" && issuer != "accounts.google.com") || expires <= nowSeconds)
    {
        GoogleError("the ID token isn't for this server, or it expired");
    }
    if (claims.contains("email") && !claims.value("email_verified", false))
    {
        GoogleError("the Google account's email isn't verified");
    }

    return {claims.at("sub").get<std::string>(), claims.value("email", "")};
}
