using System;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
// Holds a reference to a monster prefab and the number of instances to spawn in a wave.
public class MonsterAndType {
    public GameObject monster;
    [DoNotSerialize]
    public Monster monsterComponent;
    public int count;

    public void SetMonsterComponent() {
        // Intended to cache a Monster component reference if needed.
    }
}