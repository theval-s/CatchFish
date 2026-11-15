#include "GameRules.h"

#include <algorithm>
#include <cctype>
#include <cmath>
#include <iterator>
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

    const char* const FishKinds[] = {"", "bomb", "eel", "shark"};
    const char* const Shark = "shark";

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

    // Same arithmetic as the client's CostAt: double power of the float growth,
    // and nearbyint because C#'s Math.Round rounds halves to even.
    int ScaledCost(int baseCost, float growth, int level)
    {
        return static_cast<int>(std::nearbyint(baseCost * std::pow(static_cast<double>(growth), level)));
    }

    int CostAt(const GearUpgradeData& gear, int level)
    {
        return ScaledCost(gear.baseCost, gear.costGrowth, level);
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
    Refresh(state, now);
    state.savedAt = FormatTime(now);
    return state;
}

bool GameRules::Refresh(GameState& state, Clock::time_point now) const
{
    const auto energyChanged = SettleEnergy(state, now);
    const auto schoolChanged = SettleSchool(state, now);

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

    return progress.active != before || energyChanged || schoolChanged;
}

bool GameRules::SettleSchool(GameState& state, Clock::time_point now) const
{
    auto& school = state.school;
    if (school.lastTransactionAt.empty())
    {
        school.lastTransactionAt = FormatTime(now);
        return true;
    }

    const auto since = ParseTime(school.lastTransactionAt);
    const auto minutes = std::chrono::duration_cast<std::chrono::minutes>(now - since).count();
    if (minutes <= 0)
    {
        return false;
    }

    long long launderRate = 0;
    long long repRate = 0;
    for (const auto& data : _config.school.classes)
    {
        const auto found = school.classes.find(data.id);
        if (found != school.classes.end() && found->second.hasTutor)
        {
            launderRate += data.launderBase + static_cast<long long>(data.launderPerStudent) * found->second.students;
            repRate += static_cast<long long>(data.repPerStudent) * found->second.students;
        }
    }

    // Only the dirty money the player has can be laundered.
    const auto counted = std::min<long long>(minutes, _config.school.offlineCapMinutes);
    auto& player = state.player;
    const auto laundered = static_cast<int>(std::min<long long>(player.dirtyRubles, launderRate * counted));
    player.dirtyRubles -= laundered;
    player.launderedRubles += laundered;
    player.reputation += static_cast<int>(repRate * counted);

    school.lastTransactionAt = counted < minutes ? FormatTime(now) : FormatTime(since + std::chrono::minutes(minutes));
    return true;
}

void GameRules::HireTutor(GameState& state, const std::string& classId) const
{
    const auto& data = GetClass(classId);
    auto& schoolClass = state.school.classes[classId];
    if (schoolClass.hasTutor)
    {
        throw ApiError(409, "tutor_hired", classId);
    }

    Spend(state.player, data.tutorCost);
    schoolClass.hasTutor = true;
}

void GameRules::AddStudent(GameState& state, const std::string& classId) const
{
    const auto& data = GetClass(classId);
    auto& schoolClass = state.school.classes[classId];
    if (!schoolClass.hasTutor)
    {
        throw ApiError(409, "no_tutor", classId);
    }
    if (schoolClass.students >= data.maxStudents)
    {
        throw ApiError(409, "class_full", classId);
    }

    std::map<std::string, int> cost;
    for (const auto& [id, amount] : data.studentCost)
    {
        cost[id] = ScaledCost(amount, data.studentCostGrowth, schoolClass.students);
    }

    Spend(state.player, cost);
    schoolClass.students++;
}

bool GameRules::SettleEnergy(GameState& state, Clock::time_point now) const
{
    const auto& config = _config.energy;
    auto& energy = state.energy;
    if (energy.updatedAt.empty())
    {
        energy.amount = config.max;
        energy.updatedAt = FormatTime(now);
        return true;
    }

    // The timer doesn't run while full; spending from full restarts it.
    if (energy.amount >= config.max)
    {
        return false;
    }

    const auto since = ParseTime(energy.updatedAt);
    const auto interval = std::chrono::minutes(config.regenMinutes);
    const auto gained = std::max<long long>(0, (now - since) / interval);
    if (gained == 0)
    {
        return false;
    }

    if (energy.amount + gained >= config.max)
    {
        energy.amount = config.max;
        energy.updatedAt = FormatTime(now);
    }
    else
    {
        energy.amount += static_cast<int>(gained);
        energy.updatedAt = FormatTime(since + gained * interval);
    }

    return true;
}

