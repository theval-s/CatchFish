#pragma once

#include <stdexcept>
#include <string>
#include <utility>

/// A request the server refuses: sent back as { "error": code, "message": what() } with this HTTP status.
struct ApiError : std::runtime_error
{
    ApiError(int status, std::string code, const std::string& message)
        : std::runtime_error(message), status(status), code(std::move(code))
    {
    }

    int status;
    std::string code;
};
