using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Zombie Spawning")]
    public GameObject zombiePrefab;
    public Transform[] spawnPoints;
    public Transform campfireTarget;
    public bool useSpawnPointsWhenAvailable = true;
    public float spawnPointJitter = 1f;
    public float safeZoneRadius = 7f;
    public float spawnRingExtraDistance = 4f;
    public float spawnRingRandomness = 3f;
    public int initialZombiePoolSize = 0;
    public int maxZombiePoolSize = 64;

    [Header("Wave Settings")]
    public int startZombies = 3;
    public int zombiesPerWave = 2;
    public float spawnInterval = 0.5f;
    public float timeBetweenWaves = 5f;
    public float zombieHealthIncrease = 10f;
    public float zombieSpeedIncrease = 0.3f;
    public float zombieDamageIncrease = 2f;

    [Header("UI")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI zombieCountText;
    public float waveTextDisplayTime = 3f;

    [Header("Camera")]
    public float waveShakeIntensity = 0.3f;

    private int currentWave = 0;
    private int zombiesAlive = 0;
    private int lastDisplayedZombiesAlive = -1;
    private int totalZombiesThisWave = 0;
    private bool waveInProgress = false;
    private bool spawningWave = false;
    private readonly List<ZombieAI> activeZombies = new List<ZombieAI>();
    private readonly Queue<ZombieAI> zombiePool = new Queue<ZombieAI>();
    private float baseZombieHealth;
    private float baseZombieMoveSpeed;
    private float baseZombieRunSpeed;
    private float baseZombieAttackDamage;
    private bool hasZombieBaseStats;

    public System.Action<int> OnWaveStart;
    public System.Action<int> OnWaveComplete;
    public System.Action OnAllWavesComplete;

    void Start()
    {
        if (campfireTarget == null)
        {
            CampfireController campfire = FindAnyObjectByType<CampfireController>();
            if (campfire != null) campfireTarget = campfire.transform;
        }

        CacheZombieBaseStats();
        PrewarmZombiePool();

        if (waveText != null) waveText.gameObject.SetActive(false);
        UpdateZombieCountUI(true);

        StartCoroutine(StartNextWaveAfterDelay(3f));
    }

    void Update()
    {
        if (waveInProgress && !spawningWave && zombiesAlive <= 0)
        {
            waveInProgress = false;
            OnWaveComplete?.Invoke(currentWave);
            StartCoroutine(StartNextWaveAfterDelay(timeBetweenWaves));
        }
    }

    IEnumerator StartNextWaveAfterDelay(float delay)
    {
        float remaining = delay;
        while (remaining > 0)
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
        waveInProgress = true;
        activeZombies.Clear();
        zombiesAlive = 0;
        UpdateZombieCountUI(true);

        OnWaveStart?.Invoke(currentWave);
        StartCoroutine(ShowWaveText());
        ArenaCamera.Shake(waveShakeIntensity, 0.5f);
        StartCoroutine(SpawnWaveZombies());
    }

    IEnumerator ShowWaveText()
    {
        if (waveText != null)
        {
            waveText.gameObject.SetActive(true);
            waveText.text = "Wave " + currentWave;

            float elapsed = 0f;
            while (elapsed < waveTextDisplayTime)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / waveTextDisplayTime) * 0.5f;
                float scale = 1f + Mathf.Sin(elapsed * 5f) * 0.05f;
                waveText.transform.localScale = Vector3.one * scale;

                Color c = waveText.color;
                c.a = alpha;
                waveText.color = c;

                yield return null;
            }

            waveText.gameObject.SetActive(false);
            Color resetColor = waveText.color;
            resetColor.a = 1f;
            waveText.color = resetColor;
            waveText.transform.localScale = Vector3.one;
        }
    }

    IEnumerator SpawnWaveZombies()
    {
        spawningWave = true;

        for (int i = 0; i < totalZombiesThisWave; i++)
        {
            SpawnZombie();
            yield return new WaitForSeconds(spawnInterval);
        }

        spawningWave = false;
    }

    void SpawnZombie()
    {
        if (zombiePrefab == null) return;

        Vector3 spawnPos = GetSpawnPosition();
        ZombieAI ai = GetZombieInstance(spawnPos);
        if (ai == null) return;

        ApplyWaveStats(ai);
        ai.SetPoolManaged(true);
        ai.ResetForSpawn(campfireTarget);
        ai.OnDied -= HandleZombieDied;
        ai.OnDied += HandleZombieDied;

        ai.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
        ai.gameObject.SetActive(true);

        if (!activeZombies.Contains(ai))
            activeZombies.Add(ai);

        zombiesAlive = activeZombies.Count;
        UpdateZombieCountUI(false);
    }

    ZombieAI GetZombieInstance(Vector3 spawnPos)
    {
        while (zombiePool.Count > 0)
        {
            ZombieAI pooledZombie = zombiePool.Dequeue();
            if (pooledZombie == null) continue;

            pooledZombie.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            return pooledZombie;
        }

        GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
        if (zombie == null)
            return null;

        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        if (ai == null)
        {
            Destroy(zombie);
            return null;
        }

        return ai;
    }

    void CacheZombieBaseStats()
    {
        if (zombiePrefab == null) return;

        ZombieAI ai = zombiePrefab.GetComponent<ZombieAI>();
        if (ai == null) return;

        baseZombieHealth = ai.maxHealth;
        baseZombieMoveSpeed = ai.moveSpeed;
        baseZombieRunSpeed = ai.runSpeed;
        baseZombieAttackDamage = ai.attackDamage;
        hasZombieBaseStats = true;
    }

    void ApplyWaveStats(ZombieAI ai)
    {
        if (ai == null) return;

        if (!hasZombieBaseStats)
        {
            baseZombieHealth = ai.maxHealth;
            baseZombieMoveSpeed = ai.moveSpeed;
            baseZombieRunSpeed = ai.runSpeed;
            baseZombieAttackDamage = ai.attackDamage;
            hasZombieBaseStats = true;
        }

        float waveBonus = currentWave - 1;
        ai.maxHealth = baseZombieHealth + waveBonus * zombieHealthIncrease;
        ai.moveSpeed = baseZombieMoveSpeed + waveBonus * zombieSpeedIncrease;
        ai.runSpeed = baseZombieRunSpeed + waveBonus * zombieSpeedIncrease;
        ai.attackDamage = baseZombieAttackDamage + waveBonus * zombieDamageIncrease;
    }

    void PrewarmZombiePool()
    {
        if (zombiePrefab == null || initialZombiePoolSize <= 0) return;

        for (int i = 0; i < initialZombiePoolSize; i++)
        {
            GameObject zombie = Instantiate(zombiePrefab);
            ZombieAI ai = zombie != null ? zombie.GetComponent<ZombieAI>() : null;
            if (ai == null)
            {
                if (zombie != null) Destroy(zombie);
                continue;
            }

            ai.SetPoolManaged(true);
            ai.OnDied -= HandleZombieDied;
            Registry.UnregisterZombie(ai);
            zombie.SetActive(false);
            zombiePool.Enqueue(ai);
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

        if (zombie == null) yield break;

        GameObject zombieObject = zombie.gameObject;

        if (zombiePool.Count >= maxZombiePoolSize)
        {
            Destroy(zombieObject);
            yield break;
        }

        zombieObject.SetActive(false);
        zombiePool.Enqueue(zombie);
    }

    Vector3 GetSpawnPosition()
    {
        Transform point = GetRandomSpawnPoint();
        if (useSpawnPointsWhenAvailable && point != null)
        {
            return point.position + (Vector3)(Random.insideUnitCircle * spawnPointJitter);
        }

        Vector3 center = campfireTarget != null ? campfireTarget.position : Vector3.zero;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = safeZoneRadius + spawnRingExtraDistance + Random.Range(0f, spawnRingRandomness);
        return center + new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
    }

    Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        int startIndex = Random.Range(0, spawnPoints.Length);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[(startIndex + i) % spawnPoints.Length];
            if (point != null) return point;
        }

        return null;
    }

    void UpdateZombieCountUI(bool force)
    {
        if (!force && zombiesAlive == lastDisplayedZombiesAlive)
            return;

        lastDisplayedZombiesAlive = zombiesAlive;

        if (zombieCountText != null)
        {
            zombieCountText.text = "Zombies: " + zombiesAlive + "/" + totalZombiesThisWave;
        }
    }

    public int GetCurrentWave() => currentWave;
    public int GetZombiesAlive() => zombiesAlive;
    public bool IsWaveInProgress() => waveInProgress;
}

