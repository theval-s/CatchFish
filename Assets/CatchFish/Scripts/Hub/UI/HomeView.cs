using UnityEngine;

/// <summary>The home objects in the Home tab, built from the config at runtime.</summary>
public class HomeView : MonoBehaviour
{
    [SerializeField] private HomeObjectButtonView itemPrefab;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private HomeSkinCatalog skins;

    public HomeObjectButtonView AddObject(string objectId)
    {
        var item = Instantiate(itemPrefab, itemContainer);
        item.SetSkin(skins, objectId);
        return item;
    }
}
