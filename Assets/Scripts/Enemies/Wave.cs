using System;
using UnityEngine;

// Represents a wave of enemies with two explicit monster slots: skeletons and knights.
[Serializable]
public class Wave {
    [Tooltip("Skeleton enemy prefab and count for this wave.")]
    public MonsterAndType skeletons;

    [Tooltip("Knight enemy prefab and count for this wave.")]
    public MonsterAndType knights;
}