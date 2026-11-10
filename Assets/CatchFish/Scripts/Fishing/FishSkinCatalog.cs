using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>How a fish looks. Stays on the client: the server config only knows the fish id.</summary>
[Serializable]
public class FishSkin
{
    public string fishId;
    [Tooltip("Drawn facing right. Set the image's Pixels Per Unit to its height in pixels: the fish config's size then sets " +
             "how tall it is in the world. Leave empty for the placeholder square.")]
    public Sprite sprite;
    [Tooltip("Tints the sprite; white shows the art as drawn.")]
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
