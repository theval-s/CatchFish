using System;
using System.Collections.Generic;

/// <summary>
/// Open quests: checks if they can be completed with the player's resources,
/// spends the resources on completion and opens the quests it unlocks.
/// </summary>
public class QuestService : IDisposable
{
    private readonly ConfigService _config;
    private readonly ProfileSyncService _profile;
    private readonly ResourceService _resources;
    private readonly List<QuestData> _active = new List<QuestData>();

    public QuestService(ConfigService config, ProfileSyncService profile, ResourceService resources)
    {
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

    public bool TryComplete(QuestData quest)
    {
        if (!Progress.Active.Contains(quest.Id) || !_resources.TrySpend(quest.Requirements))
        {
            return false;
        }

        Progress.Active.Remove(quest.Id);
        Progress.Completed.Add(quest.Id);
        foreach (var id in quest.UnlocksId)
        {
            if (!Progress.Active.Contains(id) && !Progress.Completed.Contains(id))
            {
                Progress.Active.Add(id);
            }
        }

        _profile.Save();
        Changed?.Invoke();
        return true;
    }

    private void OnResourcesChanged() => Changed?.Invoke();
}
