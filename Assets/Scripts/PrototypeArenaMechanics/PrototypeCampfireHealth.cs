using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrototypeCampfireHealth : MonoBehaviour
{
    public static PrototypeCampfireHealth Instance { get; private set; }

    [Header("Campfire")]
    public Transform campfire;
    public float maxHealth = 220f;
    public float contactDamagePerZombie = 8f;
    public float damageRadius = 1.7f;
    public float damageTickInterval = 0.5f;

    private float currentHealth;
    private float tickTimer;
    private bool destroyed;
    private TextMeshProUGUI healthText;
    private Image healthFill;
    private readonly Collider2D[] damageHitBuffer = new Collider2D[64];

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDestroyed => destroyed;

    void Awake()
    {
        Instance = this;
        currentHealth = Mathf.Max(1f, maxHealth);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (campfire == null)
            campfire = FindCampfireTransform();

        CreateHud();
        UpdateHud();
    }

    void Update()
    {
        if (destroyed || campfire == null)
            return;

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f)
            return;

        tickTimer = Mathf.Max(0.05f, damageTickInterval);
        DamageFromNearbyZombies();
    }

    public void Repair(float amount)
    {
        if (destroyed)
            return;

        currentHealth = Mathf.Clamp(currentHealth + Mathf.Abs(amount), 0f, maxHealth);
        UpdateHud();
    }

    public void IncreaseMaxHealth(float amount)
    {
        if (destroyed)
            return;

        float bonus = Mathf.Abs(amount);
        maxHealth += bonus;
        currentHealth += bonus;
        UpdateHud();
    }

    public void TakeDamage(float amount)
    {
        if (destroyed)
            return;

        currentHealth = Mathf.Clamp(currentHealth - Mathf.Abs(amount), 0f, maxHealth);
        ArenaCamera.Shake(0.22f, 0.15f);
        UpdateHud();

        if (currentHealth <= 0f)
            DestroyCampfire();
    }

    void DamageFromNearbyZombies()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(campfire.position, damageRadius, damageHitBuffer);
        float damage = 0f;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = damageHitBuffer[i];
            if (hit == null)
                continue;

            ZombieAI zombie = hit.GetComponent<ZombieAI>();
            if (zombie == null)
                zombie = hit.GetComponentInParent<ZombieAI>();

            if (zombie != null && zombie.IsAlive)
                damage += contactDamagePerZombie;
        }

        if (damage > 0f)
            TakeDamage(damage);
    }

    void DestroyCampfire()
    {
        destroyed = true;
        currentHealth = 0f;
        UpdateHud();

        if (PrototypeRunStats.Instance != null)
            PrototypeRunStats.Instance.EndRun("The campfire was destroyed");
    }

    void CreateHud()
    {
        Canvas canvas = PrototypeArenaUi.GetOrCreateCanvas("PrototypeArenaHUD", 5500);
        Image frame = PrototypeArenaUi.CreatePanel(
            canvas.transform,
            "CampfireHealthFrame",
            new Color(0.03f, 0.025f, 0.02f, 0.72f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -34f),
            new Vector2(520f, 54f));

        healthFill = PrototypeArenaUi.CreatePanel(
            frame.transform,
            "Fill",
            new Color(1f, 0.45f, 0.08f, 0.95f),
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            Vector2.zero,
            new Vector2(-10f, -10f));
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;

        healthText = PrototypeArenaUi.CreateText(
            frame.transform,
            "Label",
            "",
            24,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
    }

    void UpdateHud()
    {
        if (healthFill != null)
            healthFill.fillAmount = maxHealth > 0f ? currentHealth / maxHealth : 0f;

        if (healthText != null)
            healthText.text = $"Campfire {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
    }

    void OnDrawGizmosSelected()
    {
        Transform target = campfire != null ? campfire : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position, damageRadius);
    }

    static Transform FindCampfireTransform()
    {
        GameObject campfireObject = GameObject.Find("CampFire");
        if (campfireObject == null)
            campfireObject = GameObject.Find("Campfire");

        return campfireObject != null ? campfireObject.transform : null;
    }
}
