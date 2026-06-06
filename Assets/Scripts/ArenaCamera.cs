using UnityEngine;

public class ArenaCamera : MonoBehaviour
{
    [Header("═══ Цели ═══")]
    public Transform target1;
    public Transform target2;

    [Header("═══ Следование ═══")]
    public float followSmoothness = 5f;
    public Vector2 offset = Vector2.zero;
    public float maxDistance = 20f;
    public float minDistance = 0f;

    [Header("═══ Зум ═══")]
    public float minZoom = 4f;            // Близко
    public float maxZoom = 8f;            // Далеко
    public float zoomSmoothness = 3f;
    public float zoomPadding = 2f;        // Отступ по краям

    [Header("═══ Lookahead ═══")]
    public float lookaheadAmount = 1.5f;  // Камера смотрит чуть вперёд
    public float lookaheadSmoothness = 2f;

    [Header("═══ Тряска ═══")]
    public float shakeDamping = 8f;

    [Header("═══ Границы арены (опционально) ═══")]
    public bool useBounds = false;
    public Vector2 boundsMin = new Vector2(-15, -10);
    public Vector2 boundsMax = new Vector2(15, 10);

    private Camera cam;
    private Vector3 currentVelocity;
    private Vector2 currentLookahead;

    // Статические переменные для тряски (вызывается отовсюду)
    private static float shakeIntensity = 0f;
    private static float shakeDuration = 0f;
    private static Vector3 shakeOffset;

    public static ArenaCamera Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (target1 == null && target2 == null) return;

        // Центр между двумя игроками
        Vector3 centerPos = GetCenterPosition();

        // Lookahead в направлении движения
        Vector2 avgVelocity = GetAverageVelocity();
        Vector2 targetLookahead = avgVelocity.normalized * lookaheadAmount;
        currentLookahead = Vector2.Lerp(currentLookahead, targetLookahead, Time.deltaTime * lookaheadSmoothness);
        centerPos += new Vector3(currentLookahead.x, currentLookahead.y, 0);

        // Применяем offset
        centerPos += new Vector3(offset.x, offset.y, 0);
        centerPos.z = transform.position.z;

        // Тряска
        UpdateShake();

        // Плавное движение к цели
        Vector3 finalPos = Vector3.SmoothDamp(
            transform.position - shakeOffset,
            centerPos,
            ref currentVelocity,
            1f / followSmoothness);

        // Границы арены
        if (useBounds)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            finalPos.x = Mathf.Clamp(finalPos.x, boundsMin.x + halfWidth, boundsMax.x - halfWidth);
            finalPos.y = Mathf.Clamp(finalPos.y, boundsMin.y + halfHeight, boundsMax.y - halfHeight);
        }

        transform.position = finalPos + shakeOffset;

        // Динамический зум по расстоянию между игроками
        UpdateZoom();
    }

    Vector3 GetCenterPosition()
    {
        if (target1 != null && target2 != null)
            return (target1.position + target2.position) * 0.5f;
        if (target1 != null) return target1.position;
        return target2.position;
    }

    Vector2 GetAverageVelocity()
    {
        Vector2 v = Vector2.zero;
        int count = 0;
        if (target1 != null)
        {
            Rigidbody2D rb = target1.GetComponent<Rigidbody2D>();
            if (rb != null) { v += rb.linearVelocity; count++; }
        }
        if (target2 != null)
        {
            Rigidbody2D rb = target2.GetComponent<Rigidbody2D>();
            if (rb != null) { v += rb.linearVelocity; count++; }
        }
        return count > 0 ? v / count : Vector2.zero;
    }

    void UpdateZoom()
    {
        if (cam == null || !cam.orthographic) return;

        float targetZoom = minZoom;
        if (target1 != null && target2 != null)
        {
            float distance = Vector2.Distance(target1.position, target2.position);
            // Зум зависит от расстояния
            float t = Mathf.Clamp01((distance + zoomPadding) / 15f);
            targetZoom = Mathf.Lerp(minZoom, maxZoom, t);
        }

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothness);
    }

    void UpdateShake()
    {
        if (shakeDuration > 0)
        {
            shakeDuration -= Time.deltaTime;
            shakeOffset = new Vector3(
                (Random.value - 0.5f) * 2f,
                (Random.value - 0.5f) * 2f,
                0) * shakeIntensity;
        }
        else
        {
            shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * shakeDamping);
            shakeIntensity = 0f;
        }
    }

    // Публичный статический метод — вызывай из любого скрипта
    public static void Shake(float intensity, float duration)
    {
        if (Instance == null) return;
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeDuration = Mathf.Max(shakeDuration, duration);
    }
}
