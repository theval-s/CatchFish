#include "TimeFormat.h"

#include <cstdint>
#include <cstdio>
#include <ctime>
#include <stdexcept>

namespace
{
    // Days since 1970-01-01 for a date in the proleptic Gregorian calendar
    // (Howard Hinnant's algorithm; avoids timegm, which Windows doesn't have).
    int64_t DaysFromCivil(int64_t year, unsigned month, unsigned day)
    {
        year -= month <= 2;
        const int64_t era = (year >= 0 ? year : year - 399) / 400;
        const auto yearOfEra = static_cast<unsigned>(year - era * 400);
        const unsigned dayOfYear = (153 * (month > 2 ? month - 3 : month + 9) + 2) / 5 + day - 1;
        const unsigned dayOfEra = yearOfEra * 365 + yearOfEra / 4 - yearOfEra / 100 + dayOfYear;
        return era * 146097 + static_cast<int64_t>(dayOfEra) - 719468;
    }
}

std::string FormatTime(Clock::time_point time)
{
    using namespace std::chrono;

    const auto sinceEpoch = duration_cast<milliseconds>(time.time_since_epoch());
    const auto seconds = static_cast<std::time_t>(duration_cast<std::chrono::seconds>(sinceEpoch).count());
    const auto millis = static_cast<int>(sinceEpoch.count() % 1000);

    std::tm parts{};
#ifdef _WIN32
    gmtime_s(&parts, &seconds);
#else
    gmtime_r(&seconds, &parts);
#endif

    char text[32];
    std::snprintf(text, sizeof(text), "%04d-%02d-%02dT%02d:%02d:%02d.%03dZ",
        parts.tm_year + 1900, parts.tm_mon + 1, parts.tm_mday,
        parts.tm_hour, parts.tm_min, parts.tm_sec, millis);
    return text;
}

Clock::time_point ParseTime(const std::string& text)
{
    using namespace std::chrono;

    int year, month, day, hour, minute, second;
    int millis = 0;
    const int read = std::sscanf(text.c_str(), "%d-%d-%dT%d:%d:%d.%3d",
        &year, &month, &day, &hour, &minute, &second, &millis);
    if (read < 6)
    {
        throw std::invalid_argument("Not an ISO 8601 time: '" + text + "'");
    }

    const auto days = DaysFromCivil(year, static_cast<unsigned>(month), static_cast<unsigned>(day));
    const auto sinceEpoch = hours(days * 24 + hour) + minutes(minute) + std::chrono::seconds(second) + milliseconds(millis);
    return Clock::time_point(duration_cast<Clock::duration>(sinceEpoch));
}
