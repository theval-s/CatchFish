#pragma once

#include <map>
#include <optional>
#include <string>
#include <vector>

#include <nlohmann/json.hpp>

// Mirrors of the client's Model/ classes. Field names match the JSON, so they're camelCase.

// ---------- Config: only the fields the rules need; /config sends the whole JSON as stored ----------

struct FishType
{
    std::string id;
    int value = 0;
    float minDepth = 0;
    bool isCollectible = false;
    // "" for a normal fish, or "bomb", "eel", "shark". A shark's value is the fine for losing the hook.
    std::string kind;
};

struct QuestData
{
    int id = 0;
    std::map<std::string, int> requirements;
    std::vector<int> unlocksId;
    // Opened for every player who hasn't done it, not only through startingQuests or unlocksId.
    bool autoStart = false;
};

struct GearUpgradeData
{
    std::string id;
    int maxLevel = 0;
    int baseCost = 0;
    // float, like the client, so prices come out the same to the ruble.
    float costGrowth = 1;
    float baseValue = 0;
    float valuePerLevel = 0;
};

/// One reward per login day; after the last day the track starts over.
struct DailyRewardTrack
{
    // Where the server's calendar day starts: +3 = midnight in Moscow.
    int utcOffsetHours = 3;
    // Resource id → amount, one map per day.
    std::vector<std::map<std::string, int>> days;
};

/// Each run costs energy; it comes back over time or for gems.
struct EnergyConfig
{
    int max = 5;
    int regenMinutes = 20;
    int runCost = 1;
    int refillGems = 10;
};

/// A school class: hire its tutor, then recruit students. A class with a tutor launders dirty rubles into
/// clean ones every minute; its students also earn reputation.
struct SchoolClassData
{
    std::string id;
    std::map<std::string, int> tutorCost;
    // The price of the first student; each next one costs studentCostGrowth times more.
    std::map<std::string, int> studentCost;
    float studentCostGrowth = 1;
    int maxStudents = 0;
    // Per minute: launderBase + launderPerStudent * students, and repPerStudent * students.
    int launderBase = 0;
    int launderPerStudent = 0;
    int repPerStudent = 0;
};

struct SchoolConfig
{
    // The most minutes counted between two requests, so a long absence pays a capped amount.
    int offlineCapMinutes = 480;
    std::vector<SchoolClassData> classes;
};

struct GameConfig
{
    std::vector<FishType> fish;
    std::vector<QuestData> quests;
    std::vector<int> startingQuests;
    std::vector<GearUpgradeData> gear;
    DailyRewardTrack dailyRewards;
    EnergyConfig energy;
    SchoolConfig school;
};

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(FishType, id, value, minDepth, isCollectible, kind)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(QuestData, id, requirements, unlocksId, autoStart)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(GearUpgradeData, id, maxLevel, baseCost, costGrowth, baseValue, valuePerLevel)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(DailyRewardTrack, utcOffsetHours, days)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(EnergyConfig, max, regenMinutes, runCost, refillGems)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(SchoolClassData, id, tutorCost, studentCost, studentCostGrowth, maxStudents,
    launderBase, launderPerStudent, repPerStudent)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(SchoolConfig, offlineCapMinutes, classes)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(GameConfig, fish, quests, startingQuests, gear, dailyRewards, energy, school)

// ---------- Profile: the server owns it, so every field is here ----------

struct PlayerState
{
    int runsCompleted = 0;
    int gems = 0;
    int launderedRubles = 0;
    int dirtyRubles = 0;
    int reputation = 0;
    std::map<std::string, int> fishCollection;
};

struct QuestProgressState
{
    std::vector<int> active;
    std::vector<int> completed;
};

struct UpgradeProgressState
{
    std::map<std::string, int> gearLevels;
};

struct ActiveRunState
{
    std::string id;
    std::string startedAt;
    float maxDepth = 0;
    int capacity = 0;
};

struct DailyRewardState
{
    // Index of the next reward in the track.
    int nextDay = 0;
    // Calendar day of the last claim (days since 1970 in the track's time zone); -1 = never.
    int lastClaimDay = -1;
};

struct EnergyState
{
    int amount = 0;
    // When the next point started to regenerate. Empty = never set: the profile starts full.
    std::string updatedAt;
};

struct SchoolClassState
{
    bool hasTutor = false;
    int students = 0;
};

struct SchoolState
{
    // Class id → state; classes the player hasn't touched aren't here.
    std::map<std::string, SchoolClassState> classes;
    // Everything up to this time has been laundered. Empty = never set.
    std::string lastTransactionAt;
};

struct GameState
{
    PlayerState player;
    QuestProgressState quests;
    UpgradeProgressState upgrades;
    DailyRewardState daily;
    EnergyState energy;
    SchoolState school;
    std::optional<ActiveRunState> activeRun;
    std::string savedAt;
};

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(PlayerState, runsCompleted, gems, launderedRubles, dirtyRubles, reputation, fishCollection)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(QuestProgressState, active, completed)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(UpgradeProgressState, gearLevels)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(ActiveRunState, id, startedAt, maxDepth, capacity)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(DailyRewardState, nextDay, lastClaimDay)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(EnergyState, amount, updatedAt)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(SchoolClassState, hasTutor, students)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(SchoolState, classes, lastTransactionAt)

// Written by hand for the optional activeRun, which is null between runs.
inline void to_json(nlohmann::json& json, const GameState& state)
{
    json = {
        {"player", state.player},
        {"quests", state.quests},
        {"upgrades", state.upgrades},
        {"daily", state.daily},
        {"energy", state.energy},
        {"school", state.school},
        {"activeRun", state.activeRun ? nlohmann::json(*state.activeRun) : nlohmann::json(nullptr)},
        {"savedAt", state.savedAt},
    };
}

inline void from_json(const nlohmann::json& json, GameState& state)
{
    state.player = json.value("player", PlayerState{});
    state.quests = json.value("quests", QuestProgressState{});
    state.upgrades = json.value("upgrades", UpgradeProgressState{});
    state.daily = json.value("daily", DailyRewardState{});
    state.energy = json.value("energy", EnergyState{});
    state.school = json.value("school", SchoolState{});
    state.savedAt = json.value("savedAt", "");

    const auto run = json.find("activeRun");
    if (run != json.end() && !run->is_null())
    {
        state.activeRun = run->get<ActiveRunState>();
    }
    else
    {
        state.activeRun.reset();
    }
}

// ---------- Requests ----------

/// Body of POST /runs/complete.
struct RunResult
{
    std::string runId;
    std::map<std::string, int> shotFish;
    // Shot fish that were on the hook when an eel was hooked: they pay half.
    std::map<std::string, int> shockedFish;
    // The shark that took the hook, or empty.
    std::string bittenBy;
};

inline void from_json(const nlohmann::json& json, RunResult& result)
{
    // The client sends null when it has no run; that's "not the active run", not a malformed request.
    const auto& runId = json.at("runId");
    result.runId = runId.is_null() ? "" : runId.get<std::string>();
    result.shotFish = json.value("shotFish", std::map<std::string, int>{});
    result.shockedFish = json.value("shockedFish", std::map<std::string, int>{});
    const auto bittenBy = json.find("bittenBy");
    result.bittenBy = bittenBy == json.end() || bittenBy->is_null() ? "" : bittenBy->get<std::string>();
}
