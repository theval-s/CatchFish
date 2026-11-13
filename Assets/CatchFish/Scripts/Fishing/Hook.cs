using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The hook at the end of the line. GameManager tells it where to go;
/// the hook handles movement, collisions with fish and holding the catch.
/// </summary>
public class Hook : MonoBehaviour
{
    [SerializeField] private float sideSpeed = 14f;
    [SerializeField] private float hitRadius = 0.3f;
    [SerializeField] private float catchRadius = 0.55f;

    [Header("Catch attachment")]
    [Tooltip("Hook-local point where caught fish hold on by their mouths; match the tip of HookVisual.")]
    [SerializeField] private Vector2 catchPoint = new Vector2(0f, -0.18f);

    [Header("Line")]
    [Tooltip("The rod tip the line hangs from, in world space: match it to the rod in Surface.prefab.")]
    [SerializeField] private Vector2 lineStart = new Vector2(0.6f, 0.9f);
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private float lineWidth = 0.04f;

    private readonly List<Fish> _caught = new List<Fish>();
    private Transform _line;
    private float _targetX;

    public IReadOnlyList<Fish> Caught => _caught;
    public int Capacity { get; private set; }
    public int HitsLeft { get; private set; }
    public bool IsFull => _caught.Count >= Capacity;
    public float Depth => -transform.position.y;

    // The hook's look is the HookVisual.prefab child in the scene; only the line is drawn here.
    private void Awake()
    {
        _line = Shapes.CreateBlock("Fishing Line", lineColor, Vector2.one, null, 2).transform;
    }

    private void LateUpdate()
    {
        // Stretch a thin block from the rod tip to the hook.
        Vector2 hookPosition = transform.position;
        var delta = hookPosition - lineStart;
        _line.position = (lineStart + hookPosition) * 0.5f;
        _line.localScale = new Vector3(lineWidth, delta.magnitude, 1f);
        _line.rotation = Quaternion.FromToRotation(Vector3.up, delta);
    }

    private void OnDestroy()
    {
        if (_line != null)
        {
            Destroy(_line.gameObject);
        }
    }

    public void ResetForRun(int hits, int capacity)
    {
        _caught.Clear();
        HitsLeft = hits;
        Capacity = capacity;
        _targetX = 0f;
        transform.position = Vector3.zero;
    }

    public void SetTargetX(float x, float halfWidth) => _targetX = Mathf.Clamp(x, -halfWidth, halfWidth);

    public void Move(float verticalSpeed)
    {
        var position = transform.position;
        position.x = Mathf.MoveTowards(position.x, _targetX, sideSpeed * Time.deltaTime);
        position.y += verticalSpeed * Time.deltaTime;
        transform.position = position;
    }

    public void SetDepth(float depth)
    {
        var position = transform.position;
        position.y = -depth;
        transform.position = position;
    }

    public bool IsHitBy(Fish fish) => IsTouching(fish, hitRadius);

    public bool CanCatch(Fish fish) => IsTouching(fish, catchRadius);

    /// <summary>Uses up one hit. Returns false if the line can't take any more.</summary>
    public bool AbsorbHit()
    {
        HitsLeft--;
        return HitsLeft >= 0;
    }

    public bool TryCatch(Fish fish)
    {
        if (IsFull || fish.State != FishState.Swimming)
        {
            return false;
        }

        _caught.Add(fish);
        fish.AttachToHook(transform, catchPoint);
        return true;
    }

    private bool IsTouching(Fish fish, float radius)
    {
        var reach = radius + fish.Radius;
        return ((Vector2)transform.position - fish.Position).sqrMagnitude <= reach * reach;
    }
}
