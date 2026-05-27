public static class RunStats
{
    public static int EnemiesKilled { get; private set; }
    public static int WavesSurvived { get; private set; }

    public static void Reset()
    {
        EnemiesKilled = 0;
        WavesSurvived = 0;
    }

    public static void RecordEnemyKilled()
    {
        EnemiesKilled++;
    }

    public static void RecordWaveSurvived()
    {
        WavesSurvived++;
    }
}