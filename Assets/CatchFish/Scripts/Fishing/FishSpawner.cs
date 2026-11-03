using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// Fills the water with fish before each run. Deeper water has more fish, pricier species
/// and harder patterns (rows with one gap, vertical walls).
/// </summary>
public class FishSpawner : MonoBehaviour
{
    [SerializeField] private Fish fishPrefab;
    [SerializeField] private FishSkinCatalog skins;
    [SerializeField] private float halfWidth = 3f;

    [Header("Density")]
    [SerializeField] private float firstSpawnDepth = 6f;
    [SerializeField] private float bandHeight = 4f;
    [SerializeField] private float groupsPerBand = 0.8f;
    [SerializeField] private float extraGroupsPer100m = 1.2f;

    [Header("Patterns")]
    [SerializeField] private float patternStartDepth = 35f;
    [Range(0f, 1f)] [SerializeField] private float patternChance = 0.25f;

    private readonly List<Fish> _fish = new List<Fish>();
    private readonly List<FishType> _candidates = new List<FishType>();
    private ConfigService _config;

    public float HalfWidth => halfWidth;
    public IReadOnlyList<Fish> AllFish => _fish;

    [Inject]
    public void Construct(ConfigService config)
    {
        _config = config;
    }

    public void SpawnOcean(float maxDepth)
    {
        Clear();

        // Spawn a bit below max depth too, so the bottom of the line doesn't look empty.
        var lastDepth = maxDepth + bandHeight * 2f;
        for (var depth = firstSpawnDepth; depth <= lastDepth; depth += bandHeight)
        {
            CollectCandidates(depth);
            if (_candidates.Count == 0)
            {
                continue;
            }

            var groups = RollGroupCount(depth);
            for (var i = 0; i < groups; i++)
            {
                var data = PickWeighted();
                var groupDepth = depth + Random.Range(0f, bandHeight);
                SpawnGroup(data, groupDepth);
            }
        }
    }

    public void Remove(Fish fish)
    {
        _fish.Remove(fish);
        Destroy(fish.gameObject);
    }

    public void Clear()
    {
        foreach (var fish in _fish)
        {
            if (fish != null)
            {
                Destroy(fish.gameObject);
            }
        }

        _fish.Clear();
    }

    private void SpawnGroup(FishType data, float depth)
    {
        if (depth < patternStartDepth || Random.value > patternChance)
        {
            SpawnSingle(data, depth);
        }
        else if (Random.value < 0.5f)
        {
            SpawnRow(data, depth);
        }
        else
        {
            SpawnColumn(data, depth);
        }
    }

    private void SpawnSingle(FishType data, float depth)
    {
        var x = Random.Range(-halfWidth + data.Size, halfWidth - data.Size);
        CreateFish(data, new Vector2(x, -depth), RandomDirection());
    }

    /// <summary>A row across the screen with a gap the hook has to slip through.</summary>
    private void SpawnRow(FishType data, float depth)
    {
        const int gapSlots = 2;
        var span = halfWidth + data.Size * 0.5f;
        var spacing = data.Size * 1.5f + 0.3f;
        var slots = Mathf.FloorToInt(2f * span / spacing);
        var direction = RandomDirection();
        var startX = Random.Range(-span, span);

        for (var slot = 0; slot < slots - gapSlots; slot++)
        {
            var x = startX + slot * spacing;
            if (x > span)
            {
                x -= 2f * span;
            }

            CreateFish(data, new Vector2(x, -depth), direction);
        }
    }

    /// <summary>A vertical stack of fish: a moving wall.</summary>
    private void SpawnColumn(FishType data, float depth)
    {
        var count = Random.Range(3, 7);
        var x = Random.Range(-halfWidth + data.Size, halfWidth - data.Size);
        var direction = RandomDirection();
        for (var i = 0; i < count; i++)
        {
            CreateFish(data, new Vector2(x, -(depth + i * (data.Size + 0.15f))), direction);
        }
    }

    private Fish CreateFish(FishType data, Vector2 position, float direction)
    {
        var fish = Instantiate(fishPrefab, position, Quaternion.identity, transform);
        fish.name = data.Name;
        fish.Init(data, skins.Get(data.Id), direction, halfWidth);
        _fish.Add(fish);
        return fish;
    }

    private void CollectCandidates(float depth)
    {
        _candidates.Clear();
        foreach (var data in _config.Config.Fish)
        {
            if (data.LivesAt(depth) && data.SpawnWeight > 0f)
            {
                _candidates.Add(data);
            }
        }
    }

    private int RollGroupCount(float depth)
    {
        var expected = groupsPerBand + extraGroupsPer100m * depth / 100f;
        var whole = Mathf.FloorToInt(expected);
        return whole + (Random.value < expected - whole ? 1 : 0);
    }

    private FishType PickWeighted()
    {
        var total = 0f;
        foreach (var data in _candidates)
        {
            total += data.SpawnWeight;
        }

        var roll = Random.Range(0f, total);
        foreach (var data in _candidates)
        {
            roll -= data.SpawnWeight;
            if (roll <= 0f)
            {
                return data;
            }
        }

        return _candidates[_candidates.Count - 1];
    }

    private static float RandomDirection() => Random.value < 0.5f ? -1f : 1f;
}
