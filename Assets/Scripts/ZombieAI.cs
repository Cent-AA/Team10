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
    public Transform campfireTarget;
    public float detectRange = 8f;
    public float chaseRange = 12f;
    public float attackRange = 1.5f;
    public float loseTargetTime = 3f;
    public float targetRefreshInterval = 0.2f;
    public float attackerSwitchRange = 5f;
    public float groupedPlayersDistance = 2.5f;
    public float groupedLockDelay = 2f;

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

    public enum ZombieState { Idle, MoveToCampfire, Chase, Attack, Hit, Dead }
    private ZombieState currentState = ZombieState.MoveToCampfire;

    private Transform target;
    private float attackTimer;
    private float loseTargetTimer;
    private float targetRefreshTimer;
    private float groupedPlayersTimer;
    private bool committedToTarget;
    private bool poolManaged;
    private Color originalColor;
    private bool isDead;

    public bool IsAlive => !isDead;
    public float DeathDespawnDelay => deathDespawnDelay;
    public System.Action<ZombieAI> OnDied;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
    }

    void OnEnable()
    {
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
        currentHealth = maxHealth;
        loseTargetTimer = loseTargetTime;

        if (campfireTarget == null)
        {
            CampfireController campfire = FindAnyObjectByType<CampfireController>();
            if (campfire != null) campfireTarget = campfire.transform;
        }
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

        campfireTarget = newCampfireTarget;
        currentHealth = maxHealth;
        isDead = false;
        currentState = ZombieState.MoveToCampfire;
        target = null;
        attackTimer = 0f;
        loseTargetTimer = loseTargetTime;
        targetRefreshTimer = 0f;
        groupedPlayersTimer = 0f;
        committedToTarget = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

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
            Transform player = Registry.Players[i];
            if (!IsValidPlayerTarget(player)) continue;

            float distSqr = ((Vector2)player.position - (Vector2)transform.position).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = player;
            }
        }

        return closest;
    }

    bool IsValidPlayerTarget(Transform player)
    {
        if (player == null) return false;

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller == null) controller = player.GetComponentInChildren<PlayerController>();

        return controller != null && controller.currentHealth > 0f;
    }

    void SetTarget(Transform newTarget)
    {
        if (newTarget == null) return;

        target = newTarget;
        loseTargetTimer = loseTargetTime;
        currentState = ZombieState.Chase;
    }

    void ClearTarget()
    {
        target = null;
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
        Vector2 destination = campfireTarget != null ? (Vector2)campfireTarget.position : Vector2.zero;
        Vector2 delta = destination - rb.position;

        if (delta.sqrMagnitude <= campfireStopDistance * campfireStopDistance)
        {
            currentState = ZombieState.Idle;
            return;
        }

        currentState = ZombieState.MoveToCampfire;
        rb.MovePosition(rb.position + delta.normalized * moveSpeed * Time.deltaTime);
    }

    void UpdateChase()
    {
        if (target == null)
        {
            ClearTarget();
            return;
        }

        float dist = Vector2.Distance(transform.position, target.position);

        if (dist > chaseRange)
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

        if (dist <= attackRange && attackTimer <= 0f)
        {
            currentState = ZombieState.Attack;
            PerformAttack();
            return;
        }

        Vector2 dir = ((Vector2)target.position - rb.position).normalized;
        float speed = dist > detectRange * 0.5f ? runSpeed : moveSpeed;
        rb.MovePosition(rb.position + dir * speed * Time.deltaTime);
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

        float dist = Vector2.Distance(transform.position, target.position);
        if (dist <= attackRange * 1.5f)
        {
            PlayerController player = target.GetComponent<PlayerController>();
            if (player == null) player = target.GetComponentInChildren<PlayerController>();

            if (player != null)
            {
                Vector2 knockDir = (target.position - transform.position).normalized;
                player.TakeDamage(attackDamage, knockDir);
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
        if (attacker == null || committedToTarget || !IsValidPlayerTarget(attacker)) return;

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
        if (rb == null) yield break;

        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.fixedDeltaTime;
            rb.MovePosition(rb.position + dir * knockbackForce * Time.fixedDeltaTime);
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

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

        OnDied?.Invoke(this);

        if (!poolManaged)
        {
            Destroy(gameObject, deathDespawnDelay);
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        bool isMoving = currentState == ZombieState.MoveToCampfire || currentState == ZombieState.Chase;
        bool isRunning = currentState == ZombieState.Chase &&
                         target != null &&
                         Vector2.Distance(transform.position, target.position) > detectRange * 0.5f;

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

        Vector2 destination = campfireTarget != null ? (Vector2)campfireTarget.position : Vector2.zero;
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
}
