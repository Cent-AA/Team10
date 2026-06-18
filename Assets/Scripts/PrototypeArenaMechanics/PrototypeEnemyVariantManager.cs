using UnityEngine;

public class PrototypeEnemyVariantManager : MonoBehaviour
{
    public int miniBossEveryWave = 5;
    public float scanInterval = 0.2f;

    private WaveManager waveManager;
    private float scanTimer;
    private int bossWaveApplied = -1;

    void Start()
    {
        waveManager = FindFirstObjectByType<WaveManager>();
    }

    void Update()
    {
        if (PrototypeRunStats.Instance != null && PrototypeRunStats.Instance.RunEnded)
            return;

        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f)
            return;

        scanTimer = Mathf.Max(0.05f, scanInterval);
        ApplyVariantsToActiveZombies();
    }

    void ApplyVariantsToActiveZombies()
    {
        int wave = waveManager != null ? Mathf.Max(1, waveManager.GetCurrentWave()) : 1;
        Registry.CleanupZombies();

        for (int i = 0; i < Registry.Zombies.Count; i++)
        {
            ZombieAI zombie = Registry.Zombies[i];
            if (zombie == null || !zombie.gameObject.activeInHierarchy || !zombie.IsAlive)
                continue;

            PrototypeEnemyVariant marker = zombie.GetComponent<PrototypeEnemyVariant>();
            if (marker == null)
                marker = zombie.gameObject.AddComponent<PrototypeEnemyVariant>();

            if (marker.AppliedWave == wave)
                continue;

            PrototypeEnemyVariant.VariantType type = PickVariant(wave, i);
            marker.Apply(type, wave);
        }
    }

    PrototypeEnemyVariant.VariantType PickVariant(int wave, int index)
    {
        bool bossWave = miniBossEveryWave > 0 && wave % miniBossEveryWave == 0;
        if (bossWave && bossWaveApplied != wave)
        {
            bossWaveApplied = wave;
            return PrototypeEnemyVariant.VariantType.MiniBoss;
        }

        if (wave <= 1)
            return PrototypeEnemyVariant.VariantType.Grunt;

        if (wave == 2)
            return index % 3 == 0 ? PrototypeEnemyVariant.VariantType.Runner : PrototypeEnemyVariant.VariantType.Grunt;

        if (wave == 3)
            return index % 4 == 0 ? PrototypeEnemyVariant.VariantType.Tank : PrototypeEnemyVariant.VariantType.Runner;

        switch (index % 4)
        {
            case 0: return PrototypeEnemyVariant.VariantType.Grunt;
            case 1: return PrototypeEnemyVariant.VariantType.Runner;
            case 2: return PrototypeEnemyVariant.VariantType.Tank;
            default: return PrototypeEnemyVariant.VariantType.Exploder;
        }
    }
}
