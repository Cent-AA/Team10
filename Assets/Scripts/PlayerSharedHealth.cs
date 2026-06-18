using UnityEngine;

public class PlayerSharedHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public bool isDowned;

    public System.Action<float, float> OnHealthChanged;
    public System.Action OnDowned;
    public System.Action OnRevived;

    void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
    }

    void Start()
    {
        NotifyHealthChanged();
    }

    public void SetHealth(float current, float max)
    {
        bool wasDowned = isDowned;
        maxHealth = Mathf.Max(1f, max);
        currentHealth = Mathf.Clamp(current, 0f, maxHealth);
        isDowned = currentHealth <= 0f;
        NotifyHealthChanged();

        if (!wasDowned && isDowned)
            OnDowned?.Invoke();
        else if (wasDowned && !isDowned)
            OnRevived?.Invoke();
    }

    public void Heal(float amount)
    {
        if (isDowned)
            return;

        currentHealth = Mathf.Clamp(currentHealth + Mathf.Abs(amount), 0f, maxHealth);
        NotifyHealthChanged();
    }

    public void MultiplyHealth(float multiplier)
    {
        multiplier = Mathf.Max(0.01f, multiplier);
        SetHealth(currentHealth * multiplier, maxHealth * multiplier);
    }

    public bool TakeDamage(float damage)
    {
        if (isDowned)
            return false;

        currentHealth = Mathf.Clamp(currentHealth - Mathf.Abs(damage), 0f, maxHealth);
        isDowned = currentHealth <= 0f;
        NotifyHealthChanged();

        if (isDowned)
            OnDowned?.Invoke();

        return isDowned;
    }

    public void Revive(float healthPercent)
    {
        isDowned = false;
        currentHealth = Mathf.Clamp(maxHealth * Mathf.Clamp01(healthPercent), 1f, maxHealth);
        NotifyHealthChanged();
        OnRevived?.Invoke();
    }

    public void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
