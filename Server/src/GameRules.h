#pragma once

#include <string>

#include "Model.h"
#include "TimeFormat.h"

/// Everything a player can do to their profile, checked against the config. Each action changes
/// the given state or throws ApiError and leaves the caller to discard it. The client's
/// LocalApiClient has a C# copy of these rules; keep them in sync.
class GameRules
{
public:
    explicit GameRules(GameConfig config);

    GameState CreateProfile(Clock::time_point now) const;
    void BuyGear(GameState& state, const std::string& gearId) const;
    void CompleteQuest(GameState& state, int questId) const;
    void StartRun(GameState& state, Clock::time_point now) const;
    void CompleteRun(GameState& state, const RunResult& result, Clock::time_point now) const;

private:
    const FishType* FindFish(const std::string& id) const;
    const QuestData* FindQuest(int id) const;
    const GearUpgradeData* FindGear(const std::string& id) const;
    float GearValue(const GameState& state, const std::string& gearId) const;

    GameConfig _config;
};
