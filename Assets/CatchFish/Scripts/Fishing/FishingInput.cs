using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Using InputSystem and Actions to control the game, basic stuff
/// </summary>
public class FishingInput
{
    private readonly InputActionMap _map;
    private readonly InputAction _steer;
    private readonly InputAction _aim;
    private readonly InputAction _press;

    public FishingInput()
    {
        if (InputSystem.actions == null)
        {
            throw new InvalidOperationException("No project-wide Input Actions asset is assigned in Project Settings > Input System Package.");
        }

        _map = InputSystem.actions.FindActionMap("Fishing", throwIfNotFound: true);
        _steer = _map.FindAction("Steer", throwIfNotFound: true);
        _aim = _map.FindAction("Aim", throwIfNotFound: true);
        _press = _map.FindAction("Press", throwIfNotFound: true);
    }

    /// <summary>Keyboard steering in [-1, 1].</summary>
    public float SteerAxis => _steer.ReadValue<float>();

    public bool SteerReleasedThisFrame => _steer.WasReleasedThisFrame();

    /// <summary>Mouse button or finger is held down.</summary>
    public bool IsPressing => _press.IsPressed();

    /// <summary>Mouse click or finger tap started this frame.</summary>
    public bool PressedThisFrame => _press.WasPressedThisFrame();

    public Vector2 PointerScreenPosition => _aim.ReadValue<Vector2>();
    
    // TODO: figure out double tap/double click if we might use it for consumables?

    public void Enable() => _map.Enable();

    public void Disable() => _map.Disable();
}
