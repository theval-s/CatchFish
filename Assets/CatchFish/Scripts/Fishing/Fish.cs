using UnityEngine;

public enum FishState
{
    Swimming,
    Hooked,
    Airborne,
}

/// <summary>
/// A single fish. Swims left/right and wraps around the screen edges; after the catch
/// is thrown into the air it flies on a simple gravity arc.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Fish : MonoBehaviour
{
    private float _direction;
    private float _halfWidth;
    private Vector2 _velocity;
    private float _gravity;

    public FishType Type { get; private set; }
    public FishState State { get; private set; }
    public float Radius => Type.Size * 0.5f;
    public Vector2 Position => transform.position;

    /// <summary>Set after the fish bumps the sinking hook, so one fish can't hit it twice.</summary>
    public bool HasHitHook { get; set; }

    /// <summary>True once an airborne fish falls back into the water.</summary>
    public bool HasFallenIntoWater => State == FishState.Airborne && _velocity.y < 0f && transform.position.y < 0f;

    private void Update()
    {
        switch (State)
        {
            case FishState.Swimming:
                Swim();
                break;
            case FishState.Airborne:
                Fly();
                break;
        }
    }

    public void Init(FishType type, FishSkin skin, float direction, float halfWidth)
    {
        Type = type;
        _direction = Mathf.Sign(direction);
        _halfWidth = halfWidth;
        State = FishState.Swimming;

        // TODO: drop the stretching once fish have real sprites
        transform.localScale = new Vector3(type.Size * 1.5f, type.Size, 1f);
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (skin.sprite != null)
        {
            spriteRenderer.sprite = skin.sprite;
        }

        spriteRenderer.color = skin.color;
    }

    public void AttachToHook(Transform hook, Vector3 localOffset)
    {
        State = FishState.Hooked;
        transform.SetParent(hook, true);
        transform.localPosition = localOffset;
    }

    public void Launch(Vector2 from, Vector2 velocity, float gravity)
    {
        State = FishState.Airborne;
        transform.SetParent(null, true);
        transform.position = from;
        _velocity = velocity;
        _gravity = gravity;
    }

    private void Swim()
    {
        var span = _halfWidth + Radius;
        var position = transform.position;
        position.x += _direction * Type.SwimSpeed * Time.deltaTime;

        // currently basic wrap around, probably needs changing
        if (position.x > span)
        {
            position.x -= 2f * span;
        }
        else if (position.x < -span)
        {
            position.x += 2f * span;
        }

        transform.position = position;
    }

    private void Fly()
    {
        _velocity.y += _gravity * Time.deltaTime;
        transform.position += (Vector3)(_velocity * Time.deltaTime);
    }
}
