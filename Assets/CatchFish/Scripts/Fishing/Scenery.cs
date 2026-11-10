using UnityEngine;

/// <summary>
/// The water around the hook: it darkens with depth and has faint depth stripes. The boat, the fisherman
/// and any sky or shore art live in Prefabs/Fishing/Surface.prefab, placed in the scene.
/// </summary>
public class Scenery : MonoBehaviour
{
    private const float Width = 40f;

    [Header("Art")]
    [Tooltip("Optional water texture, tiled over the whole depth and tinted from Shallow Water to Deep Water " +
             "(set both to white to show it as drawn). Import it with Mesh Type: Full Rect and Wrap Mode: Repeat. " +
             "Empty = a plain color.")]
    [SerializeField] private Sprite waterSprite;

    [Header("Colors")]
    [Tooltip("The camera's background, behind everything above the water.")]
    [SerializeField] private Color skyColor = new Color(0.55f, 0.80f, 0.95f);
    [SerializeField] private Color shallowWater = new Color(0.10f, 0.45f, 0.70f);
    [SerializeField] private Color deepWater = new Color(0.02f, 0.04f, 0.12f);
    [SerializeField] private Color stripeColor = new Color(1f, 1f, 1f, 0.05f);

    [Header("Depth")]
    [SerializeField] private float darkestDepth = 300f;
    [SerializeField] private float maxDepth = 1000f;
    [SerializeField] private float stripeSpacing = 5f;

    private Camera _camera;
    private SpriteRenderer _water;

    private void Awake()
    {
        _camera = Camera.main;
        _camera.backgroundColor = skyColor;

        _water = waterSprite != null
            ? CreateTiledWater()
            : Shapes.CreateBlock("Water", shallowWater, new Vector2(Width, maxDepth), transform, -10);
        _water.transform.position = new Vector3(0f, -maxDepth * 0.5f, 0f);

        for (var depth = stripeSpacing; depth < maxDepth; depth += stripeSpacing)
        {
            var stripe = Shapes.CreateBlock("Depth Stripe", stripeColor, new Vector2(Width, 0.05f), transform, -9);
            stripe.transform.position = new Vector3(0f, -depth, 0f);
        }
    }

    private void LateUpdate()
    {
        // Water gets darker the deeper the camera goes.
        var depth = Mathf.Max(0f, -_camera.transform.position.y);
        _water.color = Color.Lerp(shallowWater, deepWater, depth / darkestDepth);
    }

    // Tiled, a texture keeps its scale however deep the water goes; stretched, it would smear.
    private SpriteRenderer CreateTiledWater()
    {
        var water = new GameObject("Water").AddComponent<SpriteRenderer>();
        water.transform.SetParent(transform, false);
        water.sprite = waterSprite;
        water.drawMode = SpriteDrawMode.Tiled;
        water.size = new Vector2(Width, maxDepth);
        water.sortingOrder = -10;
        return water;
    }
}
