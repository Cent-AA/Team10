using UnityEngine;

public class ArenaCamera : MonoBehaviour
{
    [Header("═══ Цели ═══")]
    public Transform target1;
    public Transform target2;

    [Header("═══ Следование ═══")]
    public float followSmoothness = 5f;
    public Vector2 offset = Vector2.zero;

    [Header("═══ Зум ═══")]
    public float minZoom = 4f;            
    public float maxZoom = 8f;            
    public float zoomSmoothness = 3f;
    public float zoomPadding = 2f;        

    [Header("═══ Тряска ═══")]
    public float shakeDamping = 8f;

    [Header("═══ АВТО-ОГРАНИЧЕНИЕ ПО КАРТЕ ═══")]
    public SpriteRenderer mapSprite;      // СЮДА КИДАЙ СВОЙ ЛЕС (ArenaFoet_0)

    private Camera cam;
    private static float shakeIntensity = 0f;
    private static float shakeDuration = 0f;
    private Vector3 shakeOffset;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target1 == null && target2 == null) return;

        // 1. Считаем и плавно меняем зум
        UpdateZoom();

        // 2. Считаем дефолтную позицию между пацанами
        Vector3 targetPosition = GetTargetPosition();

        // 3. Считаем тряску
        UpdateShake();

        // 4. Плавно двигаем камеру в эту точку
        Vector3 finalPosition = targetPosition + shakeOffset;
        transform.position = Vector3.Lerp(transform.position, finalPosition, followSmoothness * Time.deltaTime);

        // 5. ЖЕСТКИЙ КУСТОДОРЕЗ (В САМОМ КОНЦЕ КАДРА!)
        // Обрезаем уже ФИНАЛЬНУЮ позицию самой камеры, перетирая любые лаги Лерпа
        if (mapSprite != null && cam != null)
        {
            Bounds mapBounds = mapSprite.bounds;

            // Вычисляем размеры экрана прямо в этот микромомент
            float camHeight = cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;

            // Лимиты для центра, чтобы края не оголяли синеву
            float minX = mapBounds.min.x + camWidth;
            float maxX = mapBounds.max.x - camWidth;
            float minY = mapBounds.min.y + camHeight;
            float maxY = mapBounds.max.y - camHeight;

            Vector3 clampedPos = transform.position;

            if (minX > maxX) clampedPos.x = mapBounds.center.x;
            else clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);

            if (minY > maxY) clampedPos.y = mapBounds.center.y;
            else clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY);

            // Принудительно сажаем камеру в рамки
            transform.position = clampedPos;
        }
    }

    Vector3 GetTargetPosition()
    {
        if (target1 != null && target2 == null) return new Vector3(target1.position.x + offset.x, target1.position.y + offset.y, transform.position.z);
        if (target2 != null && target1 == null) return new Vector3(target2.position.x + offset.x, target2.position.y + offset.y, transform.position.z);

        Vector2 center = (target1.position + target2.position) * 0.5f;
        return new Vector3(center.x + offset.x, center.y + offset.y, transform.position.z);
    }

    void UpdateZoom()
    {
        if (cam == null || !cam.orthographic) return;

        float targetZoom = minZoom;
        if (target1 != null && target2 != null)
        {
            float distance = Vector2.Distance(target1.position, target2.position);
            float t = Mathf.Clamp01((distance + zoomPadding) / 15f);
            targetZoom = Mathf.Lerp(minZoom, maxZoom, t);
        }

        // Не даем зуму стать больше, чем сама карта
        if (mapSprite != null)
        {
            Bounds mapBounds = mapSprite.bounds;
            float maxVerticalZoom = mapBounds.size.y / 2f;
            float maxHorizontalZoom = (mapBounds.size.x / 2f) / cam.aspect;
            
            float absoluteMaxZoom = Mathf.Min(maxVerticalZoom, maxHorizontalZoom);
            targetZoom = Mathf.Min(targetZoom, absoluteMaxZoom);
            
            // Если текущий зум уже вылетает — режем его без плавности
            if (cam.orthographicSize > absoluteMaxZoom)
            {
                cam.orthographicSize = absoluteMaxZoom;
            }
        }

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothness);
    }

    void UpdateShake()
    {
        if (shakeDuration > 0)
        {
            shakeDuration -= Time.deltaTime;
            shakeOffset = new Vector3((Random.value - 0.5f) * 2f, (Random.value - 0.5f) * 2f, 0) * shakeIntensity;
        }
        else
        {
            shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * shakeDamping);
            shakeIntensity = 0f;
        }
    }

    public static void Shake(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
    }
}