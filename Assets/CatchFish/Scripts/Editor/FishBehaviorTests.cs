#if UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Regression checks for cycle integration, targeting, formations, armor and swimming cleanup.</summary>
public class FishBehaviorTests
{
    [Test]
    public void MissingSettingsCruiseAtTheConfiguredSpeed()
    {
        var behavior = new FishSwimmingBehavior();
        behavior.Reset(null, -1f, null);
        behavior.Advance(100f, 2f, true, 0f, 0f, out var x, out var y);
        Assert.That(x, Is.EqualTo(-200f));
        Assert.That(y, Is.Zero);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Cruise));
        Assert.That(behavior.Scale, Is.EqualTo(1f));
    }

    [Test]
    public void BurstAdvancesThroughWarningActiveRecoveryAndCruise()
    {
        var behavior = NewBehavior(Cycle(FishSwimPattern.TimedBurst));
        Advance(behavior, 1.125f);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Warning));
        Assert.That(behavior.WarningProgress, Is.EqualTo(0.5f).Within(0.0001f));
        Advance(behavior, 0.125f);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Active));
        Advance(behavior, 0.5f);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Recovery));
        Advance(behavior, 0.75f);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Cruise));
    }

    [Test]
    public void OvershootIntegratesAllCrossedPhasesInsteadOfExtendingTheBurst()
    {
        var settings = Cycle(FishSwimPattern.TimedBurst);
        var coarse = NewBehavior(settings);
        coarse.Advance(2f, 1f, false, 0f, 0f, out var coarseX, out _);
        // 1 second cruise + .25 warning at .2 speed + .5 burst at x3 + .25 recovery.
        Assert.That(coarseX, Is.EqualTo(2.8f).Within(0.0001f));
        var fine = NewBehavior(settings);
        var fineX = 0f;
        for (var i = 0; i < 200; i++)
        {
            fine.Advance(0.01f, 1f, false, 0f, 0f, out var x, out _);
            fineX += x;
        }

        Assert.That(coarseX, Is.EqualTo(fineX).Within(0.0001f));
        Assert.That(coarse.Phase, Is.EqualTo(fine.Phase));
    }

    [Test]
    public void LungeOnlyTriggersOnActiveNearbyHookAndLocksDirectionAtWarning()
    {
        var behavior = NewBehavior(Cycle(FishSwimPattern.HookLunge));
        behavior.Advance(1f, 1f, false, 1f, 0f, out _, out _);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Cruise));
        behavior.Advance(1f, 1f, true, 4f, 0f, out _, out _);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Cruise));
        behavior.Advance(0.25f, 1f, true, 0f, -1f, out _, out _);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Active));
        behavior.Advance(0.5f, 1f, true, 1f, 0f, out var x, out var y);
        Assert.That(x, Is.Zero.Within(0.0001f));
        Assert.That(y, Is.EqualTo(-1.5f).Within(0.0001f));
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Recovery));
        behavior.Advance(0.5f, 1f, true, 1f, 0f, out _, out _);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Recovery));
        behavior.Advance(0.25f, 1f, true, 1f, 0f, out _, out _);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Cruise));
        behavior.Advance(0.01f, 1f, true, 1f, 0f, out _, out _);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Warning));
    }

    [TestCase(FishSwimPattern.TimedBurst)]
    [TestCase(FishSwimPattern.Inflation)]
    [TestCase(FishSwimPattern.HookLunge)]
    public void CancelClearsTimersWarningInflationAndMovement(FishSwimPattern pattern)
    {
        var behavior = NewBehavior(Cycle(pattern));
        behavior.Advance(pattern == FishSwimPattern.HookLunge ? 0.3f : 1.3f, 1f, true, 1f, 0f, out _, out _);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Active));
        behavior.Cancel();
        behavior.Advance(20f, 1f, true, 0f, 1f, out var x, out var y);
        Assert.That(x, Is.Zero);
        Assert.That(y, Is.Zero);
        Assert.That(behavior.WarningProgress, Is.Zero);
        Assert.That(behavior.Scale, Is.EqualTo(1f));
        Assert.That(behavior.WaveOffset, Is.Zero);
        behavior.Reset(Cycle(pattern), 1f, () => 0f);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Cruise));
    }

    [TestCase(1f)]
    [TestCase(-1f)]
    public void LungeTriggersAtRangeEntryAndAgainWhenRecoveryExpiresMidStep(float direction)
    {
        var behavior = new FishSwimmingBehavior();
        behavior.Reset(Cycle(FishSwimPattern.HookLunge), direction, null);
        // Start outside range, cruise for 1 second, then spend .125 seconds warning.
        behavior.Advance(1.125f, 1f, true, direction * 3f, 0f, out var x, out var y);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Warning));
        Assert.That(behavior.WarningProgress, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(x, Is.EqualTo(direction * 1.025f).Within(0.0001f));
        Assert.That(y, Is.Zero);
        // The next step crosses active + recovery, then must immediately retarget in cruise.
        behavior.Advance(1.5f, 1f, true, direction * 3f, 0f, out _, out _);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Warning));
        Assert.That(behavior.WarningProgress, Is.EqualTo(0.5f).Within(0.0001f));
        // Hook movement after warning starts cannot redirect the active lunge.
        behavior.Advance(0.25f, 1f, true, -direction, 1f, out x, out y);
        Assert.That(x, Is.EqualTo(direction * 0.4f).Within(0.0001f));
        Assert.That(y, Is.Zero);
    }

    [Test]
    public void InflationIsTelegraphedAndEndsBeforeRecovery()
    {
        var behavior = NewBehavior(Cycle(FishSwimPattern.Inflation));
        Advance(behavior, 1.125f);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Warning));
        Assert.That(behavior.Scale, Is.EqualTo(1f));
        Advance(behavior, 0.125f);
        Assert.That(behavior.Scale, Is.EqualTo(1.65f).Within(0.0001f));
        Advance(behavior, 0.5f);
        Assert.That(behavior.Scale, Is.EqualTo(1f));
    }

    [Test]
    public void WaveIsBoundedAndHasNoAccumulatingVerticalDrift()
    {
        var settings = Cycle(FishSwimPattern.Wave);
        Set(settings, "waveAmplitude", 0.45f);
        Set(settings, "wavePeriod", 4f);
        var behavior = NewBehavior(settings);
        Advance(behavior, 1f);
        Assert.That(behavior.WaveOffset, Is.EqualTo(0.45f).Within(0.0001f));
        Advance(behavior, 2f);
        Assert.That(behavior.WaveOffset, Is.EqualTo(-0.45f).Within(0.0001f));
        Advance(behavior, 397f);
        Assert.That(behavior.WaveOffset, Is.Zero.Within(0.0001f));
    }

    [Test]
    public void ZeroAndReversedRangesCannotStallTheCycle()
    {
        var settings = Cycle(FishSwimPattern.TimedBurst);
        Set(settings, "idleMin", 0f);
        Set(settings, "idleMax", 0f);
        Set(settings, "warningSeconds", 0f);
        Set(settings, "activeSeconds", 0f);
        Set(settings, "recoverySeconds", 0f);
        var behavior = NewBehavior(settings);
        behavior.Advance(1f, 1f, false, 0f, 0f, out var x, out _);
        Assert.That(float.IsNaN(x) || float.IsInfinity(x), Is.False);
        Set(settings, "idleMin", 4f);
        Set(settings, "idleMax", 2f);
        behavior.Reset(settings, 1f, () => 0f);
        Advance(behavior, 1.9f);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Cruise));
        behavior.Reset(settings, 1f, () => 1f);
        Advance(behavior, 3.9f);
        Assert.That(behavior.Phase, Is.EqualTo(FishSwimPhase.Cruise));
        Advance(behavior, 0.101f);
        Assert.That(behavior.Phase, Is.Not.EqualTo(FishSwimPhase.Cruise));
    }

    [Test]
    public void SchoolFitsCanvasWidthAndPreservesALaneAcrossWrapping()
    {
        const float bodyWidth = 0.6f * 1.5f;
        const float spacing = bodyWidth + 0.2f;
        const float span = 3f + bodyWidth * 0.5f * 1.08f;
        var count = FishSchoolLayout.FitCount(5, span, spacing, 1.2f);
        Assert.That(count, Is.InRange(3, 5));
        Assert.That(2f * span - (count - 1) * spacing - bodyWidth, Is.GreaterThanOrEqualTo(1.2f));
        var first = FishSchoolLayout.Wrap(200f, span);
        var second = FishSchoolLayout.Wrap(200f + spacing, span);
        var distance = second - first;
        if (distance < 0f)
        {
            distance += 2f * span;
        }

        Assert.That(distance, Is.EqualTo(spacing).Within(0.0001f));
        Assert.That(FishSchoolLayout.FitCount(5, 0f, 0f, 0f), Is.EqualTo(1));
        Assert.That(FishSchoolLayout.Wrap(123f, 0f), Is.InRange(-0.011f, 0.011f));
    }

    [Test]
    public void ArmorReportsExactlyOneLethalHitAndResets()
    {
        var armor = new FishArmor();
        armor.Reset(3);
        Assert.That(armor.TryHit(), Is.False);
        var firstDamage = armor.Damage;
        Assert.That(armor.HitsLeft, Is.EqualTo(2));
        Assert.That(armor.TryHit(), Is.False);
        Assert.That(armor.Damage, Is.GreaterThan(firstDamage));
        Assert.That(armor.HitsLeft, Is.EqualTo(1));
        Assert.That(armor.TryHit(), Is.True);
        Assert.That(armor.TryHit(), Is.False);
        Assert.That(armor.TryHit(), Is.False);
        armor.Reset(0);
        Assert.That(armor.HitsLeft, Is.EqualTo(1));
        Assert.That(armor.TryHit(), Is.True);
    }

    [Test]
    [Category("Unity")]
    public void CatalogCoversTheRosterAndUnknownIdsCruise()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<FishBehaviorCatalog>("Assets/CatchFish/Settings/FishBehaviorCatalog.asset");
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.Get("old-unknown-id").Pattern, Is.EqualTo(FishSwimPattern.Cruise));
        foreach (var id in new[] { "f1", "f2", "f5", "f7" })
        {
            Assert.That(catalog.Get(id).Pattern, Is.EqualTo(FishSwimPattern.Cruise), id);
            Assert.That(catalog.Get(id).AirborneHits, Is.EqualTo(1), id);
        }
        Assert.That(catalog.Get("f3").School, Is.True);
        Assert.That(catalog.Get("f4").Pattern, Is.EqualTo(FishSwimPattern.TimedBurst));
        Assert.That(catalog.Get("f6").WaveAmplitude, Is.EqualTo(0.25f));
        Assert.That(catalog.Get("f8").WaveAmplitude, Is.EqualTo(0.45f));
        Assert.That(catalog.Get("f9").Pattern, Is.EqualTo(FishSwimPattern.Inflation));
        Assert.That(catalog.Get("f10").Pattern, Is.EqualTo(FishSwimPattern.HookLunge));
        Assert.That(catalog.Get("f11").AirborneHits, Is.EqualTo(3));
        Assert.That(catalog.Get("f12").SpeedMultiplier, Is.EqualTo(3.5f));
    }

    [Test]
    [Category("Unity")]
    public void HookAndDirectLaunchRestoreRadiusTintAndScaleAndReinitIsSafe()
    {
        var fishObject = new GameObject("Behavior test fish", typeof(SpriteRenderer), typeof(Fish));
        var hook = new GameObject("Behavior test hook");
        try
        {
            var fish = fishObject.GetComponent<Fish>();
            var type = new FishType { Size = 1f, SwimSpeed = 1f, MinDepth = 0f, MaxDepth = 100f };
            var skin = new FishSkin { color = Color.cyan };
            fish.transform.position = new Vector3(0f, -10f);
            fish.Init(type, skin, 1f, 3f, Cycle(FishSwimPattern.Inflation));
            var scale = fish.transform.localScale;
            fish.Swim(1.3f, Vector2.zero, false);
            Assert.That(fish.Radius, Is.EqualTo(0.825f).Within(0.0001f));
            fish.HasHitHook = true;
            fish.AttachToHook(hook.transform, Vector3.zero);
            Assert.That(fish.Radius, Is.EqualTo(0.5f));
            Assert.That(fish.transform.localScale, Is.EqualTo(scale));
            Assert.That(fishObject.GetComponent<SpriteRenderer>().color, Is.EqualTo(skin.color));
            fish.Swim(100f, Vector2.zero, true);
            Assert.That(fish.Radius, Is.EqualTo(0.5f));
            fish.Init(type, skin, -1f, 3f, Cycle(FishSwimPattern.TimedBurst));
            Assert.That(fish.HasHitHook, Is.False);
            fish.Swim(1.125f, Vector2.zero, false);
            Assert.That(fishObject.GetComponent<SpriteRenderer>().color, Is.Not.EqualTo(skin.color));
            fish.Launch(Vector2.zero, Vector2.up, -5f);
            Assert.That(fish.transform.localScale, Is.EqualTo(scale));
            Assert.That(fishObject.GetComponent<SpriteRenderer>().color, Is.EqualTo(skin.color));
            Assert.That(fish.Radius, Is.EqualTo(0.5f));
            fish.transform.position = new Vector3(0f, -10f);
            fish.Init(type, skin, 1f, 3f, Cycle(FishSwimPattern.Inflation));
            fish.Swim(1.3f, Vector2.zero, false);
            fish.Launch(Vector2.zero, Vector2.up, -5f);
            Assert.That(fish.Radius, Is.EqualTo(0.5f));
            Assert.That(fish.transform.localScale, Is.EqualTo(scale));
        }
        finally
        {
            Object.DestroyImmediate(fishObject);
            Object.DestroyImmediate(hook);
        }
    }

    [Test]
    [Category("Unity")]
    public void WaveStaysBelowWaterAndInsideItsConfiguredDepthBand()
    {
        var fishObject = new GameObject("Wave test fish", typeof(SpriteRenderer), typeof(Fish));
        try
        {
            var fish = fishObject.GetComponent<Fish>();
            var type = new FishType { Size = 0.2f, SwimSpeed = 1f, MinDepth = 0f, MaxDepth = 1f };
            fish.transform.position = new Vector3(0f, -0.15f);
            fish.Init(type, new FishSkin(), 1f, 3f, Cycle(FishSwimPattern.Wave));
            for (var i = 0; i < 1000; i++)
            {
                fish.Swim(0.1f, Vector2.zero, false);
                Assert.That(fish.Position.y + fish.Radius, Is.LessThanOrEqualTo(0.0001f));
                Assert.That(fish.Position.y - fish.Radius, Is.GreaterThanOrEqualTo(-1.0001f));
            }
        }
        finally
        {
            Object.DestroyImmediate(fishObject);
        }
    }

    [TestCase(-0.1f)]
    [TestCase(-1.9f)]
    [Category("Unity")]
    public void InflatedBodyStaysInsideDepthBandAtBothEdges(float spawnY)
    {
        var fishObject = new GameObject("Puffer depth test", typeof(SpriteRenderer), typeof(Fish));
        try
        {
            var fish = fishObject.GetComponent<Fish>();
            fish.transform.position = new Vector3(0f, spawnY);
            fish.Init(new FishType { Size = 1f, MinDepth = 0f, MaxDepth = 2f },
                new FishSkin(), 1f, 3f, Cycle(FishSwimPattern.Inflation));
            fish.Swim(1.3f, Vector2.zero, false);
            Assert.That(fish.Radius, Is.EqualTo(0.825f).Within(0.0001f));
            Assert.That(fish.Position.y + fish.Radius, Is.LessThanOrEqualTo(0.0001f));
            Assert.That(fish.Position.y - fish.Radius, Is.GreaterThanOrEqualTo(-2.0001f));
        }
        finally
        {
            Object.DestroyImmediate(fishObject);
        }
    }

    [Test]
    [Category("Unity")]
    public void ArmoredFishShowsTwoDistinctDamageStatesAndOneLethalHit()
    {
        var fishObject = new GameObject("Armor test fish", typeof(SpriteRenderer), typeof(Fish));
        try
        {
            var fish = fishObject.GetComponent<Fish>();
            var settings = new FishBehaviorSettings();
            Set(settings, "airborneHits", 3);
            fish.Init(new FishType { Size = 1f, MaxDepth = 100f }, new FishSkin(), 1f, 3f, settings);
            Assert.That(fish.TryShoot(), Is.False);
            Assert.That(fish.HitsLeft, Is.EqualTo(3));
            fish.Launch(Vector2.zero, Vector2.up, -5f);
            Assert.That(fish.TryShoot(), Is.False);
            Set(fish, "_damageFlash", 0f);
            Invoke(fish, "ApplyDamageVisual");
            var firstColor = fishObject.GetComponent<SpriteRenderer>().color;
            var firstScale = fish.transform.localScale;
            Assert.That(fish.TryShoot(), Is.False);
            Set(fish, "_damageFlash", 0f);
            Invoke(fish, "ApplyDamageVisual");
            Assert.That(fishObject.GetComponent<SpriteRenderer>().color, Is.Not.EqualTo(firstColor));
            Assert.That(fish.transform.localScale, Is.Not.EqualTo(firstScale));
            Assert.That(fish.Radius, Is.EqualTo(0.5f));
            Assert.That(fish.TryShoot(), Is.True);
            Assert.That(fish.TryShoot(), Is.False);
            fish.Init(new FishType { Size = 1f, MaxDepth = 100f }, new FishSkin(), -1f, 3f, settings);
            Assert.That(fish.State, Is.EqualTo(FishState.Swimming));
            Assert.That(fish.HitsLeft, Is.EqualTo(3));
            Assert.That(fish.TryShoot(), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(fishObject);
        }
    }

    private static FishBehaviorSettings Cycle(FishSwimPattern pattern)
    {
        var settings = new FishBehaviorSettings();
        Set(settings, "pattern", pattern);
        Set(settings, "idleMin", 1f);
        Set(settings, "idleMax", 1f);
        Set(settings, "warningSeconds", 0.25f);
        Set(settings, "activeSeconds", 0.5f);
        Set(settings, "recoverySeconds", 0.75f);
        return settings;
    }

    private static FishSwimmingBehavior NewBehavior(FishBehaviorSettings settings)
    {
        var behavior = new FishSwimmingBehavior();
        behavior.Reset(settings, 1f, () => 0f);
        return behavior;
    }

    private static void Advance(FishSwimmingBehavior behavior, float seconds)
    {
        behavior.Advance(seconds, 1f, false, 0f, 0f, out _, out _);
    }

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    private static void Invoke(object target, string name)
    {
        target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    }
}
#endif
