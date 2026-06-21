using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 60f;
    private float currentHealth;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float runSpeed = 4f;
    public float campfireStopDistance = 0.8f;

    [Header("Targeting")]
    [System.NonSerialized]
    public Transform campfireTarget;
    public float detectRange = 8f;
    public float chaseRange = 12f;
    public float attackRange = 1.5f;
    public float loseTargetTime = 3f;
    public float targetRefreshInterval = 0.2f;
    public float attackerSwitchRange = 5f;
    public float groupedPlayersDistance = 2.5f;
    public float groupedLockDelay = 2f;
    private Coroutine knockbackRoutine;
    [Header("Attack")]
    public float attackCooldown = 1.5f;
    public float attackDamage = 15f;
    public float knockbackForce = 3f;

    [Header("Components")]
    public Animator animator;
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;

    [Header("Effects")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.1f;
    public float deathDespawnDelay = 3f;

    [Header("Ammo Drops")]
    [Range(0f, 1f)] public float ammoDropChance = 0.08f;
    public Vector2Int ammoDropAmountRange = new Vector2Int(3, 8);
    public float ammoDropScatter = 0.35f;

    public enum ZombieState { Idle, MoveToCampfire, Chase, Attack, Hit, Dead }
    private ZombieState currentState = ZombieState.MoveToCampfire;

    private Transform target;
    private PlayerController targetPlayer;
    private EngineerController targetEngineer;
    private Collider2D cachedCollider;
    private float attackTimer;
    private float loseTargetTimer;
    private float targetRefreshTimer;
    private float groupedPlayersTimer;
    private float currentTargetDistanceSqr;
    private bool committedToTarget;
    private bool poolManaged;
    private Color originalColor;
    private bool isDead;

    public bool IsAlive => !isDead;
    public bool HasActiveCollider => cachedCollider != null && cachedCollider.enabled;
    public float DeathDespawnDelay => deathDespawnDelay;
    public System.Action<ZombieAI> OnDied;

    void Awake()
    {
        CacheComponents();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
    }

    void OnEnable()
    {
        CacheComponents();
        EnsureCampfireTarget();
        Registry.RegisterZombie(this);
    }

    void OnDestroy()
    {
        Registry.UnregisterZombie(this);
    }

    void OnDisable()
    {
        Registry.UnregisterZombie(this);
    }

    void Start()
    {
        CacheComponents();
        currentHealth = maxHealth;
        loseTargetTimer = loseTargetTime;
        EnsureCampfireTarget();
    }

    void Update()
    {
        if (isDead) return;

        attackTimer -= Time.deltaTime;
        targetRefreshTimer -= Time.deltaTime;

        if (targetRefreshTimer <= 0f)
        {
            targetRefreshTimer = targetRefreshInterval;
            RefreshTarget();
        }

        UpdateGroupedTargetLock();

        switch (currentState)
        {
            case ZombieState.Idle:
            case ZombieState.MoveToCampfire:
                MoveToCampfire();
                break;
            case ZombieState.Chase:
                UpdateChase();
                break;
            case ZombieState.Attack:
            case ZombieState.Hit:
                break;
        }

        UpdateAnimator();
        UpdateFacing();
    }

    public void SetCampfireTarget(Transform newTarget)
    {
        campfireTarget = newTarget;
    }

    public void SetPoolManaged(bool managed)
    {
        poolManaged = managed;
    }

    public void ResetForSpawn(Transform newCampfireTarget)
    {
        StopAllCoroutines();
        CancelInvoke();

        CacheComponents();
        if (newCampfireTarget != null)
            campfireTarget = newCampfireTarget;
        EnsureCampfireTarget();

        currentHealth = maxHealth;
        isDead = false;
        currentState = ZombieState.MoveToCampfire;
        target = null;
        targetPlayer = null;
        targetEngineer = null;
        attackTimer = 0f;
        loseTargetTimer = loseTargetTime;
        targetRefreshTimer = 0f;
        groupedPlayersTimer = 0f;
        currentTargetDistanceSqr = 0f;
        committedToTarget = false;

        if (cachedCollider != null) cachedCollider.enabled = true;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    void RefreshTarget()
    {
        if (currentState == ZombieState.Attack || currentState == ZombieState.Hit) return;

        if (!IsValidPlayerTarget(target))
        {
            ClearTarget();
        }

        Transform closest = FindClosestVisiblePlayer();
        if (target == null)
        {
            if (closest != null) SetTarget(closest);
            return;
        }

        float targetDistSqr = ((Vector2)target.position - (Vector2)transform.position).sqrMagnitude;
        if (targetDistSqr <= chaseRange * chaseRange)
        {
            loseTargetTimer = loseTargetTime;
            return;
        }

        loseTargetTimer -= targetRefreshInterval;
        if (loseTargetTimer <= 0f)
        {
            ClearTarget();
            if (closest != null) SetTarget(closest);
        }
    }

    Transform FindClosestVisiblePlayer()
    {
        Registry.CleanupPlayers();

        float closestDistSqr = detectRange * detectRange;
        Transform closest = null;

        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform playerTransform = Registry.Players[i];
            if (!IsValidPlayerTarget(playerTransform)) continue;

            float distSqr = ((Vector2)playerTransform.position - (Vector2)transform.position).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = playerTransform;
            }
        }

        return closest;
    }

    bool IsValidPlayerTarget(Transform player)
    {
        if (player == null)
            return false;

        PlayerController playerController = Registry.GetPlayerController(player);
        if (IsValidPlayerController(playerController))
            return true;

        return IsValidEngineerController(GetEngineerController(player));
    }

    bool IsValidPlayerController(PlayerController controller)
    {
        return controller != null && controller.currentHealth > 0f;
    }

    bool IsValidEngineerController(EngineerController controller)
    {
        return controller != null && controller.currentHealth > 0f;
    }

    EngineerController GetEngineerController(Transform player)
    {
        if (player == null)
            return null;

        EngineerController engineer = player.GetComponent<EngineerController>();
        if (engineer == null)
            engineer = player.GetComponentInChildren<EngineerController>();
        if (engineer == null)
            engineer = player.GetComponentInParent<EngineerController>();

        return engineer;
    }

    void SetTarget(Transform newTarget)
    {
        if (newTarget == null) return;

        target = newTarget;
        targetPlayer = Registry.GetPlayerController(newTarget);
        targetEngineer = GetEngineerController(newTarget);
        loseTargetTimer = loseTargetTime;
        currentState = ZombieState.Chase;
    }

    void ClearTarget()
    {
        target = null;
        targetPlayer = null;
        targetEngineer = null;
        committedToTarget = false;
        groupedPlayersTimer = 0f;
        loseTargetTimer = loseTargetTime;
        currentState = ZombieState.MoveToCampfire;
    }

    void UpdateGroupedTargetLock()
    {
        if (target == null || committedToTarget)
        {
            return;
        }

        int playersNearTarget = 0;
        float groupDistSqr = groupedPlayersDistance * groupedPlayersDistance;

        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform player = Registry.Players[i];
            if (!IsValidPlayerTarget(player)) continue;

            float distSqr = ((Vector2)player.position - (Vector2)target.position).sqrMagnitude;
            if (distSqr <= groupDistSqr)
            {
                playersNearTarget++;
            }
        }

        if (playersNearTarget >= 2)
        {
            groupedPlayersTimer += Time.deltaTime;
            if (groupedPlayersTimer >= groupedLockDelay)
            {
                committedToTarget = true;
            }
        }
        else
        {
            groupedPlayersTimer = 0f;
        }
    }

    void MoveToCampfire()
    {
        EnsureCampfireTarget();
        if (campfireTarget == null)
        {
            currentState = ZombieState.Idle;
            return;
        }

        Vector2 currentPosition = GetMovementPosition();
        Vector2 destination = campfireTarget.position;
        Vector2 delta = destination - currentPosition;

        if (delta.sqrMagnitude <= campfireStopDistance * campfireStopDistance)
        {
            currentState = ZombieState.Idle;
            return;
        }

        currentState = ZombieState.MoveToCampfire;
        MoveCharacter(currentPosition + delta.normalized * moveSpeed * Time.deltaTime);
    }

    void UpdateChase()
    {
        if (target == null)
        {
            ClearTarget();
            return;
        }

        Vector2 currentPosition = GetMovementPosition();
        Vector2 delta = (Vector2)target.position - currentPosition;
        float distSqr = delta.sqrMagnitude;
        currentTargetDistanceSqr = distSqr;

        if (distSqr > chaseRange * chaseRange)
        {
            loseTargetTimer -= Time.deltaTime;
            if (loseTargetTimer <= 0f)
            {
                ClearTarget();
                return;
            }
        }
        else
        {
            loseTargetTimer = loseTargetTime;
        }

        if (distSqr <= attackRange * attackRange && attackTimer <= 0f)
        {
            currentState = ZombieState.Attack;
            PerformAttack();
            return;
        }

        if (distSqr <= 0.0001f)
            return;

        float runThresholdSqr = detectRange * detectRange * 0.25f;
        float speed = distSqr > runThresholdSqr ? runSpeed : moveSpeed;
        MoveCharacter(currentPosition + delta.normalized * speed * Time.deltaTime);
    }

    void PerformAttack()
    {
        attackTimer = attackCooldown;

        if (animator != null)
        {
            animator.SetTrigger("attack");
        }

        Invoke(nameof(DealDamage), 0.3f);
        Invoke(nameof(EndAttack), 0.6f);
    }

    void DealDamage()
    {
        if (target == null) return;

        Vector2 delta = (Vector2)target.position - (Vector2)transform.position;
        float attackReach = attackRange * 1.5f;
        if (delta.sqrMagnitude <= attackReach * attackReach)
        {
            PlayerController player = targetPlayer != null ? targetPlayer : Registry.GetPlayerController(target);
            if (player != null)
            {
                Vector2 knockDir = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
                player.TakeDamage(attackDamage, knockDir);
                ArenaCamera.Shake(0.3f, 0.1f);
                return;
            }

            EngineerController engineer = targetEngineer != null ? targetEngineer : GetEngineerController(target);
            if (engineer != null)
            {
                Vector2 knockDir = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
                engineer.TakeDamage(attackDamage, knockDir, transform);
                ArenaCamera.Shake(0.3f, 0.1f);
            }
        }
    }

    void EndAttack()
    {
        if (isDead) return;
        currentState = target != null ? ZombieState.Chase : ZombieState.MoveToCampfire;
    }

    public void TakeDamage(float damage, Vector2 knockbackDir)
    {
        TakeDamage(damage, knockbackDir, null);
    }

    public void TakeDamage(float damage, Vector2 knockbackDir, Transform attacker)
    {
        if (isDead) return;

        TrySwitchToAttacker(attacker);
        currentHealth -= damage;

        StartCoroutine(HitFlash());
        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }
        StartCoroutine(Knockback(knockbackDir));
        ArenaCamera.Shake(damage * 0.03f, 0.1f);

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            currentState = ZombieState.Hit;
            if (animator != null) animator.SetTrigger("hit");
            Invoke(nameof(RecoverFromHit), 0.3f);
        }
    }

    void TrySwitchToAttacker(Transform attacker)
    {
        if (attacker == null || committedToTarget) return;

        if (!IsValidPlayerTarget(attacker)) return;

        float distSqr = ((Vector2)attacker.position - (Vector2)transform.position).sqrMagnitude;
        if (distSqr > attackerSwitchRange * attackerSwitchRange) return;

        SetTarget(attacker);
        groupedPlayersTimer = 0f;
    }

    void RecoverFromHit()
    {
        if (isDead) return;
        currentState = target != null ? ZombieState.Chase : ZombieState.MoveToCampfire;
    }

    System.Collections.IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
    }

    System.Collections.IEnumerator Knockback(Vector2 dir)
    {
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.fixedDeltaTime;
            Vector2 currentPosition = GetMovementPosition();
            MoveCharacter(currentPosition + dir * knockbackForce * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
    }

    void Die()
    {
        isDead = true;
        currentState = ZombieState.Dead;
        CancelInvoke();
        Registry.UnregisterZombie(this);

        if (animator != null) animator.SetTrigger("die");

        if (cachedCollider != null) cachedCollider.enabled = false;

        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

        TryDropAmmo();
        OnDied?.Invoke(this);

        if (!poolManaged)
        {
            Destroy(gameObject, deathDespawnDelay);
        }
    }

    void TryDropAmmo()
    {
        if (ammoDropChance <= 0f || Random.value > ammoDropChance)
            return;

        int min = Mathf.Max(1, ammoDropAmountRange.x);
        int max = Mathf.Max(min, ammoDropAmountRange.y);
        int amount = Random.Range(min, max + 1);
        Vector2 scatter = Random.insideUnitCircle * Mathf.Max(0f, ammoDropScatter);
        AmmoPickup.Spawn(transform.position + (Vector3)scatter, amount);
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        bool isMoving = currentState == ZombieState.MoveToCampfire || currentState == ZombieState.Chase;
        bool isRunning = currentState == ZombieState.Chase &&
                         target != null &&
                         currentTargetDistanceSqr > detectRange * detectRange * 0.25f;

        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isRunning", isRunning);
    }

    void UpdateFacing()
    {
        if (spriteRenderer == null) return;

        if (target != null && currentState == ZombieState.Chase)
        {
            spriteRenderer.flipX = target.position.x < transform.position.x;
            return;
        }

        EnsureCampfireTarget();
        if (campfireTarget == null) return;

        Vector2 destination = campfireTarget.position;
        Vector2 dir = destination - (Vector2)transform.position;
        if (Mathf.Abs(dir.x) > 0.1f)
        {
            spriteRenderer.flipX = dir.x < 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackerSwitchRange);
        Gizmos.color = Color.green;
        Vector3 center = campfireTarget != null ? campfireTarget.position : Vector3.zero;
        Gizmos.DrawWireSphere(center, campfireStopDistance);
    }

    void CacheComponents()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (cachedCollider == null) cachedCollider = GetComponent<Collider2D>();
    }

    void EnsureCampfireTarget()
    {
        if (campfireTarget != null)
            return;

        GameObject campfireObject = GameObject.Find("CampFire");
        if (campfireObject == null)
            campfireObject = GameObject.Find("Campfire");

        if (campfireObject != null)
            campfireTarget = campfireObject.transform;
    }

    Vector2 GetMovementPosition()
    {
        return rb != null ? rb.position : (Vector2)transform.position;
    }

    void MoveCharacter(Vector2 nextPosition)
    {
        if (rb != null)
        {
            rb.MovePosition(nextPosition);
            return;
        }

        transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
    }
}
