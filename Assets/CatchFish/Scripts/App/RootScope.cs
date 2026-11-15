using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// The app-wide DI scope. Created by VContainerSettings before the first scene and kept
/// for the whole session; every scene scope uses it as its parent.
/// </summary>
public class RootScope : LifetimeScope
{
    [SerializeField] private LoadingScreen loadingScreen;

    [Header("Backend")]
    [Tooltip("The hosted server: https://fish.thevals.ru:1337. A server on this machine: http://localhost:8080 (editor only).")]
    [SerializeField] private string serverUrl = "https://fish.thevals.ru:1337";

    [Header("Performance")]
    [Tooltip("Frames per second to aim for. Android caps the game at 30 unless this is set; a value above the " +
             "screen's refresh rate runs at the refresh rate (60 on a 60 Hz phone, 120 on a 120 Hz one).")]
    [SerializeField] private int targetFrameRate = 120;

    protected override void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
        base.Awake();
    }

    protected override void Configure(IContainerBuilder builder)
    {
        // Infrastructure
        builder.Register<HttpApiClient>(Lifetime.Singleton).As<IApiClient>().WithParameter("baseUrl", serverUrl);
        builder.Register<ConfigService>(Lifetime.Singleton);
        builder.Register<ServerTimeService>(Lifetime.Singleton);
        builder.Register<ProfileSyncService>(Lifetime.Singleton);
        builder.RegisterComponent(loadingScreen);
        builder.Register<SceneLoader>(Lifetime.Singleton);

        // Domain
        builder.Register<ResourceService>(Lifetime.Singleton);
        builder.Register<UpgradeService>(Lifetime.Singleton);
        builder.Register<FishingService>(Lifetime.Singleton);
        builder.Register<QuestService>(Lifetime.Singleton);
        builder.Register<DailyRewardService>(Lifetime.Singleton);
    }
}
