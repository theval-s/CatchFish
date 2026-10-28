using VContainer;
using VContainer.Unity;

/// <summary>
/// The app-wide DI scope. Created by VContainerSettings before the first scene and kept
/// for the whole session; every scene scope uses it as its parent.
/// </summary>
public class RootScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Infrastructure. Swap LocalApiClient for the real server client here.
        builder.Register<LocalApiClient>(Lifetime.Singleton).As<IApiClient>();
        builder.Register<ConfigService>(Lifetime.Singleton);
        builder.Register<ServerTimeService>(Lifetime.Singleton);
        builder.Register<ProfileSyncService>(Lifetime.Singleton);
        builder.Register<SceneLoader>(Lifetime.Singleton);

        // Domain
        builder.Register<ResourceService>(Lifetime.Singleton);
        builder.Register<UpgradeService>(Lifetime.Singleton);
        builder.Register<FishingService>(Lifetime.Singleton);
        builder.Register<QuestService>(Lifetime.Singleton);
    }
}
