using VContainer;
using VContainer.Unity;

/// <summary>DI scope of the Bootstrap scene: only runs <see cref="BootstrapFlow"/>.</summary>
public class BootstrapScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<BootstrapFlow>();
    }
}
