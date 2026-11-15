using System;
using VContainer.Unity;

/// <summary>Starts fishing runs from the hub (or offers a refill without energy) and shows the "!" on the Quests tab.</summary>
public class HubMenuPresenter : IStartable, IDisposable
{
    private readonly HubMenuView _view;
    private readonly FishingService _fishing;
    private readonly QuestService _quests;
    private readonly EnergyService _energy;
    private readonly EnergyView _energyView;
    private readonly SceneLoader _scenes;
    private bool _starting;

    public HubMenuPresenter(HubMenuView view, FishingService fishing, QuestService quests, EnergyService energy,
        EnergyView energyView, SceneLoader scenes)
    {
        _view = view;
        _fishing = fishing;
        _quests = quests;
        _energy = energy;
        _energyView = energyView;
        _scenes = scenes;
    }

    public void Start()
    {
        _view.SetFirstRunMode(_fishing.IsFirstRun);
        _view.GoFishingClicked += OnGoFishing;
        _quests.Changed += RefreshBadge;
        RefreshBadge();
    }

    public void Dispose()
    {
        _view.GoFishingClicked -= OnGoFishing;
        _quests.Changed -= RefreshBadge;
    }

    // Clicks are ignored while the backend starts the run; after that the loading screen blocks input.
    private async void OnGoFishing()
    {
        if (_starting)
        {
            return;
        }

        if (!_energy.CanStartRun)
        {
            _energyView.Open();
            return;
        }

        _starting = true;
        var started = await _fishing.TryStartRunAsync();
        _starting = false;

        if (started)
        {
            _ = _scenes.LoadAsync(SceneNames.Fishing);
        }
    }

    private void RefreshBadge() => _view.SetQuestsBadge(_quests.HasCompletable);
}
