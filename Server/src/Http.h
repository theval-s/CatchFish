#pragma once

#include <cstdint>
#include <functional>
#include <string>

#include <httplib.h>
#include <nlohmann/json.hpp>

// Small helpers every route uses.

void SendJson(httplib::Response& response, const nlohmann::json& body, int status = 200);

void SendError(httplib::Response& response, int status, const std::string& code, const std::string& message);

/// Runs a handler and turns whatever it throws into an error response.
void Respond(httplib::Response& response, const std::function<void()>& handler);

/// The request body as JSON; an empty body is {}.
nlohmann::json JsonBody(const httplib::Request& request);

/// The token from "Authorization: Bearer <token>". ApiError 401 unauthorized if there isn't one.
std::string BearerToken(const httplib::Request& request);

/// The id captured by a route's `(\d+)`. ApiError 404 if it doesn't fit.
std::int64_t PathId(const httplib::Request& request);
