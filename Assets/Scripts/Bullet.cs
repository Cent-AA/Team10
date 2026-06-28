using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float lifeTime = 3f;

    private static readonly Dictionary<GameObject, Queue<Bullet>> Pools = new Dictionary<GameObject, Queue<Bullet>>();

    private Rigidbody2D rb;
    private Transform owner;
    private GameObject prefabSource;
    private Coroutine lifeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPools()
    {
        Pools.Clear();
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform ownerTransform)
    {
        if (prefab == null) return null;

        Bullet bullet = GetFromPool(prefab);
        if (bullet == null)
        {
            GameObject fallback = Instantiate(prefab, position, rotation);
            bullet = fallback.GetComponent<Bullet>();
            if (bullet == null) return fallback;

            bullet.prefabSource = prefab;
        }

        bullet.Activate(position, rotation, ownerTransform, prefab);
        return bullet.gameObject;
    }

    public void Init(Transform ownerTransform)
    {
        owner = ownerTransform;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    static Bullet GetFromPool(GameObject prefab)
    {
        if (!Pools.TryGetValue(prefab, out Queue<Bullet> pool))
        {
            return null;
        }

        while (pool.Count > 0)
        {
            Bullet bullet = pool.Dequeue();
            if (bullet != null) return bullet;
        }

        return null;
    }

    void Activate(Vector3 position, Quaternion rotation, Transform ownerTransform, GameObject prefab)
    {
        prefabSource = prefab;
        owner = ownerTransform;

        transform.SetPositionAndRotation(position, rotation);
        gameObject.SetActive(true);

        if (rb != null)
        {
            rb.linearVelocity = transform.right * speed;
            rb.angularVelocity = 0f;
        }

        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
        }

        lifeRoutine = StartCoroutine(ReturnAfterLifetime());
    }

    IEnumerator ReturnAfterLifetime()
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null || IsOwnerCollision(collision))
            return;

        ZombieAI zombie = collision.GetComponentInParent<ZombieAI>();
        if (zombie != null && zombie.IsAlive)
        {
            Vector2 knockbackDir = transform.right;
            zombie.TakeDamage(damage, knockbackDir, owner);
            ReturnToPool();
            return;
        }

        BossController boss = collision.GetComponentInParent<BossController>();
        if (boss != null && boss.IsAlive)
        {
            boss.TakeDamage(damage);
            ReturnToPool();
            return;
        }

        PrototypeReviveTarget reviveTarget = collision.GetComponentInParent<PrototypeReviveTarget>();
        if (reviveTarget != null && reviveTarget.IsDowned)
        {
            reviveTarget.ReceiveReviveDamage(damage, owner);
            ReturnToPool();
        }
    }

    bool IsOwnerCollision(Collider2D collision)
    {
        if (owner == null)
            return false;

        Transform hit = collision.transform;
        return hit == owner || hit.IsChildOf(owner);
    }

    void ReturnToPool()
    {
        if (!gameObject.activeSelf) return;

        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
            lifeRoutine = null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        owner = null;
        gameObject.SetActive(false);

        if (prefabSource == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!Pools.TryGetValue(prefabSource, out Queue<Bullet> pool))
        {
            pool = new Queue<Bullet>();
            Pools.Add(prefabSource, pool);
        }

        pool.Enqueue(this);
    }
}
