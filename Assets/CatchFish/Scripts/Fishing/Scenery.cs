using UnityEngine;

/// <summary>
/// Placeholder world
/// </summary>
public class Scenery : MonoBehaviour
{
    [SerializeField] private Color skyColor = new Color(0.55f, 0.80f, 0.95f);
    [SerializeField] private Color shallowWater = new Color(0.10f, 0.45f, 0.70f);
    [SerializeField] private Color deepWater = new Color(0.02f, 0.04f, 0.12f);
    [SerializeField] private float darkestDepth = 300f;
    [SerializeField] private float maxDepth = 1000f;
    [SerializeField] private float stripeSpacing = 5f;

    private Camera _camera;
    private SpriteRenderer _water;

    private void Awake()
    {
        _camera = Camera.main;
        _camera.backgroundColor = skyColor;

        _water = Shapes.CreateBlock("Water", shallowWater, new Vector2(40f, maxDepth), transform, -10);
        _water.transform.position = new Vector3(0f, -maxDepth * 0.5f, 0f);

        for (var depth = stripeSpacing; depth < maxDepth; depth += stripeSpacing)
        {
            var stripe = Shapes.CreateBlock("Depth Stripe", new Color(1f, 1f, 1f, 0.05f), new Vector2(40f, 0.05f), transform, -9);
            stripe.transform.position = new Vector3(0f, -depth, 0f);
        }

        var boat = Shapes.CreateBlock("Boat", new Color(0.55f, 0.30f, 0.15f), new Vector2(2.4f, 0.5f), transform, 4);
        boat.transform.position = new Vector3(-0.4f, 0.2f, 0f);

        var fisherman = Shapes.CreateBlock("Fisherman", new Color(0.95f, 0.85f, 0.70f), new Vector2(0.4f, 0.8f), transform, 4);
        fisherman.transform.position = new Vector3(-0.2f, 0.85f, 0f);
    }

    private void LateUpdate()
    {
        // Water gets darker the deeper the camera goes.
        var depth = Mathf.Max(0f, -_camera.transform.position.y);
        _water.color = Color.Lerp(shallowWater, deepWater, depth / darkestDepth);
    }
}
