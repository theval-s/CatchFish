using UnityEngine;

/// <summary>The tackle shop list; items are created from the config by the presenter.</summary>
public class GearShopView : MonoBehaviour
{
    [SerializeField] private GearUpgradeItemView itemPrefab;
    [SerializeField] private Transform itemContainer;

    public GearUpgradeItemView AddItem() => Instantiate(itemPrefab, itemContainer);
}
