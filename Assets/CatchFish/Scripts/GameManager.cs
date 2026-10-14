using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum GameState
{
    Hub,      // buy upgrades, start a run
    Descent,  // hook sinks, dodge fish
    Ascent,   // hook rises, catch fish
    Shooting, // catch flies up, shoot it for money
    Results,  // run summary
}

/// <summary>Numbers collected during one run, shown on the HUD and results screen.</summary>
public class RunStats
{
    public float DepthReached;
    public int Caught;
    public int Shot;
    public int Lost;
    public int Earnings;
}

/// <summary>
/// Core loop, settings and configs
/// </summary>
public class GameManager : MonoBehaviour
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
    private Camera _camera;
    private FishingInput _input;
    private float _slowdownLeft;

    /// <summary>Raised when a fish is shot: world position and money earned (for HUD popups).</summary>
    public event Action<Vector2, int> FishShot;

    public GameState State { get; private set; } = GameState.Hub;
    public Progression Progression { get; } = new Progression();
    public RunStats Run { get; private set; } = new RunStats();
    public Hook Hook => hook;

    private void Awake()
    {
        _camera = Camera.main;
        _input = new FishingInput();
    }

    private void OnEnable() => _input.Enable();

    private void OnDisable() => _input.Disable();

    private void Update()
    {
        switch (State)
        {
            case GameState.Descent:
                UpdateDescent();
                break;
            case GameState.Ascent:
                UpdateAscent();
                break;
            case GameState.Shooting:
                UpdateShooting();
                break;
        }

        UpdateCamera();
    }

    // ---------- Commands from the HUD ----------

    public void StartRun()
    {
        if (State != GameState.Hub)
        {
            return;
        }

        Run = new RunStats();
        _slowdownLeft = 0f;
        hook.ResetForRun(Progression.Durability, Progression.Capacity);
        spawner.SpawnOcean(Progression.MaxDepth);
        State = GameState.Descent;
    }

    public bool TryBuyUpgrade(UpgradeType type) => State == GameState.Hub && Progression.TryBuy(type);

    public void ReturnToHub()
    {
        if (State == GameState.Results)
        {
            State = GameState.Hub;
        }
    }

    // ---------- Descent: sink and dodge ----------

    private void UpdateDescent()
    {
        Steer();

        var speed = Progression.ReelSpeed;
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

        if (hook.Depth >= Progression.MaxDepth)
        {
            hook.SetDepth(Progression.MaxDepth);
            BeginAscent();
        }
    }

    private void BeginAscent()
    {
        Run.DepthReached = hook.Depth;
        State = GameState.Ascent;
    }

    // ---------- Ascent: rise and catch ----------

    private void UpdateAscent()
    {
        Steer();
        hook.Move(Progression.ReelSpeed * ascentSpeedMultiplier);

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

        Run.Caught = hook.Caught.Count;

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

        State = GameState.Shooting;
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
                Run.Lost++;
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

        Run.Shot++;
        Run.Earnings += target.Data.value;
        FishShot?.Invoke(target.Position, target.Data.value);

        _airborne.Remove(target);
        spawner.Remove(target);
    }

    private void FinishRun()
    {
        Progression.AddRunResult(Run.Earnings, Run.DepthReached);
        spawner.Clear();
        State = GameState.Results;
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
        switch (State)
        {
            case GameState.Descent:
                // Hook in the upper part of the screen so the player sees the fish they're sinking into.
                cameraFollow.TargetY = hook.transform.position.y - cameraLookAhead;
                break;
            case GameState.Ascent:
                // Hook in the lower part of the screen so the player sees the fish they're rising into.
                cameraFollow.TargetY = hook.transform.position.y + cameraLookAhead;
                break;
            case GameState.Shooting:
                cameraFollow.TargetY = Mathf.Max(2f, AverageAirborneHeight());
                break;
            default:
                // Boat in the lower half, leaving room for the menu above it.
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
