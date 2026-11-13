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
    [Header("Hooked pose")]
    [Tooltip("Seconds to ease into the hanging pose and react to hook movement.")]
    [SerializeField] private float hookedSmoothTime = 0.35f;
    [Tooltip("Maximum randomized resting tilt away from head-up, in degrees.")]
    [SerializeField] private float restingTilt = 10f;
    [Tooltip("Gentle sway amplitude in degrees; each fish has its own phase and frequency.")]
    [SerializeField] private float swayAngle = 5f;
    [Tooltip("Sway frequency range in cycles per second.")]
    [SerializeField] private Vector2 swayFrequency = new Vector2(0.4f, 0.7f);
    [Tooltip("Degrees of trailing lean per world unit per second of horizontal hook movement.")]
    [SerializeField] private float dragLean = 4f;
    [Tooltip("Maximum trailing lean in degrees.")]
    [SerializeField] private float maxDragLean = 23f;

    [Header("Airborne pose")]
    [Tooltip("Angular speed range in degrees per second; direction is chosen once per launch.")]
    [SerializeField] private Vector2 tumbleSpeed = new Vector2(25f, 55f);

    private float _direction;
    private float _halfWidth;
    private Vector2 _velocity;
    private float _gravity;
    private readonly FishSwimmingBehavior _swimming = new FishSwimmingBehavior();
    private readonly FishArmor _armor = new FishArmor();
    private SpriteRenderer _renderer;
    private Sprite _placeholderSprite;
    private bool _initialized;
    private Vector3 _baseScale;
    private Color _baseColor;
    private float _originY;
    private float _minY;
    private float _maxY;
    private float _wrapSpan;
    private float _radiusScale = 1f;
    private float _damageFlash;
    private Vector2 _mouthPosition;
    private float _headUpAngle;
    private Transform _hook;
    private Vector3 _hookOffset;
    private Vector3 _previousHookPosition;
    private float _hookedAngle;
    private float _hookedAngleVelocity;
    private float _restingAngle;
    private float _swayPhase;
    private float _swayFrequency;
    private float _angularVelocity;

    public FishType Type { get; private set; }
    public FishState State { get; private set; }
    public float Radius => Type.Size * 0.5f * _radiusScale;
    public Vector2 Position => transform.position;
    public int HitsLeft => _armor.HitsLeft;

    /// <summary>Set after the fish bumps the sinking hook, so one fish can't hit it twice.</summary>
    public bool HasHitHook { get; set; }

    /// <summary>True once an airborne fish falls back into the water.</summary>
    public bool HasFallenIntoWater => State == FishState.Airborne && _velocity.y < 0f && transform.position.y < 0f;

    private void Update()
    {
        if (State == FishState.Airborne)
        {
            Fly(Time.deltaTime);
            _damageFlash = Mathf.Max(0f, _damageFlash - Time.deltaTime);
            ApplyDamageVisual();
        }
    }

    private void LateUpdate()
    {
        UpdateHookedPose(Time.deltaTime);
    }

    public void Init(FishType type, FishSkin skin, float direction, float halfWidth,
        FishBehaviorSettings behavior = null)
    {
        transform.SetParent(null, true);
        transform.rotation = Quaternion.identity;
        Type = type;
        _direction = direction < 0f ? -1f : 1f;
        _halfWidth = halfWidth;
        State = FishState.Swimming;
        HasHitHook = false;
        _velocity = Vector2.zero;
        _gravity = 0f;
        _damageFlash = 0f;
        _radiusScale = 1f;
        _hook = null;
        _hookOffset = Vector3.zero;
        _previousHookPosition = Vector3.zero;
        _hookedAngle = 0f;
        _hookedAngleVelocity = 0f;
        _restingAngle = 0f;
        _swayPhase = 0f;
        _swayFrequency = 0f;
        _angularVelocity = 0f;
        _mouthPosition = skin.MouthPosition;
        _headUpAngle = skin.HeadUpAngle;
        behavior = behavior ?? new FishBehaviorSettings();
        _armor.Reset(behavior.AirborneHits);
        _swimming.Reset(behavior, _direction, () => Random.value);
        _originY = transform.position.y;
        // Reserve the largest body radius so inflation cannot cross the surface/depth limits.
        var depthRadius = Radius * (behavior.Pattern == FishSwimPattern.Inflation ? behavior.InflatedScale : 1f);
        _maxY = -Mathf.Max(0f, type.MinDepth) - depthRadius;
        _minY = -Mathf.Max(type.MinDepth, type.MaxDepth) + depthRadius;
        if (_minY > _maxY)
        {
            _minY = _maxY = -Mathf.Max(0f, (type.MinDepth + type.MaxDepth) * 0.5f);
        }
        _originY = Mathf.Clamp(_originY, _minY, _maxY);
        var position = transform.position;
        position.y = _originY;
        transform.position = position;

        _renderer = GetComponent<SpriteRenderer>();
        if (!_initialized)
        {
            _placeholderSprite = _renderer.sprite;
            _initialized = true;
        }

        if (skin.sprite != null)
        {
            // Real art keeps its own proportions (see FishSkin.sprite).
            _renderer.sprite = skin.sprite;
            transform.localScale = Vector3.one * type.Size;
        }
        else
        {
            // The placeholder square, stretched into a fish shape.
            _renderer.sprite = _placeholderSprite;
            transform.localScale = new Vector3(type.Size * 1.5f, type.Size, 1f);
        }

        // Art faces right; fish swimming left are mirrored.
        _renderer.flipX = _direction < 0f;
        _renderer.flipY = false;
        _baseScale = transform.localScale;
        _baseColor = skin.color;
        _renderer.color = _baseColor;
        var spriteHalfWidth = _renderer.sprite == null ? type.Size * 0.75f
            : _renderer.sprite.bounds.extents.x * _baseScale.x;
        // Keep wide canvases fully offscreen before wrapping, including the largest cue pose.
        var maxScale = behavior.Pattern == FishSwimPattern.Inflation ? behavior.InflatedScale : 1.08f;
        _wrapSpan = Mathf.Max(0.01f, _halfWidth + Mathf.Max(Radius, spriteHalfWidth) * maxScale);
    }

    public void AttachToHook(Transform hook, Vector3 localOffset)
    {
        CancelSwimming();
        State = FishState.Hooked;
        _hook = hook;
        _hookOffset = localOffset;
        _previousHookPosition = hook.TransformPoint(localOffset);
        _hookedAngle = transform.eulerAngles.z;
        _hookedAngleVelocity = 0f;
        var headUp = _renderer.flipX ? -_headUpAngle : _headUpAngle;
        if (_renderer.flipY)
        {
            headUp = 180f - headUp;
        }
        _restingAngle = headUp + Random.Range(-restingTilt, restingTilt);
        _swayPhase = Random.Range(0f, Mathf.PI * 2f);
        _swayFrequency = Random.Range(swayFrequency.x, swayFrequency.y);
        transform.SetParent(hook, true);
        PinMouth(_previousHookPosition);
    }

    public void Launch(Vector2 from, Vector2 velocity, float gravity)
    {
        CancelSwimming();
        State = FishState.Airborne;
        transform.SetParent(null, true);
        _hook = null;
        transform.position = from;
        _velocity = velocity;
        _gravity = gravity;
        _angularVelocity = Random.Range(tumbleSpeed.x, tumbleSpeed.y) * (Random.value < 0.5f ? -1f : 1f);
    }

    /// <summary>Called once by the controller after hook movement and before contact tests.</summary>
    public void Swim(float seconds, Vector2 hookPosition, bool hookActive)
    {
        if (State != FishState.Swimming)
        {
            return;
        }

        var position = transform.position;
        var delta = hookPosition - (Vector2)position;
        _swimming.Advance(seconds, Type.SwimSpeed, hookActive, delta.x, delta.y, out var moveX, out var moveY);
        position.x = FishSchoolLayout.Wrap(position.x + moveX, _wrapSpan);
        // Integrate lunge displacement, but derive oscillation from a fixed spawn origin (never cumulative).
        _originY = Mathf.Clamp(_originY + moveY, _minY, _maxY);
        position.y = Mathf.Clamp(_originY + _swimming.WaveOffset, _minY, _maxY);
        transform.position = position;
        _radiusScale = _swimming.Scale;
        _renderer.flipX = _swimming.FacingDirection < 0f;
        var warning = _swimming.Phase == FishSwimPhase.Warning;
        var pulse = warning ? Mathf.Sin(_swimming.WarningProgress * Mathf.PI * 3f) : 0f;
        transform.localScale = warning
            ? Vector3.Scale(_baseScale, new Vector3(0.82f - pulse * 0.04f, 1.06f + pulse * 0.02f, 1f))
            : _baseScale * _radiusScale;
        _renderer.color = warning ? Color.Lerp(_baseColor, new Color(1f, 0.45f, 0.2f), 0.65f) : _baseColor;
    }

    /// <summary>Returns true only on the lethal airborne hit; callers award the fish once.</summary>
    public bool TryShoot()
    {
        if (State != FishState.Airborne || _armor.HitsLeft <= 0)
        {
            return false;
        }

        var lethal = _armor.TryHit();
        _damageFlash = 0.14f;
        ApplyDamageVisual();
        return lethal;
    }

    private void CancelSwimming()
    {
        _swimming.Cancel();
        _radiusScale = 1f;
        _damageFlash = 0f;
        transform.localScale = _baseScale;
        _renderer.color = _baseColor;
    }

    private void UpdateHookedPose(float seconds)
    {
        if (State != FishState.Hooked || _hook == null || seconds <= 0f)
        {
            return;
        }

        // Sample after controller movement, including motion of a rotated hook's local tip.
        var anchor = _hook.TransformPoint(_hookOffset);
        var velocityX = (anchor.x - _previousHookPosition.x) / seconds;
        _previousHookPosition = anchor;
        _swayPhase = Mathf.Repeat(_swayPhase + seconds * _swayFrequency * Mathf.PI * 2f, Mathf.PI * 2f);
        var lean = Mathf.Clamp(-velocityX * dragLean, -maxDragLean, maxDragLean);
        var target = _restingAngle + Mathf.Sin(_swayPhase) * swayAngle + lean;
        _hookedAngle = Mathf.SmoothDampAngle(_hookedAngle, target, ref _hookedAngleVelocity,
            Mathf.Max(0.01f, hookedSmoothTime), Mathf.Infinity, seconds);
        transform.rotation = Quaternion.Euler(0f, 0f, _hookedAngle);
        PinMouth(anchor);
    }

    private void PinMouth(Vector3 anchor)
    {
        var sprite = _renderer.sprite;
        // A missing sprite uses a unit rectangle centered on the body pivot.
        var mouth = sprite == null ? _mouthPosition - Vector2.one * 0.5f
            : (Vector2.Scale(_mouthPosition, sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
        if (_renderer.flipX)
        {
            mouth.x = -mouth.x;
        }
        if (_renderer.flipY)
        {
            mouth.y = -mouth.y;
        }
        transform.position = anchor - transform.TransformVector(mouth);
    }

    private void ApplyDamageVisual()
    {
        // Persistent, distinct states after both nonlethal armor hits, plus a short impact flash.
        var damage = _armor.Damage;
        _renderer.color = _damageFlash > 0f ? Color.white
            : Color.Lerp(_baseColor, new Color(1f, 0.28f, 0.15f), damage * 0.85f);
        transform.localScale = Vector3.Scale(_baseScale, new Vector3(1f + damage * 0.16f, 1f - damage * 0.18f, 1f));
    }

    private void Fly(float seconds)
    {
        if (State != FishState.Airborne || seconds <= 0f)
        {
            return;
        }

        _velocity.y += _gravity * seconds;
        transform.position += (Vector3)(_velocity * seconds);
        transform.Rotate(0f, 0f, _angularVelocity * seconds, Space.World);
    }
}
