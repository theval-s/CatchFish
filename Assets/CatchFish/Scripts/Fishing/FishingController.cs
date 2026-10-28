using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

/// <summary>
/// Runs the fishing minigame: descent, ascent and shooting. Gear comes from the run's
/// <see cref="FishingSessionState"/>; results go back through <see cref="FishingService"/>.
/// </summary>
public class FishingController : MonoBehaviour
{
    [SerializeField] private Hook hook;
    [SerializeField] private FishSpawner spawner;
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Descent")]
    [SerializeField] private float hitSlowdownSeconds = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float hitSlowdownFactor = 0.35f;

    [Header("Ascent")]
    [SerializeField] private float ascentSpeedMultiplier = 1.6f;

    [Header("Camera")]
    [Tooltip("How far ahead of the hook the camera looks: below it while sinking, above it while rising.")]
    [SerializeField] private float cameraLookAhead = 3.5f;

    [Header("Shooting")]
    [SerializeField] private float gravity = -5f;
    [SerializeField] private float launchSpeedMin = 6f;
    [SerializeField] private float launchSpeedMax = 12f;
    [SerializeField] private float launchSpreadX = 1.2f;
    [SerializeField] private float shotRadius = 0.45f;

    private readonly List<Fish> _airborne = new List<Fish>();
    private FishingService _fishing;
    private Camera _camera;
    private FishingInput _input;
    private float _slowdownLeft;

    /// <summary>Raised when a fish is shot: world position and money earned (for HUD popups).</summary>
    public event Action<Vector2, int> FishShot;

    public FishingSessionState Session { get; private set; }
    public Hook Hook => hook;

    [Inject]
    public void Construct(FishingService fishing)
    {
        _fishing = fishing;
    }

    private void Awake()
    {
        _camera = Camera.main;
        _input = new FishingInput();
    }

    private void OnEnable() => _input.Enable();

    private void OnDisable() => _input.Disable();

    private void Start()
    {
        // The hub starts the run before loading this scene.
        Session = _fishing.Session;
        if (Session == null || Session.Phase == FishingPhase.Results)
        {
            Session = _fishing.StartRun();
        }

        _slowdownLeft = 0f;
        hook.ResetForRun(Session.Durability, Session.Capacity);
        spawner.SpawnOcean(Session.MaxDepth);
    }

    private void Update()
    {
        switch (Session.Phase)
        {
            case FishingPhase.Descent:
                UpdateDescent();
                break;
            case FishingPhase.Ascent:
                UpdateAscent();
                break;
            case FishingPhase.Shooting:
                UpdateShooting();
                break;
        }

        UpdateCamera();
    }

    // ---------- Descent: sink and dodge ----------

    private void UpdateDescent()
    {
        Steer();

        var speed = Session.ReelSpeed;
        if (_slowdownLeft > 0f)
        {
            speed *= hitSlowdownFactor;
            _slowdownLeft -= Time.deltaTime;
        }

        hook.Move(-speed);

        foreach (var fish in spawner.AllFish)
        {
            if (fish.State != FishState.Swimming || fish.HasHitHook || !hook.IsHitBy(fish))
            {
                continue;
            }

            fish.HasHitHook = true;
            if (!hook.AbsorbHit())
            {
                // Out of durability: this fish gets hooked and we start reeling in.
                hook.TryCatch(fish);
                BeginAscent();
                return;
            }

            _slowdownLeft = hitSlowdownSeconds;
        }

        if (hook.Depth >= Session.MaxDepth)
        {
            hook.SetDepth(Session.MaxDepth);
            BeginAscent();
        }
    }

    private void BeginAscent()
    {
        Session.DepthReached = hook.Depth;
        Session.Phase = FishingPhase.Ascent;
    }

    // ---------- Ascent: rise and catch ----------

    private void UpdateAscent()
    {
        Steer();
        hook.Move(Session.ReelSpeed * ascentSpeedMultiplier);

        foreach (var fish in spawner.AllFish)
        {
            if (hook.IsFull)
            {
                break;
            }

            if (fish.State == FishState.Swimming && hook.CanCatch(fish))
            {
                hook.TryCatch(fish);
            }
        }

        Session.Caught = hook.Caught.Count;

        if (hook.transform.position.y >= 0f)
        {
            hook.SetDepth(0f);
            BeginShooting();
        }
    }

