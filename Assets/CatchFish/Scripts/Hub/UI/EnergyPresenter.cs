using System;
using VContainer.Unity;

/// <summary>Keeps the energy meter and its countdown current and refills energy for gems.</summary>
public class EnergyPresenter : IStartable, ITickable, IDisposable
{
    private readonly EnergyView _view;
    private readonly EnergyService _energy;
    private readonly ResourceService _resources;
    private int _shownAmount = -1;
    private int _shownSeconds = -1;

    public EnergyPresenter(EnergyView view, EnergyService energy, ResourceService resources)
    {
        _view = view;
        _energy = energy;
        _resources = resources;
    }

    public void Start()
    {
        _view.RefillClicked += OnRefill;
        _energy.Changed += Refresh;
        _resources.Changed += Refresh;
        Refresh();
    }

    public void Dispose()
    {
        _view.RefillClicked -= OnRefill;
        _energy.Changed -= Refresh;
        _resources.Changed -= Refresh;
    }

    public void Tick()
    {
        if (_energy.Current != _shownAmount || (int)_energy.UntilNext.TotalSeconds != _shownSeconds)
        {
            Refresh();
        }
    }

    private async void OnRefill()
    {
        if (await _energy.TryRefillAsync())
        {
            _view.Close();
        }
    }

    private void Refresh()
    {
        _shownAmount = _energy.Current;
        _shownSeconds = (int)_energy.UntilNext.TotalSeconds;
        var countdown = Countdown(_shownSeconds);
        _view.Show($"{_shownAmount}/{_energy.Max}", _energy.IsFull ? "Full" : countdown);

        var text = _energy.IsFull
            ? $"Energy {_shownAmount}/{_energy.Max}\nEnergy is full."
            : $"Energy {_shownAmount}/{_energy.Max}\nNext point in {countdown}\nA run costs {_energy.RunCost}.";
        _view.ShowPopup(text, $"Refill: {_energy.RefillGems} gems", _energy.CanRefill);
    }

    private static string Countdown(int seconds)
    {
        var left = TimeSpan.FromSeconds(Math.Max(seconds, 0));
        return left.TotalHours >= 1 ? $"{(int)left.TotalHours}h {left.Minutes:00}m" : $"{left.Minutes}:{left.Seconds:00}";
    }
}
