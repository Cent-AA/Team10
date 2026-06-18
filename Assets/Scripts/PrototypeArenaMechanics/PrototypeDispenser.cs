using UnityEngine;

public class PrototypeDispenser : MonoBehaviour
{
    public Transform owner;
    public float healRadius = 3.2f;
    public float healPerSecond = 12f;
    public float healTickInterval = 0.1f;
    public float lifeTime = 45f;

    private static Sprite circleSprite;
    private static Sprite squareSprite;

    private float deathTimer;
    private float healTimer;

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

        healTimer += Time.deltaTime;
        if (healTimer >= Mathf.Max(0.02f, healTickInterval))
        {
            HealPlayers(healTimer);
            healTimer = 0f;
        }
    }

    void HealPlayers(float deltaTime)
    {
        Registry.CleanupPlayers();

        float amount = healPerSecond * deltaTime;
        float healRadiusSqr = healRadius * healRadius;

        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform playerTransform = Registry.Players[i];
            if (playerTransform == null)
                continue;

            float distanceSqr = ((Vector2)playerTransform.position - (Vector2)transform.position).sqrMagnitude;
            if (distanceSqr > healRadiusSqr)
                continue;

            PlayerController player = playerTransform.GetComponent<PlayerController>();
            if (player == null)
                player = playerTransform.GetComponentInChildren<PlayerController>();

            if (player != null)
            {
                if (player.currentHealth > 0f)
                    player.SetHealth(player.currentHealth + amount, player.maxHealth);
                continue;
            }

            EngineerController engineer = playerTransform.GetComponent<EngineerController>();
            if (engineer == null)
                engineer = playerTransform.GetComponentInChildren<EngineerController>();

            if (engineer != null && engineer.currentHealth > 0f)
                engineer.SetHealth(engineer.currentHealth + amount, engineer.maxHealth);
        }
    }

    void BuildVisuals()
    {
        Sprite circle = GetCircleSprite();
        Sprite square = GetSquareSprite();

        GameObject auraObject = new GameObject("HealAura");
        auraObject.transform.SetParent(transform, false);
        SpriteRenderer aura = auraObject.AddComponent<SpriteRenderer>();
        aura.sprite = circle;
        aura.color = new Color(0.1f, 0.85f, 0.45f, 0.18f);
        auraObject.transform.localScale = Vector3.one * healRadius * 2f;

        GameObject coreObject = new GameObject("Core");
        coreObject.transform.SetParent(transform, false);
        SpriteRenderer core = coreObject.AddComponent<SpriteRenderer>();
        core.sprite = square;
        core.color = new Color(0.18f, 0.65f, 0.42f, 1f);
        coreObject.transform.localScale = new Vector3(0.7f, 0.8f, 1f);
    }

    Sprite GetSquareSprite()
    {
        if (squareSprite == null)
            squareSprite = CreateSquareSprite(32);

        return squareSprite;
    }

    Sprite GetCircleSprite()
    {
        if (circleSprite == null)
            circleSprite = CreateCircleSprite(64);

        return circleSprite;
    }

    static Sprite CreateSquareSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, Color.white);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    static Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = distance <= 1f ? Mathf.Clamp01(1f - distance * 0.35f) : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
