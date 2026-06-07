using UnityEngine;

public class AutoWeapon : MonoBehaviour
{
    [Header("Настройки стрельбы")]
    [SerializeField] private GameObject bulletPrefab; // Префаб пули
    [SerializeField] private Transform firePoint;     // Откуда вылетает пуля
    [SerializeField] private float fireRate = 0.2f;    // Скорость стрельбы (задержка между выстрелами)

    [Header("Настройки вращения")]
    [SerializeField] private float rotationSpeed = 15f; // Скорость поворота оружия

    private float shootTimer;
    private Transform targetEnemy;

    void Update()
    {
        // Уменьшаем таймер задержки каждый кадр
        if (shootTimer > 0)
        {
            shootTimer -= Time.deltaTime;
        }

        // 1. Оружие ВСЕГДА автоматически целится (крутится) за ближайшим зомби
        FindClosestEnemy();

        if (targetEnemy != null)
        {
            RotateTowardsTarget();
        }

        // 2. СТРЕЛЬБА ТОЛЬКО ПО НАЖАТИЮ (ИЛИ ЗАЖАТИЮ) КЛАВИШИ J
        if (Input.GetKey(KeyCode.J) && shootTimer <= 0f)
        {
            Shoot();
            shootTimer = fireRate; // Сбрасываем таймер задержки
        }
    }

    void FindClosestEnemy()
    {
        ZombieAI[] enemies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (var enemy in enemies)
        {
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider == null || !enemyCollider.enabled) continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
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
            // Спавним пулю с точным поворотом ствола
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }
    }
}