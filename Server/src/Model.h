#pragma once

#include <map>
#include <optional>
#include <string>
#include <vector>

#include <nlohmann/json.hpp>

// Mirrors of the client's Model/ classes. Field names match the JSON, so they're camelCase.

// ---------- Config: only the fields the rules need; /config sends the whole file as it is ----------

struct FishType
{
    std::string id;
    int value = 0;
    float minDepth = 0;
    bool isCollectible = false;
};

struct QuestData
{
    int id = 0;
    std::map<std::string, int> requirements;
    std::vector<int> unlocksId;
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

struct GameConfig
{
    std::vector<FishType> fish;
    std::vector<QuestData> quests;
    std::vector<int> startingQuests;
    std::vector<GearUpgradeData> gear;
};

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(FishType, id, value, minDepth, isCollectible)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(QuestData, id, requirements, unlocksId)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(GearUpgradeData, id, maxLevel, baseCost, costGrowth, baseValue, valuePerLevel)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(GameConfig, fish, quests, startingQuests, gear)

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

struct GameState
{
    PlayerState player;
    QuestProgressState quests;
    UpgradeProgressState upgrades;
    std::optional<ActiveRunState> activeRun;
    std::string savedAt;
};

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(PlayerState, runsCompleted, gems, launderedRubles, dirtyRubles, reputation, fishCollection)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(QuestProgressState, active, completed)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(UpgradeProgressState, gearLevels)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(ActiveRunState, id, startedAt, maxDepth, capacity)

// Written by hand for the optional activeRun, which is null between runs.
inline void to_json(nlohmann::json& json, const GameState& state)
{
    json = {
        {"player", state.player},
        {"quests", state.quests},
        {"upgrades", state.upgrades},
        {"activeRun", state.activeRun ? nlohmann::json(*state.activeRun) : nlohmann::json(nullptr)},
        {"savedAt", state.savedAt},
    };
}

inline void from_json(const nlohmann::json& json, GameState& state)
{
    state.player = json.value("player", PlayerState{});
    state.quests = json.value("quests", QuestProgressState{});
    state.upgrades = json.value("upgrades", UpgradeProgressState{});
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
};

inline void from_json(const nlohmann::json& json, RunResult& result)
{
    // The client sends null when it has no run; that's "not the active run", not a malformed request.
    const auto& runId = json.at("runId");
    result.runId = runId.is_null() ? "" : runId.get<std::string>();
    result.shotFish = json.value("shotFish", std::map<std::string, int>{});
}
