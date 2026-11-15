using System;

public enum FishSwimPhase
{
    Cruise,
    Warning,
    Active,
    Recovery,
}

/// <summary>Deterministic swimming cycle; integrates every phase crossed by a frame, without engine timing.</summary>
public sealed class FishSwimmingBehavior
{
    private FishBehaviorSettings _settings;
    private Func<float> _random;
    private float _remaining;
    private float _elapsed;
    private float _direction;
    private float _lungeX;
    private float _lungeY;
    private bool _running;

    public FishSwimPhase Phase { get; private set; }
    public float FacingDirection => _running && _settings.Pattern == FishSwimPattern.HookLunge &&
        (Phase == FishSwimPhase.Warning || Phase == FishSwimPhase.Active) ? _lungeX : _direction;
    public float WarningProgress => Phase == FishSwimPhase.Warning
        ? 1f - _remaining / Duration(_settings.WarningSeconds) : 0f;
    public float Scale => _running && _settings.Pattern == FishSwimPattern.Inflation && Phase == FishSwimPhase.Active
        ? _settings.InflatedScale : 1f;
    public float WaveOffset => _running && _settings.Pattern == FishSwimPattern.Wave
        ? _settings.WaveAmplitude * (float)Math.Sin(_elapsed * 2.0 * Math.PI / _settings.WavePeriod) : 0f;

    public void Reset(FishBehaviorSettings settings, float direction, Func<float> random)
    {
        _settings = settings ?? new FishBehaviorSettings();
        _random = random;
        _direction = direction < 0f ? -1f : 1f;
        _lungeX = _direction;
        _lungeY = 0f;
        _elapsed = 0f;
        _running = true;
        Phase = FishSwimPhase.Cruise;
        _remaining = IdleDuration();
    }

    public void Cancel()
    {
        _running = false;
        _elapsed = 0f;
        _remaining = 0f;
        _lungeX = 0f;
        _lungeY = 0f;
        Phase = FishSwimPhase.Cruise;
    }

    public void Advance(float seconds, float speed, bool hookActive, float hookDeltaX, float hookDeltaY,
        out float moveX, out float moveY)
    {
        moveX = 0f;
        moveY = 0f;
        if (!_running || seconds <= 0f)
        {
            return;
        }

        _elapsed += seconds;
        var left = seconds;
        while (left > 0f)
        {
            var step = Math.Min(left, _remaining);
            var triggerAfterStep = false;
            if (_settings.Pattern == FishSwimPattern.HookLunge && Phase == FishSwimPhase.Cruise && hookActive)
            {
                var dx = hookDeltaX - moveX;
                var dy = hookDeltaY - moveY;
                var radius = _settings.TriggerDistance;
                var distanceSquared = dx * dx + dy * dy;
                if (distanceSquared <= radius * radius)
                {
                    // Lock once, at warning onset, including when recovery ended during this step.
                    BeginLunge(dx, dy);
                    continue;
                }

                // Split cruise at the first entry into the stationary hook's trigger circle.
                // This prevents a long step from swimming through range without a warning.
                var velocity = _direction * speed;
                if (velocity != 0f && Math.Abs(dy) <= radius)
                {
                    var edge = (float)Math.Sqrt(Math.Max(0f, radius * radius - dy * dy));
                    var entry = (dx - (velocity > 0f ? edge : -edge)) / velocity;
                    if (entry > 0f && entry <= step)
                    {
                        step = entry;
                        triggerAfterStep = true;
                    }
                }
            }
            var multiplier = Phase == FishSwimPhase.Active &&
                (_settings.Pattern == FishSwimPattern.TimedBurst || _settings.Pattern == FishSwimPattern.HookLunge)
                ? _settings.SpeedMultiplier : 1f;
            // Compression is a readable pause before a burst, not extra burst travel.
            if (Phase == FishSwimPhase.Warning)
            {
                multiplier = 0.2f;
            }

            var lunging = Phase == FishSwimPhase.Active && _settings.Pattern == FishSwimPattern.HookLunge;
            moveX += (lunging ? _lungeX : _direction) * speed * multiplier * step;
            moveY += (lunging ? _lungeY : 0f) * speed * multiplier * step;
            left -= step;
            _remaining -= step;
            if (triggerAfterStep)
            {
                // Trigger directly at the computed intersection; do not depend on a rounded
                // distance comparison next iteration (which can stall at the circle edge).
                BeginLunge(hookDeltaX - moveX, hookDeltaY - moveY);
            }
            else if (_remaining <= 0f)
            {
                NextPhase();
            }
        }
    }

    private void BeginLunge(float dx, float dy)
    {
        var distance = (float)Math.Sqrt(dx * dx + dy * dy);
        _lungeX = distance > 0.0001f ? dx / distance : _direction;
        _lungeY = distance > 0.0001f ? dy / distance : 0f;
        Phase = FishSwimPhase.Warning;
        _remaining = Duration(_settings.WarningSeconds);
    }

    private void NextPhase()
    {
        switch (Phase)
        {
            case FishSwimPhase.Cruise:
                Phase = FishSwimPhase.Warning;
                _remaining = Duration(_settings.WarningSeconds);
                break;
            case FishSwimPhase.Warning:
                Phase = FishSwimPhase.Active;
                _remaining = Duration(_settings.ActiveSeconds);
                break;
            case FishSwimPhase.Active:
                Phase = FishSwimPhase.Recovery;
                _remaining = Duration(_settings.RecoverySeconds);
                break;
            default:
                Phase = FishSwimPhase.Cruise;
                _remaining = IdleDuration();
                break;
        }
    }

    private float IdleDuration()
    {
        if (_settings.Pattern != FishSwimPattern.TimedBurst && _settings.Pattern != FishSwimPattern.Inflation)
        {
            return float.PositiveInfinity;
        }

        var min = Math.Max(0f, Math.Min(_settings.IdleMin, _settings.IdleMax));
        var max = Math.Max(min, Math.Max(_settings.IdleMin, _settings.IdleMax));
        var roll = _random == null ? 0.5f : Math.Max(0f, Math.Min(1f, _random()));
        return Duration(min + (max - min) * roll);
    }

    // Malformed zero-duration profiles must still make progress through the cycle.
    private static float Duration(float seconds) => Math.Max(0.001f, seconds);
}
