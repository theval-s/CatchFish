#include "GameRules.h"

#include <algorithm>
#include <cctype>
#include <cmath>
#include <set>
#include <stdexcept>
#include <utility>
#include <vector>

#include "ApiError.h"
#include "Random.h"

namespace
{
    // Fish spawn a little below the line's reach and the hook has a size, so allow a few meters.
    constexpr float DepthTolerance = 5.0f;
    constexpr double MinRunSeconds = 2.0;

    const char* const LineLength = "lineLength";
    const char* const HookCapacity = "hookCapacity";

    // The client reads all four, so a config without one of them would break the game.
    const char* const RequiredGear[] = {"lineLength", "lineDurability", "hookCapacity", "reelSpeed"};

    // ---------- Resources by id, like the client's PlayerState.GetResource / SetResource ----------

    bool IsCurrency(const std::string& id)
    {
        return id == "dr" || id == "lr" || id == "rep" || id == "gems";
    }

    bool IsFish(const std::string& id)
    {
        return id.size() > 1 && id[0] == 'f' && std::isdigit(static_cast<unsigned char>(id[1]));
    }

    int GetResource(const PlayerState& player, const std::string& id)
    {
        if (id == "dr")
        {
            return player.dirtyRubles;
        }
        if (id == "lr")
        {
            return player.launderedRubles;
        }
        if (id == "rep")
        {
            return player.reputation;
        }
        if (id == "gems")
        {
            return player.gems;
        }
        if (IsFish(id))
        {
            const auto found = player.fishCollection.find(id);
            return found != player.fishCollection.end() ? found->second : 0;
        }

        throw std::invalid_argument("Unknown resource id '" + id + "'");
    }

    void SetResource(PlayerState& player, const std::string& id, int value)
    {
        if (id == "dr")
        {
            player.dirtyRubles = value;
        }
        else if (id == "lr")
        {
            player.launderedRubles = value;
        }
        else if (id == "rep")
        {
            player.reputation = value;
        }
        else if (id == "gems")
        {
            player.gems = value;
        }
        else if (IsFish(id))
        {
            player.fishCollection[id] = value;
        }
        else
        {
            throw std::invalid_argument("Unknown resource id '" + id + "'");
        }
    }

    /// Spends all or nothing.
    void Spend(PlayerState& player, const std::map<std::string, int>& amounts)
    {
        for (const auto& [id, amount] : amounts)
        {
            const auto has = GetResource(player, id);
            if (has < amount)
            {
                throw ApiError(409, "not_enough_resources",
                    id + ": " + std::to_string(has) + " / " + std::to_string(amount));
            }
        }

        for (const auto& [id, amount] : amounts)
        {
            SetResource(player, id, GetResource(player, id) - amount);
        }
    }

    bool Contains(const std::vector<int>& ids, int id)
    {
        return std::find(ids.begin(), ids.end(), id) != ids.end();
    }

    int LevelOf(const GameState& state, const std::string& gearId)
    {
        const auto found = state.upgrades.gearLevels.find(gearId);
        return found != state.upgrades.gearLevels.end() ? found->second : 0;
    }

    // Same arithmetic as GearUpgradeData.CostAt on the client: double power of the float growth,
    // and nearbyint because C#'s Math.Round rounds halves to even.
    int CostAt(const GearUpgradeData& gear, int level)
    {
        return static_cast<int>(std::nearbyint(gear.baseCost * std::pow(static_cast<double>(gear.costGrowth), level)));
    }

    float ValueAt(const GearUpgradeData& gear, int level)
    {
        return gear.baseValue + gear.valuePerLevel * static_cast<float>(std::min(level, gear.maxLevel));
    }

    /// Calendar day number (days since 1970) in a time zone utcOffsetHours away from UTC.
    int DayNumber(Clock::time_point time, int utcOffsetHours)
    {
        using namespace std::chrono;
        const auto local = duration_cast<seconds>(time.time_since_epoch()).count() + utcOffsetHours * 3600LL;
        return static_cast<int>(local / 86400);
    }

    [[noreturn]] void InvalidConfig(const std::string& message)
    {
        throw ApiError(400, "invalid_config", message);
    }
}

GameRules::GameRules(GameConfig config)
    : _config(std::move(config))
{
    Validate();
}

GameState GameRules::CreateProfile(Clock::time_point now) const
{
    GameState state;
    state.quests.active = _config.startingQuests;
    Refresh(state);
    state.savedAt = FormatTime(now);
    return state;
}

