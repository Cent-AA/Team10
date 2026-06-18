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
    private static readonly Collider2D[] explosionHitBuffer = new Collider2D[48];

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

        float healthMultiplier = 1f;
        float speedMultiplier = 1f;
        float damageMultiplier = 1f;
        float scaleMultiplier = 1f;
        Color color = Color.white;

        switch (type)
        {
            case VariantType.Runner:
                healthMultiplier = 0.7f;
                speedMultiplier = 1.7f;
                damageMultiplier = 0.85f;
                scaleMultiplier = 0.88f;
                color = new Color(0.65f, 1f, 0.55f, 1f);
                break;

            case VariantType.Tank:
                healthMultiplier = 2.3f;
                speedMultiplier = 0.68f;
                damageMultiplier = 1.45f;
                scaleMultiplier = 1.28f;
                color = new Color(0.95f, 0.72f, 0.45f, 1f);
                break;

            case VariantType.Exploder:
                healthMultiplier = 0.95f;
                speedMultiplier = 1.15f;
                damageMultiplier = 0.75f;
                scaleMultiplier = 1.05f;
                color = new Color(1f, 0.38f, 0.3f, 1f);
                break;

            case VariantType.MiniBoss:
                healthMultiplier = 7.5f;
                speedMultiplier = 0.8f;
                damageMultiplier = 2.2f;
                scaleMultiplier = 1.85f;
                color = new Color(0.9f, 0.25f, 1f, 1f);
                break;
        }

        zombie.maxHealth *= healthMultiplier;
        zombie.moveSpeed *= speedMultiplier;
        zombie.runSpeed *= speedMultiplier;
        zombie.attackDamage *= damageMultiplier;
        transform.localScale = baseScale * scaleMultiplier;

        zombie.ResetForSpawn(zombie.campfireTarget);

        if (spriteRenderer != null)
            spriteRenderer.color = color;

        zombie.OnDied -= HandleDied;
        zombie.OnDied += HandleDied;
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
            Explode(18f, 2.5f);
        else if (Type == VariantType.MiniBoss)
            Explode(28f, 3.4f);
    }

    void Explode(float damage, float radius)
    {
        ArenaCamera.Shake(0.55f, 0.25f);

        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, radius, explosionHitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = explosionHitBuffer[i];
            if (hit == null)
                continue;

            Vector2 direction = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;

            PlayerController player = hit.GetComponent<PlayerController>();
            if (player != null)
                player.TakeDamage(damage, direction);

            EngineerController engineer = hit.GetComponent<EngineerController>();
            if (engineer != null)
                engineer.TakeDamage(damage, direction);
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
