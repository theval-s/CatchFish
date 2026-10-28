using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>DI scope of the Fishing scene: the minigame objects and the run HUD.</summary>
public class FishingScope : LifetimeScope
{
    [SerializeField] private FishingController controller;
    [SerializeField] private FishSpawner spawner;
    [SerializeField] private FishingHudView hudView;
    [SerializeField] private FishingResultsView resultsView;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(controller);
        builder.RegisterComponent(spawner);
        builder.RegisterComponent(hudView);
        builder.RegisterComponent(resultsView);

        builder.RegisterEntryPoint<FishingHudPresenter>();
        builder.RegisterEntryPoint<FishingResultsPresenter>();
    }
}
