using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Open quests: checks if they can be completed with the player's resources
/// and asks the backend to complete them.
/// </summary>
public class QuestService : IDisposable
{
    private readonly IApiClient _api;
    private readonly ConfigService _config;
    private readonly ProfileSyncService _profile;
    private readonly ResourceService _resources;
    private readonly List<QuestData> _active = new List<QuestData>();

    public QuestService(IApiClient api, ConfigService config, ProfileSyncService profile, ResourceService resources)
    {
        _api = api;
        _config = config;
        _profile = profile;
        _resources = resources;

        // Any resource change (a fishing run, a purchase) can make a quest completable.
        _resources.Changed += OnResourcesChanged;
    }

    /// <summary>Raised when the list of quests or whether they can be completed may have changed.</summary>
    public event Action Changed;

    private QuestProgressState Progress => _profile.State.Quests;

    public IReadOnlyList<QuestData> ActiveQuests
    {
        get
        {
            _active.Clear();
            foreach (var id in Progress.Active)
            {
                var quest = _config.GetQuest(id);
                if (quest != null)
                {
                    _active.Add(quest);
                }
            }

            return _active;
        }
    }

    /// <summary>Shown as "!" on the Quests tab.</summary>
    public bool HasCompletable
    {
        get
        {
            foreach (var quest in ActiveQuests)
            {
                if (_resources.Has(quest.Requirements))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public void Dispose() => _resources.Changed -= OnResourcesChanged;

    public bool CanComplete(QuestData quest) => Progress.Active.Contains(quest.Id) && _resources.Has(quest.Requirements);

    /// <summary>The backend spends the requirements and opens the quests this one unlocks.</summary>
    public async Task<bool> TryCompleteAsync(QuestData quest)
    {
        if (!CanComplete(quest) || !await _profile.TryApplyAsync(_api.CompleteQuestAsync(quest.Id)))
        {
            return false;
        }

        Changed?.Invoke();
        return true;
    }

    private void OnResourcesChanged() => Changed?.Invoke();
}
