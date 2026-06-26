using UnityEngine;

public class Turret : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform[] firePoints;
    [SerializeField] private float fireRate = 0.5f;

    private float fireTimer;

    [Header("10 сторон турели")]
    public Sprite[] directions;

    public float detectionRadius = 5f;

    private SpriteRenderer spriteRenderer;
    private Transform target;

    private int currentDirection;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        FindZombie();

        if (target != null)
        {
            RotateSprite();

            fireTimer += Time.deltaTime;

            if (fireTimer >= fireRate)
            {
                Shoot();
                fireTimer = 0f;
            }
        }
    }

    void FindZombie()
    {
        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");

        target = null;

        float closestDistance = detectionRadius;

        foreach (GameObject zombie in zombies)
        {
            if (!zombie.activeInHierarchy)
                continue;

            float distance = Vector2.Distance(transform.position, zombie.transform.position);

            if (distance <= closestDistance)
            {
                closestDistance = distance;
                target = zombie.transform;
            }
        }
    }

    void RotateSprite()
    {
        Vector2 dir = target.position - transform.position;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (angle < 0)
            angle += 360;

        angle = 360f - angle;

        if (angle >= 360f)
            angle -= 360f;

        int index = 0;

        // ¬право
        if (angle >= 337.5f || angle < 22.5f)
            index = 7;

        // ¬низ-вправо (немного)
        else if (angle < 45f)
            index = 8;

        // ¬низ-вправо
        else if (angle < 67.5f)
            index = 9;

        // ¬низ
        else if (angle < 112.5f)
            index = 0;

        // ¬низ-влево (немного)
        else if (angle < 135f)
            index = 1;

        // ¬низ-влево
        else if (angle < 157.5f)
            index = 2;

        // ¬лево
        else if (angle < 202.5f)
            index = 3;

        // ¬верх-влево
        else if (angle < 247.5f)
            index = 4;

        // ¬верх
        else if (angle < 292.5f)
            index = 5;

        // ¬верх-вправо
        else
            index = 6;

        currentDirection = index;
        spriteRenderer.sprite = directions[index];
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    void Shoot()
    {
        Debug.Log("—“–≈Ћяё");

        if (bulletPrefab == null || target == null)
            return;

        if (firePoints == null || firePoints.Length <= currentDirection)
            return;

        Transform shootPoint = firePoints[currentDirection];

        Vector2 dir = (target.position - shootPoint.position).normalized;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Bullet.Spawn(
            bulletPrefab,
            shootPoint.position,
            Quaternion.Euler(0, 0, angle),
            transform
        );
    }
}