    // ---------- Shooting: throw the catch up and shoot it ----------

    private void BeginShooting()
    {
        _airborne.Clear();
        foreach (var fish in hook.Caught)
        {
            var limit = spawner.HalfWidth - fish.Radius;
            var x = Mathf.Clamp(hook.transform.position.x + Random.Range(-launchSpreadX, launchSpreadX), -limit, limit);
            var velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(launchSpeedMin, launchSpeedMax));
            fish.Launch(new Vector2(x, 0f), velocity, gravity);
            _airborne.Add(fish);
        }

        Session.Phase = FishingPhase.Shooting;
    }

    private void UpdateShooting()
    {
        if (WasShootPressed(out var point))
        {
            Shoot(point);
        }

        // Fish that fall back into the water are lost.
        for (var i = _airborne.Count - 1; i >= 0; i--)
        {
            var fish = _airborne[i];
            if (fish.HasFallenIntoWater)
            {
                Session.Lost++;
                _airborne.RemoveAt(i);
                spawner.Remove(fish);
            }
        }

        if (_airborne.Count == 0)
        {
            FinishRun();
        }
    }

    private void Shoot(Vector2 point)
    {
        Fish target = null;
        var bestDistance = float.MaxValue;
        foreach (var fish in _airborne)
        {
            var distance = Vector2.Distance(fish.Position, point);
            if (distance <= shotRadius + fish.Radius && distance < bestDistance)
            {
                target = fish;
                bestDistance = distance;
            }
        }

        if (target == null)
        {
            return;
        }

        var type = target.Type;
        Session.Shot++;
        Session.Earnings += type.Value;
        if (type.IsCollectible)
        {
            Session.CollectedFish.TryGetValue(type.Id, out var count);
            Session.CollectedFish[type.Id] = count + 1;
        }

        FishShot?.Invoke(target.Position, type.Value);

        _airborne.Remove(target);
        spawner.Remove(target);
    }

    private void FinishRun()
    {
        spawner.Clear();
        _fishing.CompleteRun();
    }

    // ---------- Input ----------

    /// <summary>Hook follows the finger/mouse while held; A/D, arrows or a gamepad stick also work.</summary>
    private void Steer()
    {
        if (_input.IsPressing)
        {
            hook.SetTargetX(ScreenToWorld(_input.PointerScreenPosition).x, spawner.HalfWidth);
            return;
        }

        var axis = _input.SteerAxis;
        if (Mathf.Abs(axis) > 0.1f)
        {
            hook.SetTargetX(hook.transform.position.x + axis, spawner.HalfWidth);
        }
        else if (_input.SteerReleasedThisFrame)
        {
            // Stop where we are instead of drifting to the last target.
            hook.SetTargetX(hook.transform.position.x, spawner.HalfWidth);
        }
    }

    private bool WasShootPressed(out Vector2 worldPoint)
    {
        if (_input.PressedThisFrame)
        {
            worldPoint = ScreenToWorld(_input.PointerScreenPosition);
            return true;
        }

        worldPoint = default;
        return false;
    }

    private Vector2 ScreenToWorld(Vector2 screenPosition) => _camera.ScreenToWorldPoint(screenPosition);

    // ---------- Camera ----------

    private void UpdateCamera()
    {
        switch (Session.Phase)
        {
            case FishingPhase.Descent:
                // Hook in the upper part of the screen so the player sees the fish they're sinking into.
                cameraFollow.TargetY = hook.transform.position.y - cameraLookAhead;
                break;
            case FishingPhase.Ascent:
                // Hook in the lower part of the screen so the player sees the fish they're rising into.
                cameraFollow.TargetY = hook.transform.position.y + cameraLookAhead;
                break;
            case FishingPhase.Shooting:
                cameraFollow.TargetY = Mathf.Max(2f, AverageAirborneHeight());
                break;
            default:
                // Boat in the lower half, like the hub used to show it.
                cameraFollow.TargetY = 2.5f;
                break;
        }
    }

    private float AverageAirborneHeight()
    {
        if (_airborne.Count == 0)
        {
            return 0f;
        }

        var sum = 0f;
        foreach (var fish in _airborne)
        {
            sum += fish.Position.y;
        }

        return sum / _airborne.Count;
    }
}
