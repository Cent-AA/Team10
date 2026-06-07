using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("═══ Зомби ═══")]
    public GameObject zombiePrefab;
    public Transform[] spawnPoints;            // Точки спавна по краям арены
    public Transform campfireTarget;
    public bool useSpawnPointsWhenAvailable = true;
    public float spawnPointJitter = 1f;
    public float safeZoneRadius = 7f;
    public float spawnRingExtraDistance = 4f;
    public float spawnRingRandomness = 3f;
    public int initialZombiePoolSize = 0;
    public int maxZombiePoolSize = 64;

    [Header("═══ Волны ═══")]
    public int startZombies = 3;               // Зомби на первой волне
    public int zombiesPerWave = 2;             // +N зомби каждую волну
    public float spawnInterval = 0.5f;         // Задержка между спавнами
    public float timeBetweenWaves = 5f;        // Пауза между волнами
    public float zombieHealthIncrease = 10f;   // +HP каждую волну
    public float zombieSpeedIncrease = 0.3f;   // +скорость каждую волну
    public float zombieDamageIncrease = 2f;    // +урон каждую волну

    [Header("═══ UI ═══")]
    public TextMeshProUGUI waveText;                       // "Волна 3"
    public TextMeshProUGUI zombieCountText;                // "Зомби: 5"
    public float waveTextDisplayTime = 3f;

    [Header("═══ Камера ═══")]
    public float waveShakeIntensity = 0.3f;

    // Состояние
    private int currentWave = 0;
    private int zombiesAlive = 0;
    private int totalZombiesThisWave = 0;
    private bool waveInProgress = false;
    private List<GameObject> activeZombies = new List<GameObject>();
    private Queue<GameObject> zombiePool = new Queue<GameObject>();
    private float baseZombieHealth;
    private float baseZombieMoveSpeed;
    private float baseZombieRunSpeed;
    private float baseZombieAttackDamage;
    private bool hasZombieBaseStats;

    // События
    public System.Action<int> OnWaveStart;      // номер волны
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
        UpdateZombieCountUI();

        // Начинаем первую волну через паузу
        StartCoroutine(StartNextWaveAfterDelay(3f));
    }

    void Update()
    {
        // Очищаем мёртвых зомби из списка
        activeZombies.RemoveAll(IsZombieInactive);
        zombiesAlive = activeZombies.Count;

        UpdateZombieCountUI();

        // Все зомби убиты — следующая волна
        if (waveInProgress && zombiesAlive <= 0)
        {
            waveInProgress = false;
            OnWaveComplete?.Invoke(currentWave);
            StartCoroutine(StartNextWaveAfterDelay(timeBetweenWaves));
        }
    }

    IEnumerator StartNextWaveAfterDelay(float delay)
    {
        // Отсчёт
        float remaining = delay;
        while (remaining > 0)
        {
            if (waveText != null)
            {
                waveText.gameObject.SetActive(true);
                if (currentWave == 0)
                    waveText.text = "ПРИГОТОВЬСЯ!\n" + Mathf.CeilToInt(remaining);
                else
                    waveText.text = "ВОЛНА " + currentWave + " ПРОЙДЕНА!\nСЛЕДУЮЩАЯ ЧЕРЕЗ " + Mathf.CeilToInt(remaining);
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

        OnWaveStart?.Invoke(currentWave);

        // Показываем номер волны
        StartCoroutine(ShowWaveText());

        // Тряска камеры
        ArenaCamera.Shake(waveShakeIntensity, 0.5f);

        // Спавним зомби
        StartCoroutine(SpawnWaveZombies());
    }

    IEnumerator ShowWaveText()
    {
        if (waveText != null)
        {
            waveText.gameObject.SetActive(true);
            waveText.text = "ВОЛНА " + currentWave;

            // Мерцание
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
        for (int i = 0; i < totalZombiesThisWave; i++)
        {
            SpawnZombie();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnZombie()
    {
        if (zombiePrefab == null) return;

        // Выбираем случайную точку спавна
        Vector3 spawnPos = GetSpawnPosition();

        GameObject zombie = GetZombieInstance(spawnPos);

        // Усиление по волнам
        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        if (ai != null)
        {
            ApplyWaveStats(ai);
            ai.SetPoolManaged(true);
            ai.ResetForSpawn(campfireTarget);
            ai.OnDied -= HandleZombieDied;
            ai.OnDied += HandleZombieDied;
        }

        zombie.SetActive(true);
        activeZombies.Add(zombie);
        zombiesAlive++;
    }

    GameObject GetZombieInstance(Vector3 spawnPos)
    {
        while (zombiePool.Count > 0)
        {
            GameObject pooledZombie = zombiePool.Dequeue();
            if (pooledZombie == null) continue;

            pooledZombie.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            return pooledZombie;
        }

        return Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
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
            ZombieAI ai = zombie.GetComponent<ZombieAI>();
            if (ai != null)
            {
                ai.SetPoolManaged(true);
                ai.OnDied -= HandleZombieDied;
                Registry.UnregisterZombie(ai);
            }

            zombie.SetActive(false);
            zombiePool.Enqueue(zombie);
        }
    }

    void HandleZombieDied(ZombieAI zombie)
    {
        if (zombie != null)
        {
            StartCoroutine(ReturnZombieToPoolAfterDelay(zombie));
        }
    }

    IEnumerator ReturnZombieToPoolAfterDelay(ZombieAI zombie)
    {
        yield return new WaitForSeconds(zombie.DeathDespawnDelay);

        if (zombie == null) yield break;

        zombie.OnDied -= HandleZombieDied;
        GameObject zombieObject = zombie.gameObject;

        if (zombiePool.Count >= maxZombiePoolSize)
        {
            Destroy(zombieObject);
            yield break;
        }

        zombieObject.SetActive(false);
        zombiePool.Enqueue(zombieObject);
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

    void UpdateZombieCountUI()
    {
        if (zombieCountText != null)
            zombieCountText.text = "Зомби: " + zombiesAlive;
    }

    // Публичные методы
    bool IsZombieInactive(GameObject zombie)
    {
        if (zombie == null) return true;

        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        return ai == null || !ai.IsAlive;
    }

    public int GetCurrentWave() => currentWave;
    public int GetZombiesAlive() => zombiesAlive;
    public bool IsWaveInProgress() => waveInProgress;
}
