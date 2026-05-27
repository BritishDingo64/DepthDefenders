using System;
using UnityEngine;

[Serializable]
// Holds a reference to a monster prefab and the number of instances to spawn in a wave.
public class MonsterAndType {
    public GameObject monster;
    public int count;
}