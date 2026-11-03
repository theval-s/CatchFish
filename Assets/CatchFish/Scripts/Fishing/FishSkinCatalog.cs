using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>How a fish looks. Stays on the client: the server config only knows the fish id.</summary>
[Serializable]
public class FishSkin
{
    public string fishId;
    [Tooltip("Leave empty to keep the prefab's sprite.")]
    public Sprite sprite;
    public Color color = Color.white;
}

/// <summary>Maps fish ids from the config to sprites and colors.</summary>
[CreateAssetMenu(menuName = "CatchFish/Fish Skin Catalog")]
public class FishSkinCatalog : ScriptableObject
{
    [SerializeField] private FishSkin fallback = new FishSkin { color = Color.magenta };
    [SerializeField] private List<FishSkin> skins = new List<FishSkin>();

    public FishSkin Get(string fishId)
    {
        foreach (var skin in skins)
        {
            if (skin.fishId == fishId)
            {
                return skin;
            }
        }

        return fallback;
    }
}
