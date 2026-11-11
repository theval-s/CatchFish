#pragma once

#include <string>

#include "Model.h"
#include "TimeFormat.h"

/// Everything a player can do to their profile, checked against the config. Each action changes
/// the given state or throws ApiError and leaves the caller to discard it.
class GameRules
{
public:
    /// Checks the config first (ids unique, references and resource ids exist) and throws
    /// ApiError 400 invalid_config naming the first problem, so the rules never run on a broken config.
    explicit GameRules(GameConfig config);

    GameState CreateProfile(Clock::time_point now) const;

    /// Brings a profile up to date with a config that may have changed since it was saved: opens
    /// auto-start quests, drops open quests that no longer exist. True if anything changed.
    bool Refresh(GameState& state) const;

    void BuyGear(GameState& state, const std::string& gearId) const;
    void CompleteQuest(GameState& state, int questId) const;
    void StartRun(GameState& state, Clock::time_point now) const;
    void CompleteRun(GameState& state, const RunResult& result, Clock::time_point now) const;
    void ClaimDailyReward(GameState& state, Clock::time_point now) const;

private:
    void Validate() const;
    bool IsResource(const std::string& id) const;
    const FishType* FindFish(const std::string& id) const;
    const QuestData* FindQuest(int id) const;
    const GearUpgradeData* FindGear(const std::string& id) const;
    float GearValue(const GameState& state, const std::string& gearId) const;

    GameConfig _config;
};
