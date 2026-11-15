using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>How a home object looks at each level. Stays on the client: the config only knows the object id.</summary>
[Serializable]
public class HomeSkin
{
    public string objectId;
    [Tooltip("One sprite per level, from level 0. Missing levels use the last sprite before them, or the placeholder.")]
    public Sprite[] levelSprites;
    [Tooltip("Tints the sprite; white shows the art as drawn.")]
    public Color color = Color.white;
}

/// <summary>Maps home object ids from the config to a sprite per level.</summary>
[CreateAssetMenu(menuName = "CatchFish/Home Skin Catalog")]
public class HomeSkinCatalog : ScriptableObject
{
    [SerializeField] private Sprite placeholder;
    [SerializeField] private List<HomeSkin> skins = new List<HomeSkin>();

    public Sprite GetSprite(string objectId, int level)
    {
        var skin = Find(objectId);
        if (skin == null || skin.levelSprites == null)
        {
            return placeholder;
        }

        for (var i = Mathf.Min(level, skin.levelSprites.Length - 1); i >= 0; i--)
        {
            if (skin.levelSprites[i] != null)
            {
                return skin.levelSprites[i];
            }
        }

        return placeholder;
    }

    public Color GetColor(string objectId)
    {
        var skin = Find(objectId);
        return skin != null ? skin.color : Color.white;
    }

    private HomeSkin Find(string objectId)
    {
        foreach (var skin in skins)
        {
            if (skin.objectId == objectId)
            {
                return skin;
            }
        }

        return null;
    }
}
