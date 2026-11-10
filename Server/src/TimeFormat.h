#pragma once

#include <chrono>
#include <string>

using Clock = std::chrono::system_clock;

/// "2026-11-03T12:00:00.000Z": ISO 8601 in UTC, which the client's Newtonsoft reads as a UTC DateTime.
std::string FormatTime(Clock::time_point time);

/// Reads what FormatTime writes (the milliseconds are optional). Throws std::invalid_argument otherwise.
Clock::time_point ParseTime(const std::string& text);
