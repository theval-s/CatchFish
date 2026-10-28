using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One gear upgrade in the tackle shop. The Button's OnClick calls <see cref="Buy"/>.</summary>
public class GearUpgradeItemView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    public event Action BuyClicked;

    public void Show(string text, bool canBuy)
    {
        label.text = text;
        button.interactable = canBuy;
    }

    // Button OnClick
    public void Buy() => BuyClicked?.Invoke();
}
