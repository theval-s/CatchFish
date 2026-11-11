#pragma once

#include <httplib.h>

#include "App.h"

/// What the game uses: sign-in, the config, the profile and every player action.
void AddGameRoutes(httplib::Server& server, App& app);

/// /dev/…: editing the live config and looking into players, for the team (with Postman).
/// Every request needs the X-Dev-Key header; without a key configured they all answer 404.
void AddDevRoutes(httplib::Server& server, App& app);
