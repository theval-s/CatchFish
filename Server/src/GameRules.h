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

    /// Brings a profile up to date with a config that may have changed since it was saved and with
    /// the time: opens auto-start quests, drops open quests that no longer exist, regenerates energy,
    /// pays out the school.
    /// True if anything changed.
    bool Refresh(GameState& state, Clock::time_point now) const;

    void BuyGear(GameState& state, const std::string& gearId) const;
    void CompleteQuest(GameState& state, int questId) const;
    void StartRun(GameState& state, Clock::time_point now) const;
    void CompleteRun(GameState& state, const RunResult& result, Clock::time_point now) const;
    void ClaimDailyReward(GameState& state, Clock::time_point now) const;
    void RefillEnergy(GameState& state, Clock::time_point now) const;
    void HireTutor(GameState& state, const std::string& classId) const;
    void AddStudent(GameState& state, const std::string& classId) const;
    void UpgradeHome(GameState& state, const std::string& objectId, bool useGems) const;
    void LevelUpHome(GameState& state, const std::string& objectId) const;

private:
    void Validate() const;
    bool SettleEnergy(GameState& state, Clock::time_point now) const;
    bool SettleSchool(GameState& state, Clock::time_point now) const;
    const SchoolClassData& GetClass(const std::string& id) const;
    const HomeObjectData& GetHomeObject(const std::string& id) const;
    bool QuestsDone(const GameState& state, const HomeLevelData& level) const;
    bool IsResource(const std::string& id) const;
    const FishType* FindFish(const std::string& id) const;
    const QuestData* FindQuest(int id) const;
    const GearUpgradeData* FindGear(const std::string& id) const;
    float GearValue(const GameState& state, const std::string& gearId) const;

    GameConfig _config;
};
