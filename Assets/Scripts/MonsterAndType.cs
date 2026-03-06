using System;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class MonsterAndType {
    public GameObject monster;
    [DoNotSerialize]
    public Monster monsterComponent;
    public int count;
    public void SetMonsterComponent() {

    }
}