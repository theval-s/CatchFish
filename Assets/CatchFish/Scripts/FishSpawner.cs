using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fills the water with fish before each run. Deeper water has more fish, pricier species
/// and harder patterns (rows with one gap, vertical walls).
/// </summary>
public class FishSpawner : MonoBehaviour
{
    [SerializeField] private float halfWidth = 3f;

    [Header("Density")]
    [SerializeField] private float firstSpawnDepth = 6f;
    [SerializeField] private float bandHeight = 4f;
    [SerializeField] private float groupsPerBand = 0.8f;
    [SerializeField] private float extraGroupsPer100m = 1.2f;

    [Header("Patterns")]
    [SerializeField] private float patternStartDepth = 35f;
    [Range(0f, 1f)] [SerializeField] private float patternChance = 0.25f;

    [SerializeField] private FishData[] fishTypes =
    {
        //            name          color                              value minD  maxD  speed size  weight
        new FishData("Amogusfish",    new Color(0.75f, 0.80f, 0.85f),   2,   0f,  80f, 1.2f, 0.6f, 10f),
        new FishData("Sobakafish",   new Color(0.30f, 0.60f, 0.90f),   6,  15f, 150f, 1.8f, 0.7f,  7f),
        new FishData("Abobafish",       new Color(0.20f, 0.30f, 0.70f),  15,  50f, 250f, 2.4f, 0.9f,  5f),
        new FishData("Volkfish", new Color(0.55f, 0.25f, 0.55f),  40, 110f, 400f, 1.0f, 0.9f,  3f),
        new FishData("FishFish",   new Color(1.00f, 0.80f, 0.10f), 120,  70f, 400f, 3.2f, 0.6f, 0.6f),
        new FishData("Jellyfish",  new Color(1.00f, 0.45f, 0.75f), -10,  25f, 400f, 0.4f, 0.8f,  4f),
    };

    private readonly List<Fish> _fish = new List<Fish>();
    private readonly List<FishData> _candidates = new List<FishData>();

    public float HalfWidth => halfWidth;
    public IReadOnlyList<Fish> AllFish => _fish;

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

    private void SpawnGroup(FishData data, float depth)
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

    private void SpawnSingle(FishData data, float depth)
    {
        var x = Random.Range(-halfWidth + data.size, halfWidth - data.size);
        CreateFish(data, new Vector2(x, -depth), RandomDirection());
    }

    /// <summary>A row across the screen with a gap the hook has to slip through.</summary>
    private void SpawnRow(FishData data, float depth)
    {
        const int gapSlots = 2;
        var span = halfWidth + data.size * 0.5f;
        var spacing = data.size * 1.5f + 0.3f;
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
    private void SpawnColumn(FishData data, float depth)
    {
        var count = Random.Range(3, 7);
        var x = Random.Range(-halfWidth + data.size, halfWidth - data.size);
        var direction = RandomDirection();
        for (var i = 0; i < count; i++)
        {
            CreateFish(data, new Vector2(x, -(depth + i * (data.size + 0.15f))), direction);
        }
    }

    private Fish CreateFish(FishData data, Vector2 position, float direction)
    {
        //TODO: Change creating fishes
        var block = Shapes.CreateBlock(data.name, data.color, Vector2.one, transform, sortingOrder: 1);
        block.transform.position = position;

        var fish = block.gameObject.AddComponent<Fish>();
        fish.Init(data, direction, halfWidth);
        _fish.Add(fish);
        return fish;
    }

    private void CollectCandidates(float depth)
    {
        _candidates.Clear();
        foreach (var data in fishTypes)
        {
            if (data.LivesAt(depth) && data.spawnWeight > 0f)
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

    private FishData PickWeighted()
    {
        var total = 0f;
        foreach (var data in _candidates)
        {
            total += data.spawnWeight;
        }

        var roll = Random.Range(0f, total);
        foreach (var data in _candidates)
        {
            roll -= data.spawnWeight;
            if (roll <= 0f)
            {
                return data;
            }
        }

        return _candidates[_candidates.Count - 1];
    }

    private static float RandomDirection() => Random.value < 0.5f ? -1f : 1f;
}
