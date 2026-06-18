using UnityEngine;

public class PrototypeReviveManager : MonoBehaviour
{
    public float scanInterval = 0.35f;
    public float baseRequiredDamage = 45f;
    public float requiredDamageIncrease = 25f;
    public float reviveHealthPercent = 0.3f;

    private float scanTimer;

    void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f)
            return;

        scanTimer = scanInterval;
        EnsureTargets();
    }

    void EnsureTargets()
    {
        Registry.CleanupPlayers();

        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform player = Registry.Players[i];
            if (player != null)
                EnsureTarget(player.gameObject);
        }
    }

    void EnsureTarget(GameObject targetObject)
    {
        if (targetObject == null)
            return;

        PrototypeReviveTarget target = targetObject.GetComponent<PrototypeReviveTarget>();
        if (target == null)
            target = targetObject.AddComponent<PrototypeReviveTarget>();

        target.baseRequiredDamage = baseRequiredDamage;
        target.requiredDamageIncrease = requiredDamageIncrease;
        target.reviveHealthPercent = reviveHealthPercent;
    }
}