bool GameRules::Refresh(GameState& state) const
{
    auto& progress = state.quests;
    const auto before = progress.active;

    // A quest deleted from the config can't be completed any more.
    progress.active.erase(
        std::remove_if(progress.active.begin(), progress.active.end(), [this](int id) { return FindQuest(id) == nullptr; }),
        progress.active.end());

    for (const auto& quest : _config.quests)
    {
        if (quest.autoStart && !Contains(progress.active, quest.id) && !Contains(progress.completed, quest.id))
        {
            progress.active.push_back(quest.id);
        }
    }

    return progress.active != before;
}

void GameRules::ClaimDailyReward(GameState& state, Clock::time_point now) const
{
    const auto& track = _config.dailyRewards;
    auto& daily = state.daily;
    const auto today = DayNumber(now, track.utcOffsetHours);
    if (daily.lastClaimDay >= today)
    {
        throw ApiError(409, "already_claimed", "the next reward opens tomorrow");
    }

    // Missed days cost nothing: the track carries on from where the player stopped,
    // and starts over after its last day.
    const auto day = static_cast<std::size_t>(std::max(daily.nextDay, 0)) % track.days.size();
    for (const auto& [id, amount] : track.days[day])
    {
        SetResource(state.player, id, GetResource(state.player, id) + amount);
    }

    daily.nextDay = static_cast<int>((day + 1) % track.days.size());
    daily.lastClaimDay = today;
}

void GameRules::BuyGear(GameState& state, const std::string& gearId) const
{
    const auto* gear = FindGear(gearId);
    if (gear == nullptr)
    {
        throw ApiError(400, "unknown_gear", gearId);
    }

    const auto level = LevelOf(state, gearId);
    if (level >= gear->maxLevel)
    {
        throw ApiError(409, "gear_maxed", gearId);
    }

    Spend(state.player, {{"dr", CostAt(*gear, level)}});
    state.upgrades.gearLevels[gearId] = level + 1;
}

void GameRules::CompleteQuest(GameState& state, int questId) const
{
    const auto* quest = FindQuest(questId);
    if (quest == nullptr)
    {
        throw ApiError(400, "unknown_quest", std::to_string(questId));
    }

    auto& progress = state.quests;
    if (!Contains(progress.active, questId))
    {
        throw ApiError(409, "quest_not_active", std::to_string(questId));
    }

    Spend(state.player, quest->requirements);
    progress.active.erase(std::find(progress.active.begin(), progress.active.end(), questId));
    progress.completed.push_back(questId);
    for (const auto id : quest->unlocksId)
    {
        if (!Contains(progress.active, id) && !Contains(progress.completed, id))
        {
            progress.active.push_back(id);
        }
    }
}

void GameRules::StartRun(GameState& state, Clock::time_point now) const
{
    // TODO: refuse while the wanted level is too high, once it exists
    ActiveRunState run;
    run.id = RandomHex(16);
    run.startedAt = FormatTime(now);
    run.maxDepth = GearValue(state, LineLength);
    run.capacity = static_cast<int>(GearValue(state, HookCapacity));
    state.activeRun = run;
}

void GameRules::CompleteRun(GameState& state, const RunResult& result, Clock::time_point now) const
{
    if (!state.activeRun || state.activeRun->id != result.runId)
    {
        throw ApiError(409, "no_active_run", result.runId);
    }

    const auto& run = *state.activeRun;
    const std::chrono::duration<double> elapsed = now - ParseTime(run.startedAt);
    if (elapsed.count() < MinRunSeconds)
    {
        throw ApiError(400, "invalid_run", "the run was too short");
    }

    // The minigame runs on the client, so the results can only be checked for being possible.
    std::vector<std::pair<const FishType*, int>> shot;
    auto total = 0;
    for (const auto& [fishId, count] : result.shotFish)
    {
        const auto* fish = FindFish(fishId);
        if (fish == nullptr)
        {
            throw ApiError(400, "unknown_fish", fishId);
        }

        if (count < 0 || fish->minDepth > run.maxDepth + DepthTolerance)
        {
            throw ApiError(400, "invalid_run", fishId + " can't be caught with this line");
        }

        shot.emplace_back(fish, count);
        total += count;
    }

    if (total > run.capacity)
    {
        throw ApiError(400, "invalid_run", "more fish than the hook holds");
    }

    auto& player = state.player;
    auto earnings = 0;
    for (const auto& [fish, count] : shot)
    {
        earnings += fish->value * count;
        if (fish->isCollectible)
        {
            SetResource(player, fish->id, GetResource(player, fish->id) + count);
        }
    }

    // Jellyfish cost money, but the balance never goes below zero.
    player.dirtyRubles = std::max(0, player.dirtyRubles + earnings);
    player.runsCompleted++;
    state.activeRun.reset();
}

