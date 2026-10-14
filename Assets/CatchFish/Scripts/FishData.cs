using System;
using UnityEngine;

/// <summary>Settings for one kind of fish. Edited in the FishSpawner inspector.</summary>

// Probably should be changed to ScriptableObjects and prefabs?
[Serializable]
public class FishData
{
    public string name = "Fish";
    public Color color = Color.white;

    [Tooltip("Money for shooting it. Negative = a fish you should avoid (e.g. jellyfish).")]
    public int value = 1;

    [Tooltip("Depth range in meters where this fish lives.")]
    public float minDepth;
    public float maxDepth = 100f;

    public float swimSpeed = 1f;
    public float size = 0.6f;

    [Tooltip("Relative spawn chance compared to other fish at the same depth.")]
    public float spawnWeight = 1f;

    public FishData(string name, Color color, int value, float minDepth, float maxDepth, float swimSpeed,
        float size, float spawnWeight)
    {
        this.name = name;
        this.color = color;
        this.value = value;
        this.minDepth = minDepth;
        this.maxDepth = maxDepth;
        this.swimSpeed = swimSpeed;
        this.size = size;
        this.spawnWeight = spawnWeight;
    }

    public bool LivesAt(float depth) => depth >= minDepth && depth <= maxDepth;
}
