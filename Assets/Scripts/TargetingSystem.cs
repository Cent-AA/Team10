using UnityEngine;
using System.Collections.Generic;

public class TargetingSystem : MonoBehaviour
{
    [Header("═══ Настройки ═══")]
    public int playerNumber = 1;
    public float detectRange = 10f;
    public float autoTargetRange = 6f;
    public LayerMask enemyLayer;

    [Header("═══ Визуал таргета ═══")]
    public Color targetColor = new Color(0.3f, 0.6f, 1f, 0.5f);  // Синий для P1
    public float triangleSize = 0.3f;
    public float triangleDistance = 1f;
    public float rotationSpeed = 90f;
    public int triangleCount = 4;

    [HideInInspector] public Transform currentTarget;

    private GameObject targetIndicator;
    private List<SpriteRenderer> triangles = new List<SpriteRenderer>();
    private float currentAngle = 0f;
    private readonly Collider2D[] targetHitBuffer = new Collider2D[32];

    void Start()
    {
        CreateTargetIndicator();
    }

    void Update()
    {
        FindTarget();
        UpdateIndicator();
    }

    void FindTarget()
    {
        ContactFilter2D targetFilter = new ContactFilter2D();
        targetFilter.SetLayerMask(enemyLayer);
        targetFilter.useLayerMask = true;
        targetFilter.useTriggers = Physics2D.queriesHitTriggers;

        int hitCount = Physics2D.OverlapCircle(transform.position, detectRange, targetFilter, targetHitBuffer);
        float closestDistSqr = autoTargetRange * autoTargetRange;
        Transform closest = null;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = targetHitBuffer[i];
            if (hit == null) continue;
            if (hit.transform == transform) continue;

            ZombieAI zombie = hit.GetComponent<ZombieAI>();
            PlayerController player = hit.GetComponent<PlayerController>();
            float distSqr = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;

            if (zombie != null)
            {
                if (distSqr < closestDistSqr)
                {
                    closestDistSqr = distSqr;
                    closest = hit.transform;
                }
            }
            else if (player != null && player.currentHealth > 0 && player.playerNumber != playerNumber)
            {
                if (distSqr < closestDistSqr)
                {
                    closestDistSqr = distSqr;
                    closest = hit.transform;
                }
            }

            targetHitBuffer[i] = null;
        }

        currentTarget = closest;
    }

    void CreateTargetIndicator()
    {
        targetIndicator = new GameObject("TargetIndicator_P" + playerNumber);

        for (int i = 0; i < triangleCount; i++)
        {
            GameObject triObj = new GameObject("Triangle" + i);
            triObj.transform.SetParent(targetIndicator.transform);

            SpriteRenderer sr = triObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateTriangleSprite();
            sr.color = targetColor;
            sr.sortingOrder = 100;

            triObj.transform.localScale = Vector3.one * triangleSize;
            triangles.Add(sr);
        }

        targetIndicator.SetActive(false);
    }

    void UpdateIndicator()
    {
        if (currentTarget == null)
        {
            targetIndicator.SetActive(false);
            return;
        }

        targetIndicator.SetActive(true);
        targetIndicator.transform.position = currentTarget.position;

        // Вращение
        currentAngle += rotationSpeed * Time.deltaTime;

        for (int i = 0; i < triangles.Count; i++)
        {
            float angle = currentAngle + (360f / triangleCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * triangleDistance;
            triangles[i].transform.localPosition = pos;

            // Треугольники смотрят на цель
            float lookAngle = angle + 90f;
            triangles[i].transform.localRotation = Quaternion.Euler(0, 0, lookAngle);

            // Пульсация прозрачности
            float pulse = 0.3f + Mathf.Sin(Time.time * 3f + i) * 0.2f;
            Color c = targetColor;
            c.a = pulse;
            triangles[i].color = c;
        }
    }

    Sprite CreateTriangleSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

        // Рисуем треугольник
        for (int y = 0; y < size; y++)
        {
            float t = (float)y / size;
            int halfWidth = Mathf.RoundToInt(t * size / 2f);
            int center = size / 2;
            for (int x = center - halfWidth; x <= center + halfWidth; x++)
            {
                if (x >= 0 && x < size)
                    tex.SetPixel(x, y, Color.white);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, autoTargetRange);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
