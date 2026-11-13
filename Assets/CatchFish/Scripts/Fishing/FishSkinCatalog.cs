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

    [Header("Hook attachment")]
    [Tooltip("Mouth point on the unflipped sprite canvas: (0, 0) is bottom-left, (1, 1) is top-right.")]
    [SerializeField] private Vector2 mouthPosition = new Vector2(0.9f, 0.5f);
    [Tooltip("Z rotation in degrees that makes the unflipped art head-up. Use 90 for right-facing fish, " +
             "or 0 for an already-upright creature like the jelly.")]
    [SerializeField] private float headUpAngle = 90f;

    public Vector2 MouthPosition => mouthPosition;
    public float HeadUpAngle => headUpAngle;
}

/// <summary>Maps fish ids from the config to sprites, colors and mouth attachment points.</summary>
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
