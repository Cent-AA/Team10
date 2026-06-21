using UnityEngine;

public class AutoWeapon : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private bool requireAmmo = true;
    [SerializeField] private bool canShootWithoutTarget = true;

    [Header("Targeting")]
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float targetRefreshInterval = 0.15f;
    [SerializeField] private float zombieTargetRange = 14f;
    [SerializeField] private float downedAllyTargetRange = 18f;

    private float shootTimer;
    private float targetRefreshTimer;
    private Transform target;
    private Transform owner;
    private WeaponAmmo ammo;

    public int playerNumber = 1;

    void Awake()
    {
        PlayerController player = GetComponentInParent<PlayerController>();
        EngineerController engineer = GetComponentInParent<EngineerController>();
        owner = player != null ? player.transform : engineer != null ? engineer.transform : transform.root;
        ammo = owner != null ? owner.GetComponent<WeaponAmmo>() : null;

        if (requireAmmo && ammo == null && owner != null)
            ammo = owner.gameObject.AddComponent<WeaponAmmo>();
    }

    void Update()
    {
        if (shootTimer > 0f)
            shootTimer -= Time.deltaTime;

        targetRefreshTimer -= Time.deltaTime;
        if (targetRefreshTimer <= 0f)
        {
            targetRefreshTimer = targetRefreshInterval;
            FindBestTarget();
        }

        if (target != null)
            RotateTowardsTarget();

        if (GetHeldInput(PlayerControlAction.Shoot) && shootTimer <= 0f)
        {
            if (Shoot())
                shootTimer = fireRate;
        }
    }

    void FindBestTarget()
    {
        target = FindClosestDownedAlly();
        if (target != null)
            return;

        target = FindClosestZombie();
    }

    Transform FindClosestDownedAlly()
    {
        float closestDistSqr = downedAllyTargetRange * downedAllyTargetRange;
        Transform closest = null;
        Vector2 selfPosition = transform.position;

        Registry.CleanupPlayers();
        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform player = Registry.Players[i];
            if (player == null || IsOwner(player)) continue;

            PrototypeReviveTarget reviveTarget = player.GetComponent<PrototypeReviveTarget>();
            if (reviveTarget == null)
                reviveTarget = player.GetComponentInChildren<PrototypeReviveTarget>();

            if (reviveTarget == null || !reviveTarget.IsDowned) continue;

            float distSqr = ((Vector2)reviveTarget.transform.position - selfPosition).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = reviveTarget.transform;
            }
        }

        return closest;
    }

    Transform FindClosestZombie()
    {
        float closestDistSqr = zombieTargetRange * zombieTargetRange;
        Transform closest = null;
        Vector2 selfPosition = transform.position;

        Registry.CleanupZombies();
        for (int i = 0; i < Registry.Zombies.Count; i++)
        {
            ZombieAI enemy = Registry.Zombies[i];
            if (enemy == null || !enemy.IsAlive || !enemy.HasActiveCollider) continue;

            float distSqr = ((Vector2)enemy.transform.position - selfPosition).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = enemy.transform;
            }
        }

        return closest;
    }

    void RotateTowardsTarget()
    {
        Vector2 direction = (target.position - transform.position).normalized;
        if (direction.sqrMagnitude <= 0.0001f) return;

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    bool Shoot()
    {
        PlayerController pc = owner.GetComponent<PlayerController>();
        if (pc != null && pc.currentHealth <= 0f) return true;
        EngineerController ec = owner.GetComponent<EngineerController>();
        // need to clean this up later but idk if this script will even be used for sniper or medic
        if (bulletPrefab == null || firePoint == null) return false;
        if (target == null && !canShootWithoutTarget) return false;
        if (requireAmmo && ammo != null && !ammo.TryConsume(1)) return false;

        Bullet.Spawn(bulletPrefab, firePoint.position, firePoint.rotation, owner);
        return true;
    }

    bool IsOwner(Transform candidate)
    {
        if (candidate == null || owner == null) return false;
        return candidate == owner || candidate.IsChildOf(owner) || owner.IsChildOf(candidate);
    }

    bool GetHeldInput(PlayerControlAction action)
    {
        InputJoinManager.InputType type = GetInputType();
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
            case InputJoinManager.InputType.KeyboardArrows:
                return PlayerInputBindings.GetKeyboardAction(playerNumber, action);
            case InputJoinManager.InputType.Gamepad:
                return PlayerInputBindings.GetGamepadAction(playerNumber, action, GetGamepadIndex());
        }

        return false;
    }

    InputJoinManager.InputType GetInputType()
    {
        return playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
    }

    int GetGamepadIndex()
    {
        return playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;
    }
}
