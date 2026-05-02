using System;
using System.Collections.Generic;
using UnityEngine;

// Represents a wave of enemies with optional subwaves and monster type counts.
[Serializable]
public class Wave {
    public int monsterCountInPlay;
    public List<MonsterAndType> monsters;
    public int numberOfSubWaves;
}