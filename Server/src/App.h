#pragma once

#include <mutex>
#include <string>
#include <utility>

#include "Accounts.h"
#include "Content.h"
#include "GoogleAuth.h"
#include "Storage.h"

/// What the routes share. Every route that touches it holds `lock`: one request at a time reads and
/// writes the game state. Simple, can't lose an update, and fast enough for a handful of players.
struct App
{
    App(const std::string& dbPath, const std::string& seedPath, std::string devKey, GoogleAuth::Config google)
        : storage(dbPath), content(storage, seedPath, Clock::now()), accounts(storage), google(std::move(google)),
          devKey(std::move(devKey))
    {
    }

    Storage storage;
    Content content;
    Accounts accounts;
    // Talks to Google over the network, so routes call it without holding `lock`.
    const GoogleAuth google;
    // Empty = the dev API is off.
    std::string devKey;
    std::mutex lock;
};
