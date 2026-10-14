using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One upgrade in the shop. Which upgrade it sells is picked in the inspector;
/// the Button's OnClick calls <see cref="Buy"/>.
/// </summary>
public class UpgradeButton : MonoBehaviour
{
    [SerializeField] private UpgradeType upgradeType;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    private GameManager _game;

    public void Init(GameManager game) => _game = game;

    public void Buy() => _game.TryBuyUpgrade(upgradeType);

    public void Refresh(Progression progression)
    {
        var upgrade = progression.GetUpgrade(upgradeType);
        if (upgrade.IsMaxed)
        {
            label.text = $"{upgrade.Title}: {upgrade.Value:0.#}{upgrade.Unit} (max)";
            button.interactable = false;
            return;
        }

        label.text = $"{upgrade.Title}: {upgrade.Value:0.#} → {upgrade.NextValue:0.#}{upgrade.Unit} (${upgrade.NextCost})";
        button.interactable = progression.Money >= upgrade.NextCost;
    }
}
