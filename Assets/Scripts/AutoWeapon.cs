using UnityEngine;

public class AutoWeapon : MonoBehaviour
{
    [Header("РќР°СЃС‚СЂРѕР№РєРё СЃС‚СЂРµР»СЊР±С‹")]
    [SerializeField] private GameObject bulletPrefab; // РџСЂРµС„Р°Р± РїСѓР»Рё
    [SerializeField] private Transform firePoint;     // РћС‚РєСѓРґР° РІС‹Р»РµС‚Р°РµС‚ РїСѓР»СЏ
    [SerializeField] private float fireRate = 0.2f;    // РЎРєРѕСЂРѕСЃС‚СЊ СЃС‚СЂРµР»СЊР±С‹ (Р·Р°РґРµСЂР¶РєР° РјРµР¶РґСѓ РІС‹СЃС‚СЂРµР»Р°РјРё)

    [Header("РќР°СЃС‚СЂРѕР№РєРё РІСЂР°С‰РµРЅРёСЏ")]
    [SerializeField] private float rotationSpeed = 15f; // РЎРєРѕСЂРѕСЃС‚СЊ РїРѕРІРѕСЂРѕС‚Р° РѕСЂСѓР¶РёСЏ
    [SerializeField] private float targetRefreshInterval = 0.15f;

    private float shootTimer;
    private float targetRefreshTimer;
    private Transform targetEnemy;
    private Transform owner;
    public int playerNumber =1;

    void Awake()
    {
        PlayerController player = GetComponentInParent<PlayerController>();
        owner = player != null ? player.transform : transform.root;
    }

    void Update()
    {
        // РЈРјРµРЅСЊС€Р°РµРј С‚Р°Р№РјРµСЂ Р·Р°РґРµСЂР¶РєРё РєР°Р¶РґС‹Р№ РєР°РґСЂ
        if (shootTimer > 0)
        {
            shootTimer -= Time.deltaTime;
        }

        // 1. РћСЂСѓР¶РёРµ Р’РЎР•Р“Р”Рђ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё С†РµР»РёС‚СЃСЏ (РєСЂСѓС‚РёС‚СЃСЏ) Р·Р° Р±Р»РёР¶Р°Р№С€РёРј Р·РѕРјР±Рё
        targetRefreshTimer -= Time.deltaTime;
        if (targetRefreshTimer <= 0f)
        {
            targetRefreshTimer = targetRefreshInterval;
            FindClosestEnemy();
        }

        if (targetEnemy != null)
        {
            RotateTowardsTarget();
        }
        // 2. РЎРўР Р•Р›Р¬Р‘Рђ РўРћР›Р¬РљРћ РџРћ РќРђР–РђРўРР® (РР›Р Р—РђР–РђРўРР®) РљР›РђР’РРЁР J
        if (GetHeldInput(PlayerControlAction.Shoot )&& shootTimer <= 0f)
        {
             Shoot();
              shootTimer = fireRate; // РЎР±СЂР°СЃС‹РІР°РµРј С‚Р°Р№РјРµСЂ Р·Р°РґРµСЂР¶РєРё
        }
        //NIKITINO NAM NE NUZHNO
       /* if (Input.GetKey(KeyCode.J) && shootTimer <= 0f)
        {
            Shoot();
            shootTimer = fireRate; // РЎР±СЂР°СЃС‹РІР°РµРј С‚Р°Р№РјРµСЂ Р·Р°РґРµСЂР¶РєРё
        }*/
    }

    void FindClosestEnemy()
    {
        float closestDistSqr = float.PositiveInfinity;
        Transform closest = null;
        Vector2 selfPosition = transform.position;

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

        targetEnemy = closest;
    }

    void RotateTowardsTarget()
    {
        Vector2 direction = (targetEnemy.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            // РЎРїР°РІРЅРёРј РїСѓР»СЋ СЃ С‚РѕС‡РЅС‹Рј РїРѕРІРѕСЂРѕС‚РѕРј СЃС‚РІРѕР»Р°
            Bullet.Spawn(bulletPrefab, firePoint.position, firePoint.rotation, owner);
        }
    }
        bool GetHeldInput(PlayerControlAction action)
    {
        var type = GetInputType();
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