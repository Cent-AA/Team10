using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Zombie Prefabs — assign variants here")]
    [Tooltip("Required fallback prefab and the prefab used for Grunts.")]
    public GameObject zombiePrefab;
    [Tooltip("Optional. Leave empty to reuse Zombie Prefab with Runner modifiers.")]
    public GameObject runnerZombiePrefab;
    [Tooltip("Optional. Leave empty to reuse Zombie Prefab with Tank modifiers.")]
    public GameObject tankZombiePrefab;
    [Tooltip("Optional. Leave empty to reuse Zombie Prefab with Exploder modifiers.")]
    public GameObject exploderZombiePrefab;
    [Tooltip("Optional. Leave empty to reuse Zombie Prefab with MiniBoss modifiers.")]
    public GameObject miniBossZombiePrefab;

    [Header("Zombie Spawning")]
    public Transform[] spawnPoints;
    public Transform campfireTarget;
    public bool useSpawnPointsWhenAvailable = true;
    public float spawnPointJitter = 1f;
    public float safeZoneRadius = 7f;
    public float spawnRingExtraDistance = 4f;
    public float spawnRingRandomness = 3f;
    public int initialZombiePoolSize = 32;
    public int maxZombiePoolSize = 64;
    [Range(1, 64)] public int maxActiveZombies = 32;

    [Header("Wave Settings")]
    public int startZombies = 3;
    public int zombiesPerWave = 2;
    public float spawnInterval = 0.5f;
    public float timeBetweenWaves = 5f;
    public float zombieHealthIncrease = 8f;
    public float zombieSpeedIncrease = 0.09f;
    public float zombieDamageIncrease = 1f;

    [Header("Wave Stat Caps")]
    public float maxZombieMoveSpeed = 5.5f;
    public float maxZombieRunSpeed = 6.75f;
    public float maxZombieAttackDamage = 30f;

    [Header("Enemy Coordination")]
    public EnemyDirector enemyDirector;
    public PrototypeEnemyVariantManager variantManager;

    [Header("UI")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI zombieCountText;
    public float waveTextDisplayTime = 3f;

    [Header("Camera")]
    public float waveShakeIntensity = 0.3f;

    [Header("Boss")]
    public GameObject bossPrefab;
    public BossIntroSequence bossIntroSequence;
    public Transform bossSpawnPoint;
    public int bossWave = 5;

    private bool bossSpawned;
    private BossController activeBoss;
    private bool bossWavePending;
    private bool bossHasSpawned;

    private int currentWave;
    private int zombiesAlive;
    private int lastDisplayedZombiesAlive = -1;
    private int lastDisplayedRemaining = -1;
    private int totalZombiesThisWave;
    private int spawnedZombiesThisWave;
    private bool waveInProgress;
    private bool spawningWave;

    private readonly List<ZombieAI> activeZombies = new List<ZombieAI>(32);
    private readonly Dictionary<GameObject, Queue<ZombieAI>> zombiePools = new Dictionary<GameObject, Queue<ZombieAI>>();
    private readonly Dictionary<ZombieAI, GameObject> zombieSources = new Dictionary<ZombieAI, GameObject>();
    private readonly Dictionary<GameObject, ZombieBaseStats> prefabBaseStats = new Dictionary<GameObject, ZombieBaseStats>();
    private int pooledZombieCount;
    private float ammoDropChanceBonus;

    private struct ZombieBaseStats
    {
        public float Health;
        public float MoveSpeed;
        public float RunSpeed;
        public float AttackDamage;
        public float AmmoDropChance;
    }

    public System.Action<int> OnWaveStart;
    public System.Action<int> OnWaveComplete;
    public System.Action OnAllWavesComplete;

    void Start()
    {
        if (campfireTarget == null)
            campfireTarget = FindCampfireTransform();

        if (enemyDirector == null)
            enemyDirector = FindAnyObjectByType<EnemyDirector>();
        if (enemyDirector == null)
            enemyDirector = gameObject.AddComponent<EnemyDirector>();
        enemyDirector.Initialize(campfireTarget);

        if (variantManager == null)
            variantManager = GetComponent<PrototypeEnemyVariantManager>();

        CacheConfiguredPrefabStats();
        PrewarmZombiePool();

        if (waveText != null)
            waveText.gameObject.SetActive(false);
        UpdateZombieCountUI(true);

        StartCoroutine(StartNextWaveAfterDelay(3f));
    }

    void Update()
    {
        if (waveInProgress && !spawningWave && zombiesAlive <= 0 && !IsBossBlockingWaveEnd())
        {
            waveInProgress = false;
            OnWaveComplete?.Invoke(currentWave);
            StartCoroutine(StartNextWaveAfterDelay(timeBetweenWaves));
        }
    }

    IEnumerator StartNextWaveAfterDelay(float delay)
    {
        float remaining = delay;
        while (remaining > 0f)
        {
            if (waveText != null)
            {
                waveText.gameObject.SetActive(true);
                waveText.text = "Next wave in " + Mathf.CeilToInt(remaining);
            }

            remaining -= Time.deltaTime;
            yield return null;
        }

        StartWave();
    }

    void StartWave()
    {
        currentWave++;
        totalZombiesThisWave = startZombies + (currentWave - 1) * zombiesPerWave;
        spawnedZombiesThisWave = 0;
        waveInProgress = true;
        activeZombies.Clear();
        zombiesAlive = 0;
        variantManager?.BeginWave(currentWave);
        UpdateZombieCountUI(true);

        OnWaveStart?.Invoke(currentWave);
        StartCoroutine(ShowWaveText());
        ArenaCamera.Shake(waveShakeIntensity, 0.5f);
        StartCoroutine(SpawnWaveZombies());

        if (currentWave == bossWave && !bossSpawned && bossPrefab != null)
        {
            bossSpawned = true;
            bossWavePending = true;
            StartCoroutine(SpawnBoss());
        }
    }

    IEnumerator ShowWaveText()
    {
        if (waveText == null)
            yield break;

        waveText.gameObject.SetActive(true);
        waveText.text = "Wave " + currentWave;

        float elapsed = 0f;
        while (elapsed < waveTextDisplayTime)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - elapsed / waveTextDisplayTime * 0.5f;
            float scale = 1f + Mathf.Sin(elapsed * 5f) * 0.05f;
            waveText.transform.localScale = Vector3.one * scale;

            Color color = waveText.color;
            color.a = alpha;
            waveText.color = color;
            yield return null;
        }

        waveText.gameObject.SetActive(false);
        Color resetColor = waveText.color;
        resetColor.a = 1f;
        waveText.color = resetColor;
        waveText.transform.localScale = Vector3.one;
    }

    IEnumerator SpawnWaveZombies()
    {
        spawningWave = true;
        WaitForSeconds spawnDelay = new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));

        for (int i = 0; i < totalZombiesThisWave; i++)
        {
            // Once the active cap is reached, the rest of the wave waits as
            // reinforcements instead of increasing the physics crowd.
            while (activeZombies.Count >= Mathf.Max(1, maxActiveZombies))
                yield return null;

            SpawnZombie(i);
            spawnedZombiesThisWave++;
            UpdateZombieCountUI(false);

            if (i + 1 < totalZombiesThisWave)
                yield return spawnDelay;
        }

        spawningWave = false;
    }

    IEnumerator SpawnBoss()
    {
        yield return new WaitForSeconds(1f);

        Vector3 spawnPosition = bossSpawnPoint != null
            ? bossSpawnPoint.position
            : new Vector3(-15f, 0f, 0f);

        GameObject bossObject = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
        BossController boss = bossObject != null ? bossObject.GetComponent<BossController>() : null;
        if (boss == null)
        {
            bossWavePending = false;
            yield break;
        }

        activeBoss = boss;
        bossHasSpawned = true;
        boss.ConfigureForWave(currentWave);

        if (bossIntroSequence != null)
        {
            bossIntroSequence.boss = boss;
            bossIntroSequence.Play();
        }
        else
        {
            boss.Activate();
        }
    }

    void SpawnZombie(int spawnIndex)
    {
        PrototypeEnemyVariant.VariantType type = variantManager != null
            ? variantManager.PickVariant(currentWave, spawnIndex)
            : PickFallbackVariant(currentWave, spawnIndex);

        GameObject prefab = GetPrefabForVariant(type);
        if (prefab == null)
            return;

        Vector3 spawnPosition = GetSpawnPosition();
        ZombieAI zombie = GetZombieInstance(prefab, spawnPosition);
        if (zombie == null)
            return;

        ApplyWaveStats(zombie, prefab);

        PrototypeEnemyVariant variant = zombie.GetComponent<PrototypeEnemyVariant>();
        if (variant == null)
            variant = zombie.gameObject.AddComponent<PrototypeEnemyVariant>();
        variant.Apply(type, currentWave);

        zombie.SetPoolManaged(true);
        zombie.SetEnemyDirector(enemyDirector);
        zombie.ResetForSpawn(campfireTarget);
        variant.RefreshVisuals();
        zombie.OnDied -= HandleZombieDied;
        zombie.OnDied += HandleZombieDied;

        zombie.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        zombie.gameObject.SetActive(true);

        if (!activeZombies.Contains(zombie))
            activeZombies.Add(zombie);

        enemyDirector?.RefreshCollisionRules(zombie);
        zombiesAlive = activeZombies.Count;
        UpdateZombieCountUI(false);
    }

    ZombieAI GetZombieInstance(GameObject prefab, Vector3 spawnPosition)
    {
        Queue<ZombieAI> pool = GetPool(prefab);
        while (pool.Count > 0)
        {
            ZombieAI pooledZombie = pool.Dequeue();
            pooledZombieCount = Mathf.Max(0, pooledZombieCount - 1);
            if (pooledZombie == null)
                continue;

            pooledZombie.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            zombieSources[pooledZombie] = prefab;
            return pooledZombie;
        }

        GameObject zombieObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        if (zombieObject == null)
            return null;

        zombieObject.SetActive(false);
        ZombieAI zombie = zombieObject.GetComponent<ZombieAI>();
        if (zombie == null)
        {
            Destroy(zombieObject);
            return null;
        }

        zombieSources[zombie] = prefab;
        return zombie;
    }

    void CacheConfiguredPrefabStats()
    {
        CachePrefabStats(zombiePrefab);
        CachePrefabStats(runnerZombiePrefab);
        CachePrefabStats(tankZombiePrefab);
        CachePrefabStats(exploderZombiePrefab);
        CachePrefabStats(miniBossZombiePrefab);
    }

    void CachePrefabStats(GameObject prefab)
    {
        if (prefab == null || prefabBaseStats.ContainsKey(prefab))
            return;

        ZombieAI zombie = prefab.GetComponent<ZombieAI>();
        if (zombie == null)
            return;

        prefabBaseStats[prefab] = new ZombieBaseStats
        {
            Health = zombie.maxHealth,
            MoveSpeed = zombie.moveSpeed,
            RunSpeed = zombie.runSpeed,
            AttackDamage = zombie.attackDamage,
            AmmoDropChance = zombie.ammoDropChance
        };
    }

    void ApplyWaveStats(ZombieAI zombie, GameObject sourcePrefab)
    {
        if (zombie == null)
            return;

        if (!prefabBaseStats.TryGetValue(sourcePrefab, out ZombieBaseStats stats))
        {
            stats = new ZombieBaseStats
            {
                Health = zombie.maxHealth,
                MoveSpeed = zombie.moveSpeed,
                RunSpeed = zombie.runSpeed,
                AttackDamage = zombie.attackDamage,
                AmmoDropChance = zombie.ammoDropChance
            };
            prefabBaseStats[sourcePrefab] = stats;
        }

        float waveBonus = Mathf.Max(0, currentWave - 1);
        zombie.maxHealth = stats.Health + waveBonus * zombieHealthIncrease;
        zombie.moveSpeed = Mathf.Min(maxZombieMoveSpeed, stats.MoveSpeed + waveBonus * zombieSpeedIncrease);
        zombie.runSpeed = Mathf.Min(maxZombieRunSpeed, stats.RunSpeed + waveBonus * zombieSpeedIncrease);
        zombie.attackDamage = Mathf.Min(maxZombieAttackDamage, stats.AttackDamage + waveBonus * zombieDamageIncrease);
        zombie.ammoDropChance = Mathf.Clamp01(stats.AmmoDropChance + ammoDropChanceBonus);
    }

    void PrewarmZombiePool()
    {
        if (zombiePrefab == null || initialZombiePoolSize <= 0)
            return;

        Queue<ZombieAI> pool = GetPool(zombiePrefab);
        int count = Mathf.Min(initialZombiePoolSize, maxZombiePoolSize);
        for (int i = 0; i < count; i++)
        {
            GameObject zombieObject = Instantiate(zombiePrefab);
            if (zombieObject == null)
                continue;

            zombieObject.SetActive(false);
            ZombieAI zombie = zombieObject.GetComponent<ZombieAI>();
            if (zombie == null)
            {
                Destroy(zombieObject);
                continue;
            }

            zombie.SetPoolManaged(true);
            zombie.SetEnemyDirector(enemyDirector);
            zombie.OnDied -= HandleZombieDied;
            Registry.UnregisterZombie(zombie);
            zombieSources[zombie] = zombiePrefab;
            pool.Enqueue(zombie);
            pooledZombieCount++;
        }
    }

    void HandleZombieDied(ZombieAI zombie)
    {
        if (zombie == null)
            return;

        zombie.OnDied -= HandleZombieDied;
        activeZombies.Remove(zombie);
        zombiesAlive = activeZombies.Count;
        UpdateZombieCountUI(false);
        StartCoroutine(ReturnZombieToPoolAfterDelay(zombie));
    }

    IEnumerator ReturnZombieToPoolAfterDelay(ZombieAI zombie)
    {
        yield return new WaitForSeconds(zombie.DeathDespawnDelay);

        if (zombie == null)
            yield break;

        GameObject zombieObject = zombie.gameObject;
        if (pooledZombieCount >= maxZombiePoolSize || !zombieSources.TryGetValue(zombie, out GameObject sourcePrefab))
        {
            zombieSources.Remove(zombie);
            Destroy(zombieObject);
            yield break;
        }

        zombieObject.SetActive(false);
        GetPool(sourcePrefab).Enqueue(zombie);
        pooledZombieCount++;
    }

    Queue<ZombieAI> GetPool(GameObject prefab)
    {
        if (!zombiePools.TryGetValue(prefab, out Queue<ZombieAI> pool))
        {
            pool = new Queue<ZombieAI>();
            zombiePools[prefab] = pool;
        }

        return pool;
    }

    GameObject GetPrefabForVariant(PrototypeEnemyVariant.VariantType type)
    {
        switch (type)
        {
            case PrototypeEnemyVariant.VariantType.Runner:
                return runnerZombiePrefab != null ? runnerZombiePrefab : zombiePrefab;
            case PrototypeEnemyVariant.VariantType.Tank:
                return tankZombiePrefab != null ? tankZombiePrefab : zombiePrefab;
            case PrototypeEnemyVariant.VariantType.Exploder:
                return exploderZombiePrefab != null ? exploderZombiePrefab : zombiePrefab;
            case PrototypeEnemyVariant.VariantType.MiniBoss:
                return miniBossZombiePrefab != null ? miniBossZombiePrefab : zombiePrefab;
            default:
                return zombiePrefab;
        }
    }

    static PrototypeEnemyVariant.VariantType PickFallbackVariant(int wave, int spawnIndex)
    {
        if (wave <= 1)
            return PrototypeEnemyVariant.VariantType.Grunt;
        if (wave == 2 && spawnIndex % 4 == 0)
            return PrototypeEnemyVariant.VariantType.Runner;

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

    Vector3 GetSpawnPosition()
    {
        Transform point = GetRandomSpawnPoint();
        if (useSpawnPointsWhenAvailable && point != null)
            return point.position + (Vector3)(Random.insideUnitCircle * spawnPointJitter);

        Vector3 center = campfireTarget != null ? campfireTarget.position : Vector3.zero;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = safeZoneRadius + spawnRingExtraDistance + Random.Range(0f, spawnRingRandomness);
        return center + new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
    }

    Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        int startIndex = Random.Range(0, spawnPoints.Length);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[(startIndex + i) % spawnPoints.Length];
            if (point != null)
                return point;
        }

        return null;
    }

    void UpdateZombieCountUI(bool force)
    {
        int remaining = Mathf.Max(0, totalZombiesThisWave - spawnedZombiesThisWave);
        if (!force && zombiesAlive == lastDisplayedZombiesAlive && remaining == lastDisplayedRemaining)
            return;

        lastDisplayedZombiesAlive = zombiesAlive;
        lastDisplayedRemaining = remaining;

        if (zombieCountText == null)
            return;

        zombieCountText.text = remaining > 0
            ? $"Zombies: {zombiesAlive}  Reinforcements: {remaining}"
            : "Zombies: " + zombiesAlive;
    }

    bool IsBossBlockingWaveEnd()
    {
        if (!bossWavePending)
            return false;
        if (!bossHasSpawned)
            return true;
        if (activeBoss != null && activeBoss.IsAlive)
            return true;

        bossWavePending = false;
        return false;
    }

    public int GetCurrentWave() => currentWave;
    public int GetZombiesAlive() => zombiesAlive;
    public bool IsWaveInProgress() => waveInProgress;
    public bool IsBossAlive() => bossWavePending && bossHasSpawned && activeBoss != null && activeBoss.IsAlive;

    public void SetAmmoDropChanceBonus(float bonus)
    {
        ammoDropChanceBonus = Mathf.Clamp(bonus, 0f, 0.5f);
        for (int i = 0; i < activeZombies.Count; i++)
        {
            ZombieAI zombie = activeZombies[i];
            if (zombie == null || !zombieSources.TryGetValue(zombie, out GameObject sourcePrefab))
                continue;

            if (prefabBaseStats.TryGetValue(sourcePrefab, out ZombieBaseStats stats))
                zombie.ammoDropChance = Mathf.Clamp01(stats.AmmoDropChance + ammoDropChanceBonus);
        }
    }

    static Transform FindCampfireTransform()
    {
        GameObject campfireObject = GameObject.Find("CampFire");
        if (campfireObject == null)
            campfireObject = GameObject.Find("Campfire");

        return campfireObject != null ? campfireObject.transform : null;
    }
}
