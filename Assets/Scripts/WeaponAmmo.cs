using UnityEngine;

public class WeaponAmmo : MonoBehaviour
{
    [SerializeField] private int maxAmmo = 60;
    [SerializeField] private int currentAmmo = 30;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public System.Action<int, int> OnAmmoChanged;

    void Awake()
    {
        maxAmmo = Mathf.Max(0, maxAmmo);
        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
    }

    public bool TryConsume(int amount)
    {
        amount = Mathf.Max(1, amount);
        if (currentAmmo < amount)
            return false;

        currentAmmo -= amount;
        NotifyAmmoChanged();
        return true;
    }

    public int AddAmmo(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount == 0 || currentAmmo >= maxAmmo)
            return 0;

        int previous = currentAmmo;
        currentAmmo = Mathf.Clamp(currentAmmo + amount, 0, maxAmmo);
        NotifyAmmoChanged();
        return currentAmmo - previous;
    }

    public void SetAmmo(int current, int max)
    {
        maxAmmo = Mathf.Max(0, max);
        currentAmmo = Mathf.Clamp(current, 0, maxAmmo);
        NotifyAmmoChanged();
    }

    void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }
}
