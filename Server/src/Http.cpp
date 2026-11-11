#include "Http.h"

#include <exception>
#include <iostream>

#include "ApiError.h"

void SendJson(httplib::Response& response, const nlohmann::json& body, int status)
{
    response.status = status;
    response.set_content(body.dump(), "application/json");
}

void SendError(httplib::Response& response, int status, const std::string& code, const std::string& message)
{
    SendJson(response, {{"error", code}, {"message", message}}, status);
}

void Respond(httplib::Response& response, const std::function<void()>& handler)
{
    try
    {
        handler();
    }
    catch (const ApiError& error)
    {
        SendError(response, error.status, error.code, error.what());
    }
    catch (const nlohmann::json::exception& error)
    {
        SendError(response, 400, "bad_request", error.what());
    }
    catch (const std::exception& error)
    {
        std::cerr << "Internal error: " << error.what() << std::endl;
        SendError(response, 500, "internal_error", error.what());
    }
}

nlohmann::json JsonBody(const httplib::Request& request)
{
    return request.body.empty() ? nlohmann::json::object() : nlohmann::json::parse(request.body);
}

std::string BearerToken(const httplib::Request& request)
{
    const std::string prefix = "Bearer ";
    const auto header = request.get_header_value("Authorization");
    if (header.compare(0, prefix.size(), prefix) != 0 || header.size() == prefix.size())
    {
        throw ApiError(401, "unauthorized", "no session token");
    }

    return header.substr(prefix.size());
}

std::int64_t PathId(const httplib::Request& request)
{
    try
    {
        return std::stoll(request.matches[1].str());
    }
    catch (const std::exception&)
    {
        throw ApiError(404, "not_found", request.path);
    }
}
