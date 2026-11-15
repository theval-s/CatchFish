using System;
using System.Collections.Generic;
using System.Linq;
using VContainer.Unity;

/// <summary>Shows the home objects and runs their upgrade popup: steps, gems for the rest, quest-gated level-ups.</summary>
public class HomePresenter : IStartable, IDisposable
{
    private readonly HomeView _view;
    private readonly HomeObjectView _popup;
    private readonly HomeService _home;
    private readonly ResourceService _resources;
    private readonly FishingService _fishing;
    private readonly Dictionary<HomeObjectData, HomeObjectButtonView> _items = new Dictionary<HomeObjectData, HomeObjectButtonView>();
    private HomeObjectData _selected;

    public HomePresenter(HomeView view, HomeObjectView popup, HomeService home, ResourceService resources,
        FishingService fishing)
    {
        _fishing = fishing;
        _view = view;
        _popup = popup;
        _home = home;
        _resources = resources;
    }

    public void Start()
    {
        foreach (var data in _home.Objects)
        {
            var item = _view.AddObject(data.Id);
            item.Clicked += () => Select(data);
            _items[data] = item;
        }

        _popup.UpgradeClicked += OnUpgrade;
        _popup.GemsClicked += OnPayWithGems;
        _popup.LevelUpClicked += OnLevelUp;
        _home.Changed += Refresh;
        _resources.Changed += Refresh;
        Refresh();
    }

    public void Dispose()
    {
        _popup.UpgradeClicked -= OnUpgrade;
        _popup.GemsClicked -= OnPayWithGems;
        _popup.LevelUpClicked -= OnLevelUp;
        _home.Changed -= Refresh;
        _resources.Changed -= Refresh;
    }

    private void Select(HomeObjectData data)
    {
        // Before the first run the hub is about one thing: "Go fishing".
        if (_fishing.IsFirstRun)
        {
            return;
        }

        _selected = data;
        ShowSelected();
        _popup.Open();
    }

    private void OnUpgrade() => _ = _home.TryUpgradeAsync(_selected, false);

    private void OnPayWithGems() => _ = _home.TryUpgradeAsync(_selected, true);

    private void OnLevelUp() => _ = _home.TryLevelUpAsync(_selected);

    private void Refresh()
    {
        foreach (var pair in _items)
        {
            var level = _home.Get(pair.Key).Level;
            pair.Value.Show(pair.Key.Title, LevelLine(pair.Key), level);
        }

        if (_selected != null)
        {
            ShowSelected();
        }
    }

    private void ShowSelected()
    {
        var data = _selected;
        var state = _home.Get(data);
        var level = _home.CurrentLevel(data);
        var maxed = _home.IsMaxed(data);
        _popup.Show(data.Id, state.Level, data.Title, LevelLine(data), level.Description,
            maxed ? "Top level" : $"Progress: {state.Steps}/{level.RequiredProgressSteps}");

        if (maxed)
        {
            _popup.ShowActions(null, false, null, false, null, false, false);
            return;
        }

        var stepsDone = _home.StepsDone(data);
        var missing = _home.MissingQuests(data);
        var gems = _home.GemsForShortfall(data);
        _popup.ShowActions(
            stepsDone ? null : $"Upgrade: {NumberFormat.Short(level.ProgressCost)} LR",
            _home.CanUpgrade(data),
            !stepsDone && gems > 0 ? $"Pay the rest: {gems} gems" : null,
            _home.CanUpgradeWithGems(data),
            stepsDone && missing.Count > 0 ? "Needs quests:\n" + string.Join("\n", missing.Select(quest => quest.Title)) : null,
            stepsDone,
            _home.CanLevelUp(data));
    }

    private string LevelLine(HomeObjectData data) =>
        $"Level {_home.Get(data).Level + 1}: {_home.CurrentLevel(data).Title}";
}
