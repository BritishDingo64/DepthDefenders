using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Wave {
    public int monsterCountInPlay;
    public List<MonsterAndType> monsters;
    public int numberOfSubWaves;
}