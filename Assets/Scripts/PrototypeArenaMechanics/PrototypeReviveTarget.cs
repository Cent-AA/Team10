using TMPro;
using UnityEngine;

public class PrototypeReviveTarget : MonoBehaviour
{
    public float baseRequiredDamage = 45f;
    public float requiredDamageIncrease = 25f;
    public float reviveHealthPercent = 0.3f;

    private PlayerController player;
    private EngineerController engineer;
    private Rigidbody2D rb;
    private TextMeshPro label;
    private bool downed;
    private int deaths;
    private float reviveProgress;
    private float requiredDamage;

    public bool IsDowned => downed;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        engineer = GetComponent<EngineerController>();
        rb = GetComponent<Rigidbody2D>();

        if (player != null)
            player.OnDeath += HandleDeath;
    }

    void OnDestroy()
    {
        if (player != null)
            player.OnDeath -= HandleDeath;
    }

    void Update()
    {
        if (!downed && IsHealthEmpty())
            HandleDeath();

        if (label != null)
            label.transform.rotation = Quaternion.identity;
    }

    public bool ReceiveReviveDamage(float amount, Transform reviver)
    {
        if (!downed)
            return false;

        if (reviver == transform || reviver == null)
            return true;

        reviveProgress = Mathf.Clamp(reviveProgress + Mathf.Abs(amount), 0f, requiredDamage);
        UpdateLabel();
        ArenaCamera.Shake(0.12f, 0.08f);

        if (reviveProgress >= requiredDamage)
            Revive();

        return true;
    }

    void HandleDeath()
    {
        if (downed)
            return;

        downed = true;
        deaths++;
        reviveProgress = 0f;
        requiredDamage = baseRequiredDamage + (deaths - 1) * requiredDamageIncrease;
        CreateLabel();
        UpdateLabel();
    }

    void Revive()
    {
        downed = false;
        reviveProgress = 0f;

        if (label != null)
            Destroy(label.gameObject);

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Dynamic;

        if (player != null)
        {
            player.SetHealth(player.maxHealth * reviveHealthPercent, player.maxHealth);
            if (player.puppet != null)
                player.puppet.Revive();
        }

        if (engineer != null)
        {
            engineer.Revive(reviveHealthPercent);
        }

        ArenaCamera.Shake(0.45f, 0.18f);
    }

    bool IsHealthEmpty()
    {
        if (player != null)
            return player.currentHealth <= 0f;

        if (engineer != null)
            return engineer.currentHealth <= 0f;

        return false;
    }

    void CreateLabel()
    {
        if (label != null)
            return;

        GameObject labelObject = new GameObject("ReviveProgress");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        label = labelObject.AddComponent<TextMeshPro>();
        label.fontSize = 3.2f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.5f, 0.95f, 1f, 1f);

        MeshRenderer meshRenderer = labelObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.sortingOrder = 200;
    }

    void UpdateLabel()
    {
        if (label == null)
            return;

        label.text = $"REVIVE\n{Mathf.CeilToInt(reviveProgress)} / {Mathf.CeilToInt(requiredDamage)}";
    }
}
