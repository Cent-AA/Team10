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

    // События
    public System.Action<int> OnWaveStart;      // номер волны
    public System.Action<int> OnWaveComplete;
    public System.Action OnAllWavesComplete;

    void Start()
    {
        if (waveText != null) waveText.gameObject.SetActive(false);
        UpdateZombieCountUI();

        // Начинаем первую волну через паузу
        StartCoroutine(StartNextWaveAfterDelay(3f));
    }

    void Update()
    {
        // Очищаем мёртвых зомби из списка
        activeZombies.RemoveAll(z => z == null);
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
        Vector3 spawnPos;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPos = point.position + (Vector3)(Random.insideUnitCircle * 1f);
        }
        else
        {
            // Спавн по краям если точки не заданы
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = 8f;
            spawnPos = new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0);
        }

        GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);

        // Усиление по волнам
        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        if (ai != null)
        {
            float waveBonus = (currentWave - 1);
            ai.maxHealth += waveBonus * zombieHealthIncrease;
            ai.moveSpeed += waveBonus * zombieSpeedIncrease;
            ai.runSpeed += waveBonus * zombieSpeedIncrease;
            ai.attackDamage += waveBonus * zombieDamageIncrease;
        }

        activeZombies.Add(zombie);
        zombiesAlive++;
    }

    void UpdateZombieCountUI()
    {
        if (zombieCountText != null)
            zombieCountText.text = "Зомби: " + zombiesAlive;
    }

    // Публичные методы
    public int GetCurrentWave() => currentWave;
    public int GetZombiesAlive() => zombiesAlive;
    public bool IsWaveInProgress() => waveInProgress;
}