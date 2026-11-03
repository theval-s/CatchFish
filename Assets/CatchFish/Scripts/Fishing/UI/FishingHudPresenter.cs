using System;
using UnityEngine;
using VContainer.Unity;

/// <summary>Keeps the run HUD in sync with the minigame every frame.</summary>
public class FishingHudPresenter : IStartable, ITickable, IDisposable
{
    private readonly FishingHudView _view;
    private readonly FishingController _controller;

    public FishingHudPresenter(FishingHudView view, FishingController controller)
    {
        _view = view;
        _controller = controller;
    }

    public void Start() => _controller.FishShot += OnFishShot;

    public void Dispose() => _controller.FishShot -= OnFishShot;

    public void Tick()
    {
        var session = _controller.Session;
        if (session == null)
        {
            return;
        }

        var running = session.Phase != FishingPhase.Results;
        _view.SetVisible(running);
        if (!running)
        {
            return;
        }

        var hook = _controller.Hook;
        _view.Show(PhaseHint(session.Phase), hook.Depth, session.MaxDepth, Mathf.Max(0, hook.HitsLeft),
            hook.Caught.Count, hook.Capacity, session.Earnings);
    }

    private void OnFishShot(Vector2 worldPosition, int value) =>
        _view.ShowPopup(worldPosition, value >= 0 ? $"+{value}" : $"-{-value}");

    private static string PhaseHint(FishingPhase phase)
    {
        switch (phase)
        {
            case FishingPhase.Descent: return "Dodge the fish!";
            case FishingPhase.Ascent: return "Catch the fish!";
            default: return "Shoot them!";
        }
    }
}
