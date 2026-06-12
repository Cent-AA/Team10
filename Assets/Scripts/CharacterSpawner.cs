using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("═══ Префабы персонажей ═══")]
    public GameObject heavyPrefab;
    public GameObject engineerPrefab;
    public GameObject medicPrefab;       // Пока null — будет спавнить Heavy
    public GameObject sniperPrefab;      // Пока null — будет спавнить Heavy

    [Header("═══ Точки спавна ═══")]
    public Transform spawnPoint1;
    public Transform spawnPoint2;

    [Header("═══ Камера ═══")]
    public ArenaCamera arenaCamera;            // Если есть ArenaCamera
    public Camera splitCamP1;                  // Или сплит-скрин камера P1
    public Camera splitCamP2;                  // Или сплит-скрин камера P2

    private GameObject player1Object;
    private GameObject player2Object;

    void Start()
    {
        player1Object = SpawnCharacter(CharacterSelector.player1Character, spawnPoint1, 1);
        player2Object = SpawnCharacter(CharacterSelector.player2Character, spawnPoint2, 2);

        // ArenaCamera — следит за обоими
        if (arenaCamera != null)
        {
            if (player1Object != null) arenaCamera.target1 = player1Object.transform;
            if (player2Object != null) arenaCamera.target2 = player2Object.transform;
        }
    }

    // Публичный доступ к игрокам (для камер и других систем)
    public static Transform GetPlayer(int number)
    {
        CharacterSpawner spawner = FindFirstObjectByType<CharacterSpawner>();
        if (spawner == null) return null;
        if (number == 1 && spawner.player1Object != null) return spawner.player1Object.transform;
        if (number == 2 && spawner.player2Object != null) return spawner.player2Object.transform;
        return null;
    }

    GameObject SpawnCharacter(int characterIndex, Transform spawnPoint, int playerNumber)
    {
        GameObject prefab = GetPrefab(characterIndex);
        if (prefab == null)
        {
            Debug.LogWarning("Нет префаба для персонажа " + characterIndex + ", спавню Heavy");
            prefab = heavyPrefab;
        }
        if (prefab == null)
        {
            Debug.LogError("Heavy префаб не назначен!");
            return null;
        }

        GameObject player = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        player.name = "Player" + playerNumber;

        // Назначаем номер игрока для PlayerController
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.playerNumber = playerNumber;
            // Подключаем таргет
            TargetingSystem ts = player.GetComponent<TargetingSystem>();
            if (ts != null)
            {
                ts.playerNumber = playerNumber;
                ts.targetColor = playerNumber == 1
                    ? new Color(0.3f, 0.6f, 1f, 0.5f)
                    : new Color(1f, 0.7f, 0.2f, 0.5f);
                if (pc.targeting == null) pc.targeting = ts;
            }
        }

        // Назначаем номер для EngineerController
        EngineerController ec = player.GetComponent<EngineerController>();
        if (ec != null) ec.playerNumber = playerNumber;

        Debug.Log("Заспавнен Player" + playerNumber + ": персонаж " + characterIndex
            + " | Ввод: " + (playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input));

        return player;
    }

    GameObject GetPrefab(int index)
    {
        switch (index)
        {
            case 0: return heavyPrefab;
            case 1: return engineerPrefab;
            case 2: return medicPrefab != null ? medicPrefab : heavyPrefab;     // Заглушка
            case 3: return sniperPrefab != null ? sniperPrefab : heavyPrefab;   // Заглушка
            default: return heavyPrefab;
        }
    }
}