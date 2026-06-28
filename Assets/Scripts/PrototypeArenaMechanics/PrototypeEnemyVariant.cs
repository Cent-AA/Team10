using UnityEngine;

public class PrototypeEnemyVariant : MonoBehaviour
{
    public enum VariantType
    {
        Grunt,
        Runner,
        Tank,
        Exploder,
        MiniBoss
    }

    private ZombieAI zombie;
    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private bool baseScaleCaptured;
    private Color appliedColor = Color.white;
    private static readonly Collider2D[] explosionHitBuffer = new Collider2D[48];
    private readonly System.Collections.Generic.HashSet<Transform> explosionVictims = new System.Collections.Generic.HashSet<Transform>();

    public int AppliedWave { get; private set; } = -1;
    public VariantType Type { get; private set; }

    void Awake()
    {
        Cache();
    }

    void OnDestroy()
    {
        if (zombie != null)
            zombie.OnDied -= HandleDied;
    }

    public void Apply(VariantType type, int wave)
    {
        StopAllCoroutines();
        Cache();

        if (zombie == null)
            return;

        if (!baseScaleCaptured)
        {
            baseScale = transform.localScale;
            baseScaleCaptured = true;
        }

        Type = type;
        AppliedWave = wave;
        transform.localScale = baseScale;

        float healthMultiplier = 1f;
        float speedMultiplier = 1f;
        float damageMultiplier = 1f;
        float scaleMultiplier = 1f;
        Color color = Color.white;

        switch (type)
        {
            case VariantType.Runner:
                healthMultiplier = 0.65f;
                speedMultiplier = 1.45f;
                damageMultiplier = 0.65f;
                scaleMultiplier = 0.88f;
                color = new Color(0.65f, 1f, 0.55f, 1f);
                break;

            case VariantType.Tank:
                healthMultiplier = 2f;
                speedMultiplier = 0.65f;
                damageMultiplier = 1f;
                scaleMultiplier = 1.28f;
                color = new Color(0.95f, 0.72f, 0.45f, 1f);
                break;

            case VariantType.Exploder:
                healthMultiplier = 0.75f;
                speedMultiplier = 1.05f;
                damageMultiplier = 0.75f;
                scaleMultiplier = 1.05f;
                color = new Color(1f, 0.38f, 0.3f, 1f);
                break;

            case VariantType.MiniBoss:
                healthMultiplier = 4.5f;
                speedMultiplier = 0.75f;
                damageMultiplier = 1.35f;
                scaleMultiplier = 1.65f;
                color = new Color(0.9f, 0.25f, 1f, 1f);
                break;
        }

        zombie.maxHealth *= healthMultiplier;
        zombie.moveSpeed *= speedMultiplier;
        zombie.runSpeed *= speedMultiplier;
        zombie.attackDamage *= damageMultiplier;
        zombie.SetArchetype(type);
        transform.localScale = baseScale * scaleMultiplier;
        appliedColor = color;
        RefreshVisuals();

        zombie.OnDied -= HandleDied;
        zombie.OnDied += HandleDied;
    }

    public void RefreshVisuals()
    {
        Cache();
        if (spriteRenderer != null)
            spriteRenderer.color = appliedColor;
    }

    void Cache()
    {
        if (zombie == null)
            zombie = GetComponent<ZombieAI>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void HandleDied(ZombieAI deadZombie)
    {
        if (PrototypeRunStats.Instance != null)
            PrototypeRunStats.Instance.RegisterKill();

        if (Type == VariantType.Exploder)
            StartCoroutine(ExplodeAfterWarning(1f, 14f, 2.5f));
        else if (Type == VariantType.MiniBoss)
            StartCoroutine(ExplodeAfterWarning(0.65f, 18f, 3f));
    }

    System.Collections.IEnumerator ExplodeAfterWarning(float warningDuration, float damage, float radius)
    {
        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                float pulse = Mathf.PingPong(elapsed * 8f, 1f);
                spriteRenderer.color = Color.Lerp(appliedColor, Color.white, pulse);
            }
            yield return null;
        }

        Explode(damage, radius);
    }

    void Explode(float damage, float radius)
    {
        ArenaCamera.Shake(0.55f, 0.25f);
        explosionVictims.Clear();

        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, radius, explosionHitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = explosionHitBuffer[i];
            if (hit == null)
                continue;

            PlayerController player = hit.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                if (!explosionVictims.Add(player.transform))
                    continue;

                Vector2 direction = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                float previousHealth = player.currentHealth;
                player.TakeDamage(damage, direction);
                if (player.currentHealth < previousHealth)
                    PixelBloodOverlay.PlayForPlayer(player.playerNumber, damage);
                continue;
            }

            EngineerController engineer = hit.GetComponentInParent<EngineerController>();
            if (engineer != null)
            {
                if (!explosionVictims.Add(engineer.transform))
                    continue;

                Vector2 direction = ((Vector2)engineer.transform.position - (Vector2)transform.position).normalized;
                float previousHealth = engineer.currentHealth;
                engineer.TakeDamage(damage, direction);
                if (engineer.currentHealth < previousHealth)
                    PixelBloodOverlay.PlayForPlayer(engineer.playerNumber, damage);
            }
        }

        if (PrototypeCampfireHealth.Instance != null && PrototypeCampfireHealth.Instance.IsDestroyed == false)
        {
            Transform campfire = PrototypeCampfireHealth.Instance.campfire != null
                ? PrototypeCampfireHealth.Instance.campfire
                : PrototypeCampfireHealth.Instance.transform;
            float distance = Vector2.Distance(transform.position, campfire.position);
            if (distance <= radius + 1.5f)
                PrototypeCampfireHealth.Instance.TakeDamage(damage);
        }
    }
}
