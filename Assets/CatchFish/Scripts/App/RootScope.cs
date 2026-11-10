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
    [Tooltip("On: the C++ server in Server/ (see Server/README.md). Off: LocalApiClient, the same rules on the device.")]
    [SerializeField] private bool useServer;
    [Tooltip("The hosted server: https://fish.thevals.ru:1337. A server on this machine: http://localhost:8080 (editor only).")]
    [SerializeField] private string serverUrl = "https://fish.thevals.ru:1337";

    protected override void Configure(IContainerBuilder builder)
    {
        // Infrastructure
        if (useServer)
        {
            builder.Register<HttpApiClient>(Lifetime.Singleton).As<IApiClient>().WithParameter("baseUrl", serverUrl);
        }
        else
        {
            builder.Register<LocalApiClient>(Lifetime.Singleton).As<IApiClient>();
        }

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
    }
}