void GameRules::RefillEnergy(GameState& state, Clock::time_point now) const
{
    auto& energy = state.energy;
    if (energy.amount >= _config.energy.max)
    {
        throw ApiError(409, "energy_full", "energy is already full");
    }

    Spend(state.player, {{"gems", _config.energy.refillGems}});
    energy.amount = _config.energy.max;
    energy.updatedAt = FormatTime(now);
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

void GameRules::UpgradeHome(GameState& state, const std::string& objectId, bool useGems) const
{
    const auto& data = GetHomeObject(objectId);
    auto& object = state.home[objectId];
    if (object.level >= static_cast<int>(data.levels.size()) - 1)
    {
        throw ApiError(409, "home_maxed", objectId);
    }

    const auto& level = data.levels[object.level];
    if (object.steps >= level.requiredProgressSteps)
    {
        throw ApiError(409, "steps_full", objectId);
    }

    // Short of laundered rubles, gems can pay the rest.
    auto& player = state.player;
    const auto missing = level.progressCost - player.launderedRubles;
    if (useGems && missing > 0)
    {
        Spend(player, {{"lr", player.launderedRubles}, {"gems", (missing + _config.lrPerGem - 1) / _config.lrPerGem}});
    }
    else
    {
        Spend(player, {{"lr", level.progressCost}});
    }

    object.steps++;
    if (object.steps >= level.requiredProgressSteps && QuestsDone(state, level))
    {
        object.level++;
        object.steps = 0;
    }
}

void GameRules::LevelUpHome(GameState& state, const std::string& objectId) const
{
    const auto& data = GetHomeObject(objectId);
    auto& object = state.home[objectId];
    if (object.level >= static_cast<int>(data.levels.size()) - 1)
    {
        throw ApiError(409, "home_maxed", objectId);
    }

    const auto& level = data.levels[object.level];
    if (object.steps < level.requiredProgressSteps)
    {
        throw ApiError(409, "steps_not_full", objectId);
    }
    if (!QuestsDone(state, level))
    {
        throw ApiError(409, "quests_not_done", objectId);
    }

    object.level++;
    object.steps = 0;
}

bool GameRules::QuestsDone(const GameState& state, const HomeLevelData& level) const
{
    return std::all_of(level.prerequisiteQuests.begin(), level.prerequisiteQuests.end(),
        [&state](int id) { return Contains(state.quests.completed, id); });
}

void GameRules::StartRun(GameState& state, Clock::time_point now) const
{
    // TODO: refuse while the wanted level is too high, once it exists
    // Refresh has already regenerated the energy.
    auto& energy = state.energy;
    const auto cost = _config.energy.runCost;
    if (energy.amount < cost)
    {
        throw ApiError(409, "not_enough_energy",
            std::to_string(energy.amount) + " / " + std::to_string(cost));
    }
    if (energy.amount >= _config.energy.max)
    {
        energy.updatedAt = FormatTime(now);
    }
    energy.amount -= cost;

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
    const auto reachable = [&run](const FishType& fish) { return fish.minDepth <= run.maxDepth + DepthTolerance; };

    std::vector<std::pair<const FishType*, int>> shot;
    auto total = 0;
    for (const auto& [fishId, count] : result.shotFish)
    {
        const auto* fish = FindFish(fishId);
        if (fish == nullptr)
        {
            throw ApiError(400, "unknown_fish", fishId);
        }

        if (count < 0 || !reachable(*fish) || fish->kind == Shark)
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

    for (const auto& [fishId, count] : result.shockedFish)
    {
        const auto found = result.shotFish.find(fishId);
        if (count < 0 || count > (found != result.shotFish.end() ? found->second : 0))
        {
            throw ApiError(400, "invalid_run", "more shocked " + fishId + " than shot");
        }
    }

    auto& player = state.player;
    auto earnings = 0;
    if (!result.bittenBy.empty())
    {
        const auto* shark = FindFish(result.bittenBy);
        if (shark == nullptr)
        {
            throw ApiError(400, "unknown_fish", result.bittenBy);
        }
        if (shark->kind != Shark || !reachable(*shark) || total > 0)
        {
            throw ApiError(400, "invalid_run", "a shark takes the hook with the whole catch");
        }

        earnings = shark->value;
    }

    for (const auto& [fish, count] : shot)
    {
        const auto found = result.shockedFish.find(fish->id);
        const auto shocked = found != result.shockedFish.end() ? found->second : 0;
        earnings += fish->value * (count - shocked) + fish->value / 2 * shocked;
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
        if (std::find(std::begin(FishKinds), std::end(FishKinds), fish.kind) == std::end(FishKinds))
        {
            InvalidConfig("fish '" + fish.id + "' has kind '" + fish.kind + "'; use \"\", bomb, eel or shark");
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

    const auto& energy = _config.energy;
    if (energy.max <= 0 || energy.regenMinutes <= 0 || energy.runCost < 0 || energy.runCost > energy.max
        || energy.refillGems < 0)
    {
        InvalidConfig("energy needs max > 0, regenMinutes > 0, 0 <= runCost <= max and refillGems >= 0");
    }

    if (_config.school.offlineCapMinutes <= 0)
    {
        InvalidConfig("school.offlineCapMinutes must be above 0");
    }
    std::set<std::string> classIds;
    for (const auto& data : _config.school.classes)
    {
        const auto name = "school class '" + data.id + "'";
        if (data.id.empty() || !classIds.insert(data.id).second)
        {
            InvalidConfig(name + " needs a unique id");
        }
        for (const auto* costs : {&data.tutorCost, &data.studentCost})
        {
            for (const auto& [id, amount] : *costs)
            {
                if (!IsResource(id) || amount < 0)
                {
                    InvalidConfig(name + " costs " + std::to_string(amount) + " of '" + id + "'");
                }
            }
        }
        if (data.studentCostGrowth <= 0 || data.maxStudents < 0 || data.launderBase < 0 || data.launderPerStudent < 0
            || data.repPerStudent < 0)
        {
            InvalidConfig(name + " needs studentCostGrowth > 0 and no negative counts or rates");
        }
    }

    if (_config.lrPerGem <= 0)
    {
        InvalidConfig("lrPerGem must be above 0");
    }
    std::set<std::string> homeIds;
    for (const auto& object : _config.home)
    {
        const auto name = "home object '" + object.id + "'";
        if (object.id.empty() || !homeIds.insert(object.id).second)
        {
            InvalidConfig(name + " needs a unique id");
        }
        if (object.levels.empty())
        {
            InvalidConfig(name + " needs at least one level");
        }
        for (std::size_t i = 0; i + 1 < object.levels.size(); i++)
        {
            const auto& level = object.levels[i];
            const auto levelName = name + " level " + std::to_string(i);
            if (level.requiredProgressSteps <= 0 || level.progressCost < 0)
            {
                InvalidConfig(levelName + " needs requiredProgressSteps > 0 and progressCost >= 0");
            }
            for (const auto id : level.prerequisiteQuests)
            {
                if (FindQuest(id) == nullptr)
                {
                    InvalidConfig(levelName + " needs quest " + std::to_string(id) + ", which doesn't exist");
                }
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

const SchoolClassData& GameRules::GetClass(const std::string& id) const
{
    for (const auto& data : _config.school.classes)
    {
        if (data.id == id)
        {
            return data;
        }
    }

    throw ApiError(400, "unknown_class", id);
}

const HomeObjectData& GameRules::GetHomeObject(const std::string& id) const
{
    for (const auto& object : _config.home)
    {
        if (object.id == id)
        {
            return object;
        }
    }

    throw ApiError(400, "unknown_home_object", id);
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
