using System;
using System.Collections.Generic;
using UnityEngine;

// Represents a wave of enemies with optional subwaves and monster type counts.
[Serializable]
public class Wave {
    // Number of monsters currently active/allowed to be alive for this wave configuration.
    public int monsterCountInPlay;

    // The list of monster types and counts to spawn as part of this wave.
    public List<MonsterAndType> monsters;

    // How many sub-waves (batches) this wave is split into. Used by spawners to schedule spawns.
    public int numberOfSubWaves;
}