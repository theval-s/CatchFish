using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>DI scope of the Hub scene: its views and their presenters.</summary>
public class HubScope : LifetimeScope
{
    [SerializeField] private CurrencyBarView currencyBar;
    [SerializeField] private HubMenuView menu;
    [SerializeField] private GearShopView gearShop;
    [SerializeField] private QuestsView quests;
    [SerializeField] private DailyRewardView dailyReward;
    [SerializeField] private EnergyView energy;
    [SerializeField] private DialogView introDialog;
    [SerializeField] private SchoolView school;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(currencyBar);
        builder.RegisterComponent(menu);
        builder.RegisterComponent(gearShop);
        builder.RegisterComponent(quests);
        builder.RegisterComponent(dailyReward);
        builder.RegisterComponent(energy);
        builder.RegisterComponent(introDialog);
        builder.RegisterComponent(school);

        builder.RegisterEntryPoint<CurrencyBarPresenter>();
        builder.RegisterEntryPoint<HubMenuPresenter>();
        builder.RegisterEntryPoint<GearShopPresenter>();
        builder.RegisterEntryPoint<QuestsPresenter>();
        builder.RegisterEntryPoint<DailyRewardPresenter>();
        builder.RegisterEntryPoint<EnergyPresenter>();
        builder.RegisterEntryPoint<IntroDialogPresenter>();
        builder.RegisterEntryPoint<SchoolPresenter>();
    }
}