void GameRules::Validate() const
{
    std::set<std::string> fishIds;
    for (const auto& fish : _config.fish)
    {
        if (!IsFish(fish.id))
        {
            InvalidConfig("fish id '" + fish.id + "' must look like f<number>: it's also a resource id");
        }
        if (!fishIds.insert(fish.id).second)
        {
            InvalidConfig("fish '" + fish.id + "' appears twice");
        }
    }

    std::set<int> questIds;
    for (const auto& quest : _config.quests)
    {
        const auto name = "quest " + std::to_string(quest.id);
        if (!questIds.insert(quest.id).second)
        {
            InvalidConfig(name + " appears twice");
        }
        for (const auto& [id, amount] : quest.requirements)
        {
            if (!IsResource(id) || amount <= 0)
            {
                InvalidConfig(name + " requires " + std::to_string(amount) + " of '" + id + "'");
            }
        }
        for (const auto unlocked : quest.unlocksId)
        {
            if (FindQuest(unlocked) == nullptr)
            {
                InvalidConfig(name + " unlocks quest " + std::to_string(unlocked) + ", which doesn't exist");
            }
        }
    }

    for (const auto id : _config.startingQuests)
    {
        if (FindQuest(id) == nullptr)
        {
            InvalidConfig("startingQuests has quest " + std::to_string(id) + ", which doesn't exist");
        }
    }

    std::set<std::string> gearIds;
    for (const auto& gear : _config.gear)
    {
        if (!gearIds.insert(gear.id).second)
        {
            InvalidConfig("gear '" + gear.id + "' appears twice");
        }
        if (gear.maxLevel < 0 || gear.baseCost < 0 || gear.costGrowth <= 0)
        {
            InvalidConfig("gear '" + gear.id + "' needs maxLevel >= 0, baseCost >= 0 and costGrowth > 0");
        }
    }
    for (const auto* id : RequiredGear)
    {
        if (FindGear(id) == nullptr)
        {
            InvalidConfig(std::string("gear '") + id + "' is missing; the game needs it");
        }
    }

    const auto& track = _config.dailyRewards;
    if (track.utcOffsetHours < -12 || track.utcOffsetHours > 14)
    {
        InvalidConfig("dailyRewards.utcOffsetHours must be between -12 and 14");
    }
    if (track.days.empty())
    {
        InvalidConfig("dailyRewards needs at least one day");
    }
    for (std::size_t i = 0; i < track.days.size(); i++)
    {
        const auto name = "dailyRewards day " + std::to_string(i + 1);
        if (track.days[i].empty())
        {
            InvalidConfig(name + " has no reward");
        }
        for (const auto& [id, amount] : track.days[i])
        {
            if (!IsResource(id) || amount <= 0)
            {
                InvalidConfig(name + " gives " + std::to_string(amount) + " of '" + id + "'");
            }
        }
    }
}

bool GameRules::IsResource(const std::string& id) const
{
    return IsCurrency(id) || FindFish(id) != nullptr;
}

const FishType* GameRules::FindFish(const std::string& id) const
{
    for (const auto& fish : _config.fish)
    {
        if (fish.id == id)
        {
            return &fish;
        }
    }

    return nullptr;
}

const QuestData* GameRules::FindQuest(int id) const
{
    for (const auto& quest : _config.quests)
    {
        if (quest.id == id)
        {
            return &quest;
        }
    }

    return nullptr;
}

const GearUpgradeData* GameRules::FindGear(const std::string& id) const
{
    for (const auto& gear : _config.gear)
    {
        if (gear.id == id)
        {
            return &gear;
        }
    }

    return nullptr;
}

float GameRules::GearValue(const GameState& state, const std::string& gearId) const
{
    const auto* gear = FindGear(gearId);
    if (gear == nullptr)
    {
        throw std::invalid_argument("The config has no gear '" + gearId + "'");
    }

    return ValueAt(*gear, LevelOf(state, gearId));
}
