using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The upgrade popup of a home object: its level, picture, progress, and the Upgrade, gem and Level up
/// buttons. Opening and closing is pure UI.
/// </summary>
public class HomeObjectView : MonoBehaviour
{
    [SerializeField] private GameObject popup;
    [SerializeField] private HomeSkinCatalog skins;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image picture;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text questsText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeLabel;
    [SerializeField] private Button gemsButton;
    [SerializeField] private TMP_Text gemsLabel;
    [SerializeField] private Button levelUpButton;

    public event Action UpgradeClicked;
    public event Action GemsClicked;
    public event Action LevelUpClicked;

    private void Awake() => Close();

    public void Show(string objectId, int level, string title, string levelLine, string description, string progress)
    {
        titleText.text = title;
        levelText.text = levelLine;
        descriptionText.text = description;
        progressText.text = progress;
        picture.sprite = skins.GetSprite(objectId, level);
        picture.color = skins.GetColor(objectId);
    }

    /// <summary>Null text hides that part.</summary>
    public void ShowActions(string upgradeText, bool canUpgrade, string gemsText, bool canUseGems, string questsLine,
        bool showLevelUp, bool canLevelUp)
    {
        upgradeButton.gameObject.SetActive(upgradeText != null);
        upgradeLabel.text = upgradeText;
        upgradeButton.interactable = canUpgrade;
        gemsButton.gameObject.SetActive(gemsText != null);
        gemsLabel.text = gemsText;
        gemsButton.interactable = canUseGems;
        questsText.gameObject.SetActive(questsLine != null);
        questsText.text = questsLine;
        levelUpButton.gameObject.SetActive(showLevelUp);
        levelUpButton.interactable = canLevelUp;
    }

    // Button OnClick methods below.

    public void Open() => popup.SetActive(true);

    public void Close() => popup.SetActive(false);

    public void Upgrade() => UpgradeClicked?.Invoke();

    public void PayWithGems() => GemsClicked?.Invoke();

    public void LevelUp() => LevelUpClicked?.Invoke();
}
