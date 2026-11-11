#pragma once

#include <string>

#include <picosha2.h>

/// SHA-256 as hex. Session tokens and guest secrets are stored only as this hash, so a copy of the
/// database doesn't let anyone sign in. They're 256-bit random values, so a fast hash is enough.
inline std::string HashSecret(const std::string& secret)
{
    return picosha2::hash256_hex_string(secret);
}
