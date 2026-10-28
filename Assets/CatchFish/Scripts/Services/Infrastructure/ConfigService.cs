using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>Loads the game config from the backend and looks things up in it.</summary>
public class ConfigService
{
    private readonly IApiClient _api;
    private readonly Dictionary<string, FishType> _fish = new Dictionary<string, FishType>();
    private readonly Dictionary<int, QuestData> _quests = new Dictionary<int, QuestData>();
    private readonly Dictionary<string, GearUpgradeData> _gear = new Dictionary<string, GearUpgradeData>();

    public ConfigService(IApiClient api)
    {
        _api = api;
    }

    public GameConfig Config { get; private set; }

    public async Task LoadAsync()
    {
        Config = await _api.LoadConfigAsync();

        _fish.Clear();
        foreach (var fish in Config.Fish)
        {
            _fish[fish.Id] = fish;
        }

        _quests.Clear();
        foreach (var quest in Config.Quests)
        {
            _quests[quest.Id] = quest;
        }

        _gear.Clear();
        foreach (var gear in Config.Gear)
        {
            _gear[gear.Id] = gear;
        }
    }

    public FishType GetFish(string id) => _fish.TryGetValue(id, out var fish) ? fish : null;

    public QuestData GetQuest(int id) => _quests.TryGetValue(id, out var quest) ? quest : null;

    public GearUpgradeData GetGear(string id) => _gear[id];
}
