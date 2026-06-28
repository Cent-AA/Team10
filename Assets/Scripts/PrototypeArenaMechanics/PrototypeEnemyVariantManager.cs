using UnityEngine;

/// <summary>
/// Lightweight wave composition policy. WaveManager asks it for a variant at
/// spawn time; it never scans the active enemy list.
/// </summary>
public class PrototypeEnemyVariantManager : MonoBehaviour
{
    [Min(0)] public int miniBossEveryWave = 5;

    private int miniBossWaveApplied = -1;

    public void BeginWave(int wave)
    {
        if (miniBossWaveApplied != wave)
            miniBossWaveApplied = -1;
    }

    public PrototypeEnemyVariant.VariantType PickVariant(int wave, int spawnIndex)
    {
        wave = Mathf.Max(1, wave);

        bool miniBossWave = miniBossEveryWave > 0 && wave % miniBossEveryWave == 0;
        if (miniBossWave && miniBossWaveApplied != wave)
        {
            miniBossWaveApplied = wave;
            return PrototypeEnemyVariant.VariantType.MiniBoss;
        }

        if (wave <= 1)
            return PrototypeEnemyVariant.VariantType.Grunt;

        if (wave == 2)
            return spawnIndex % 4 == 0
                ? PrototypeEnemyVariant.VariantType.Runner
                : PrototypeEnemyVariant.VariantType.Grunt;

        if (wave == 3)
        {
            if (spawnIndex % 6 == 1) return PrototypeEnemyVariant.VariantType.Tank;
            if (spawnIndex % 4 == 0) return PrototypeEnemyVariant.VariantType.Runner;
            return PrototypeEnemyVariant.VariantType.Grunt;
        }

        // Stable ten-enemy composition: 50% grunt, 20% runner,
        // 20% tank, 10% exploder. It is predictable enough to balance.
        switch (spawnIndex % 10)
        {
            case 1:
            case 6:
                return PrototypeEnemyVariant.VariantType.Runner;
            case 3:
            case 8:
                return PrototypeEnemyVariant.VariantType.Tank;
            case 5:
                return PrototypeEnemyVariant.VariantType.Exploder;
            default:
                return PrototypeEnemyVariant.VariantType.Grunt;
        }
    }
}
