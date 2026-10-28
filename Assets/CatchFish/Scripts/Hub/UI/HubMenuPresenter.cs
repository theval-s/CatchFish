using System;
using VContainer.Unity;

/// <summary>Starts fishing runs from the hub and shows the "!" on the Quests tab.</summary>
public class HubMenuPresenter : IStartable, IDisposable
{
    private readonly HubMenuView _view;
    private readonly FishingService _fishing;
    private readonly QuestService _quests;
    private readonly SceneLoader _scenes;

    public HubMenuPresenter(HubMenuView view, FishingService fishing, QuestService quests, SceneLoader scenes)
    {
        _view = view;
        _fishing = fishing;
        _quests = quests;
        _scenes = scenes;
    }

    public void Start()
    {
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
        _fishing.StartRun();
        _ = _scenes.LoadAsync(SceneNames.Fishing);
    }

    private void RefreshBadge() => _view.SetQuestsBadge(_quests.HasCompletable);
}
