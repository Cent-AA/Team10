using UnityEngine;

public class PlayerBoundsLock : MonoBehaviour
{
    [Header("Настройки границ для игрока")]
    public float minX = -8.0f;
    public float maxX = 8.0f;
    public float minY = -4.5f;
    public float maxY = 4.5f;

    // Срабатывает ПОСЛЕ того, как отработала вся физика, удары зомби и перемещения
    void LateUpdate()
    {
        Vector3 pos = transform.position;

        // Если физика пробита или отключена, принудительно возвращаем в загон
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }
}