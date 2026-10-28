using System;
using VContainer.Unity;

/// <summary>Starts fishing runs from the hub and shows the "!" on the Quests tab.</summary>
public class HubMenuPresenter : IStartable, IDisposable
{
    private readonly HubMenuView _view;
    private readonly FishingService _fishing;
    private readonly QuestService _quests;
    private readonly SceneLoader _scenes;
    private readonly ProfileSyncService _profile;
    private bool _startingRun;

    public HubMenuPresenter(HubMenuView view, FishingService fishing, QuestService quests, SceneLoader scenes,
        ProfileSyncService profile)
    {
        _view = view;
        _fishing = fishing;
        _quests = quests;
        _scenes = scenes;
        _profile = profile;
    }

    public void Start()
    {
        _view.SetNewbieMode(_profile.State.Player.MatchesPlayed == 0);
        _view.GoFishingClicked += OnGoFishing;
        _quests.Changed += RefreshBadge;
        RefreshBadge();
    }

    public void Dispose()
    {
        _view.GoFishingClicked -= OnGoFishing;
        _quests.Changed -= RefreshBadge;
    }

    private void OnGoFishing()
    {
        if (_startingRun) return;
        _startingRun = true;
        _view.SetGoFishingEnabled(false);
        _fishing.StartRun();
        _ = _scenes.LoadAsync(SceneNames.Fishing);
    }

    private void RefreshBadge() => _view.SetQuestsBadge(_quests.HasCompletable);
}
