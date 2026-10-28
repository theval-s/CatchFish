using System;
using VContainer.Unity;

/// <summary>Updates the currency counters whenever the player's resources change.</summary>
public class CurrencyBarPresenter : IStartable, IDisposable
{
    private readonly CurrencyBarView _view;
    private readonly ResourceService _resources;

    public CurrencyBarPresenter(CurrencyBarView view, ResourceService resources)
    {
        _view = view;
        _resources = resources;
    }

    public void Start()
    {
        _resources.Changed += Refresh;
        Refresh();
    }

    public void Dispose() => _resources.Changed -= Refresh;

    private void Refresh() =>
        _view.Show(_resources.Get(ResourceIds.Gems), _resources.Get(ResourceIds.LaunderedRubles),
            _resources.Get(ResourceIds.DirtyRubles));
}
