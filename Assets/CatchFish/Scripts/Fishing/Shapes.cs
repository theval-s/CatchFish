using UnityEngine;

/// <summary>
/// PLACEHOLDER for graphics to create fish from code for now
/// until we make prefabs with set sprites and etc3
/// </summary>
public static class Shapes
{
    private static Sprite _square;
    private static Material _material;

    private static Sprite Square
    {
        get
        {
            if (_square == null)
            {
                var texture = Texture2D.whiteTexture;
                _square = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), texture.width);
            }

            return _square;
        }
    }

    /// <summary>Unlit, so placeholder colors show exactly as set regardless of 2D lights.</summary>
    private static Material Material
    {
        get
        {
            if (_material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                _material = new Material(shader);
            }

            return _material;
        }
    }

    public static SpriteRenderer CreateBlock(string name, Color color, Vector2 size, Transform parent = null,
        int sortingOrder = 0)
    {
        //TODO: this needs to be removed
        var block = new GameObject(name);
        block.transform.SetParent(parent, false);
        block.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = Square;
        renderer.sharedMaterial = Material;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }
}
