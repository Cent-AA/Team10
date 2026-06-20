using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    private static Sprite pickupSprite;

    [SerializeField] private int ammoAmount = 6;
    [SerializeField] private float lifetime = 20f;
    [SerializeField] private float magnetRadius = 1.7f;
    [SerializeField] private float magnetSpeed = 7f;

    private Transform magnetTarget;
    private float lifeTimer;

    public static AmmoPickup Spawn(Vector3 position, int amount)
    {
        GameObject pickupObject = new GameObject("AmmoPickup");
        pickupObject.transform.position = position;

        AmmoPickup pickup = pickupObject.AddComponent<AmmoPickup>();
        pickup.ammoAmount = Mathf.Max(1, amount);
        pickup.BuildRuntimePickup();
        return pickup;
    }

    void Awake()
    {
        lifeTimer = lifetime;
    }

    void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (magnetTarget != null && !CanReceiveAmmo(magnetTarget))
            magnetTarget = null;

        if (magnetTarget == null)
            magnetTarget = FindNearestAmmoReceiver();

        if (magnetTarget != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                magnetTarget.position,
                magnetSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        WeaponAmmo ammo = GetAmmoReceiver(other.transform);
        if (ammo == null)
            return;

        if (ammo.AddAmmo(ammoAmount) > 0)
            Destroy(gameObject);
    }

    void BuildRuntimePickup()
    {
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetPickupSprite();
        renderer.color = new Color(1f, 0.86f, 0.18f, 1f);
        renderer.sortingOrder = 80;

        CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.32f;
    }

    Transform FindNearestAmmoReceiver()
    {
        float closestDistSqr = magnetRadius * magnetRadius;
        Transform closest = null;

        Registry.CleanupPlayers();
        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform player = Registry.Players[i];
            if (player == null) continue;

            WeaponAmmo ammo = GetAmmoReceiver(player);
            if (ammo == null || ammo.CurrentAmmo >= ammo.MaxAmmo) continue;

            float distSqr = ((Vector2)player.position - (Vector2)transform.position).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = player;
            }
        }

        return closest;
    }

    WeaponAmmo GetAmmoReceiver(Transform target)
    {
        if (target == null)
            return null;

        WeaponAmmo ammo = target.GetComponentInParent<WeaponAmmo>();
        if (ammo != null)
            return ammo;

        PlayerController player = target.GetComponentInParent<PlayerController>();
        if (player != null)
            return player.gameObject.AddComponent<WeaponAmmo>();

        EngineerController engineer = target.GetComponentInParent<EngineerController>();
        if (engineer != null)
            return engineer.gameObject.AddComponent<WeaponAmmo>();

        return null;
    }

    bool CanReceiveAmmo(Transform target)
    {
        WeaponAmmo ammo = GetAmmoReceiver(target);
        return ammo != null && ammo.CurrentAmmo < ammo.MaxAmmo;
    }

    static Sprite GetPickupSprite()
    {
        if (pickupSprite != null)
            return pickupSprite;

        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color fill = Color.white;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, dist <= radius ? fill : clear);
            }
        }

        texture.Apply();
        pickupSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return pickupSprite;
    }
}
