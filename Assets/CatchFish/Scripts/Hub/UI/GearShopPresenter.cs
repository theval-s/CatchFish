using System;
using System.Collections.Generic;
using VContainer.Unity;

/// <summary>Fills the tackle shop from the config and buys upgrades with dirty rubles.</summary>
public class GearShopPresenter : IStartable, IDisposable
{
    private readonly GearShopView _view;
    private readonly ConfigService _config;
    private readonly UpgradeService _upgrades;
    private readonly ResourceService _resources;
    private readonly Dictionary<string, GearUpgradeItemView> _items = new Dictionary<string, GearUpgradeItemView>();

    public GearShopPresenter(GearShopView view, ConfigService config, UpgradeService upgrades,
        ResourceService resources)
    {
        _view = view;
        _config = config;
        _upgrades = upgrades;
        _resources = resources;
    }

    public void Start()
    {
        foreach (var gear in _config.Config.Gear)
        {
            var gearId = gear.Id;
            var item = _view.AddItem();
            item.BuyClicked += () => _upgrades.TryBuy(gearId);
            _items[gearId] = item;
        }

        _upgrades.Changed += Refresh;
        _resources.Changed += Refresh;
        Refresh();
    }

    public void Dispose()
    {
        _upgrades.Changed -= Refresh;
        _resources.Changed -= Refresh;
    }

    private void Refresh()
    {
        foreach (var pair in _items)
        {
            var gearId = pair.Key;
            var gear = _config.GetGear(gearId);
            var value = _upgrades.Value(gearId);

            if (_upgrades.IsMaxed(gearId))
            {
                pair.Value.Show($"{gear.Title}: {value:0.#}{gear.Unit} (max)", false);
                continue;
            }

            pair.Value.Show(
                $"{gear.Title}: {value:0.#} → {_upgrades.NextValue(gearId):0.#}{gear.Unit} ({_upgrades.NextCost(gearId)} DR)",
                _upgrades.CanBuy(gearId));
        }
    }
}
