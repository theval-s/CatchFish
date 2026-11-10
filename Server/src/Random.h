#pragma once

#include <random>
#include <string>

/// Random hex string straight from the OS's random source: tokens and run ids must not be guessable.
inline std::string RandomHex(int bytes)
{
    static const char Digits[] = "0123456789abcdef";
    std::random_device source;
    std::string text;
    for (int i = 0; i < bytes; i++)
    {
        const auto byte = source() & 0xFF;
        text += Digits[byte >> 4];
        text += Digits[byte & 0x0F];
    }

    return text;
}
