using System.Collections;
using UnityEngine;

public class PrototypeTurret : MonoBehaviour
{
    public Transform owner;
    public float range = 9f;
    public float targetRefreshInterval = 0.12f;
    public float fireInterval = 0.28f;
    public float damage = 9f;
    public float lifeTime = 35f;

    private static Material tracerMaterial;
    private static Sprite squareSprite;

    private Transform barrel;
    private LineRenderer tracerLine;
    private ZombieAI cachedTarget;
    private float targetRefreshTimer;
    private float fireTimer;
    private float deathTimer;

    void Start()
    {
        deathTimer = lifeTime;
        BuildVisuals();
    }

    void Update()
    {
        deathTimer -= Time.deltaTime;
        if (deathTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        targetRefreshTimer -= Time.deltaTime;
        if (!IsValidTarget(cachedTarget) || targetRefreshTimer <= 0f)
        {
            targetRefreshTimer = Mathf.Max(0.03f, targetRefreshInterval);
            cachedTarget = FindTarget();
        }

        if (cachedTarget == null)
            return;

        Vector2 direction = cachedTarget.transform.position - transform.position;
        if (barrel != null && direction.sqrMagnitude > 0.001f)
            barrel.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            fireTimer = fireInterval;
            Fire(cachedTarget, direction.normalized);
        }
    }

    ZombieAI FindTarget()
    {
        Registry.CleanupZombies();

        ZombieAI best = null;
        float bestDistanceSqr = range * range;

        for (int i = 0; i < Registry.Zombies.Count; i++)
        {
            ZombieAI zombie = Registry.Zombies[i];
            if (!IsValidTarget(zombie))
                continue;

            float distanceSqr = ((Vector2)zombie.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                best = zombie;
            }
        }

        return best;
    }

    bool IsValidTarget(ZombieAI zombie)
    {
        if (zombie == null || !zombie.gameObject.activeInHierarchy || !zombie.IsAlive)
            return false;

        float rangeSqr = range * range;
        return ((Vector2)zombie.transform.position - (Vector2)transform.position).sqrMagnitude <= rangeSqr;
    }

    void Fire(ZombieAI target, Vector2 direction)
    {
        if (target == null)
            return;

        target.TakeDamage(damage, direction * 0.4f, owner);
        StartCoroutine(TracerRoutine(target.transform.position));
    }

    IEnumerator TracerRoutine(Vector3 targetPosition)
    {
        LineRenderer line = GetTracerLine();
        if (line == null)
            yield break;

        line.gameObject.SetActive(true);
        line.positionCount = 2;
        line.startWidth = 0.055f;
        line.endWidth = 0.015f;
        line.material = GetTracerMaterial();
        line.startColor = new Color(0.4f, 0.9f, 1f, 1f);
        line.endColor = new Color(0.4f, 0.9f, 1f, 0f);
        line.SetPosition(0, transform.position + Vector3.up * 0.25f);
        line.SetPosition(1, targetPosition);

        yield return new WaitForSeconds(0.05f);
        if (line != null)
            line.gameObject.SetActive(false);
    }

    void BuildVisuals()
    {
        Transform existingBase = transform.Find("Base");
        Transform existingBarrel = transform.Find("Barrel");
        if (existingBase == null || existingBarrel == null)
        {
            Debug.LogWarning($"{nameof(PrototypeTurret)} on {name} is missing Base or Barrel child.", this);
            return;
        }

        Sprite square = GetSquareSprite();

        GameObject baseObject = existingBase.gameObject;
        baseObject.transform.SetParent(transform, false);
        SpriteRenderer baseRenderer = baseObject.GetComponent<SpriteRenderer>();
        if (baseRenderer == null)
            baseRenderer = baseObject.AddComponent<SpriteRenderer>();

        baseRenderer.sprite = square;
        baseRenderer.color = new Color(0.22f, 0.32f, 0.36f, 1f);
        baseObject.transform.localScale = new Vector3(0.7f, 0.5f, 1f);

        GameObject barrelObject = existingBarrel.gameObject;
        barrelObject.transform.SetParent(transform, false);
        barrel = barrelObject.transform;
        SpriteRenderer barrelRenderer = barrelObject.GetComponent<SpriteRenderer>();
        if (barrelRenderer == null)
            barrelRenderer = barrelObject.AddComponent<SpriteRenderer>();

        barrelRenderer.sprite = square;
        barrelRenderer.color = new Color(0.55f, 0.9f, 1f, 1f);
        barrelObject.transform.localPosition = new Vector3(0.25f, 0.12f, 0f);
        barrelObject.transform.localScale = new Vector3(0.8f, 0.18f, 1f);

        GetTracerLine();
    }

    LineRenderer GetTracerLine()
    {
        if (tracerLine != null)
            return tracerLine;

        Transform tracer = transform.Find("TurretTracer");
        if (tracer == null)
            return null;

        tracerLine = tracer.GetComponent<LineRenderer>();
        if (tracerLine == null)
            tracerLine = tracer.gameObject.AddComponent<LineRenderer>();

        tracerLine.gameObject.SetActive(false);
        return tracerLine;
    }

    Material GetTracerMaterial()
    {
        if (tracerMaterial == null)
            tracerMaterial = new Material(Shader.Find("Sprites/Default"));

        return tracerMaterial;
    }

    Sprite GetSquareSprite()
    {
        if (squareSprite == null)
            squareSprite = CreateSquareSprite(32);

        return squareSprite;
    }

    static Sprite CreateSquareSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, Color.white);
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
