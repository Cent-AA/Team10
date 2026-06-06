using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    [Header("═══ Здоровье ═══")]
    public float maxHealth = 60f;
    private float currentHealth;

    [Header("═══ Движение ═══")]
    public float moveSpeed = 2f;
    public float runSpeed = 4f;
    public float patrolSpeed = 1.5f;

    [Header("═══ Обнаружение ═══")]
    public float detectRange = 8f;       // Видит игрока
    public float chaseRange = 12f;       // Преследует до этого расстояния
    public float attackRange = 1.5f;     // Бьёт
    public float loseTargetTime = 3f;    // Теряет цель через N сек

    [Header("═══ Атака ═══")]
    public float attackCooldown = 1.5f;
    public float attackDamage = 15f;
    public float knockbackForce = 3f;

    [Header("═══ Патруль ═══")]
    public float patrolRadius = 5f;      // Радиус бродяжничества
    public float patrolWaitMin = 1f;
    public float patrolWaitMax = 3f;
    public float waypointThreshold = 0.3f;

    [Header("═══ Компоненты ═══")]
    public Animator animator;
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;

    [Header("═══ Эффекты ═══")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.1f;

    public enum ZombieState { Idle, Patrol, Chase, Attack, Hit, Dead }
    private ZombieState currentState = ZombieState.Idle;

    private Transform target;
    private Vector3 spawnPoint;
    private Vector2 patrolTarget;
    private float attackTimer;
    private float loseTargetTimer;
    private float patrolWaitTimer;
    private bool isWaiting = false;
    private Color originalColor;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        spawnPoint = transform.position;
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        SetNewPatrolPoint();
        patrolWaitTimer = Random.Range(patrolWaitMin, patrolWaitMax);
    }

    void Update()
    {
        if (isDead) return;

        attackTimer -= Time.deltaTime;
        FindClosestPlayer();

        switch (currentState)
        {
            case ZombieState.Idle:
                UpdateIdle();
                break;
            case ZombieState.Patrol:
                UpdatePatrol();
                break;
            case ZombieState.Chase:
                UpdateChase();
                break;
            case ZombieState.Attack:
                // Ждём конца анимации атаки
                break;
            case ZombieState.Hit:
                // Ждём конца анимации
                break;
        }

        UpdateAnimator();
        UpdateFacing();
    }

    // ═══════════ ПОИСК ИГРОКА ═══════════
    void FindClosestPlayer()
    {
        if (currentState == ZombieState.Attack || currentState == ZombieState.Hit) return;

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (var p in players)
        {
            if (p.currentHealth <= 0) continue;
            float dist = Vector2.Distance(transform.position, p.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = p.transform;
            }
        }

        if (closest != null && closestDist <= detectRange)
        {
            target = closest;
            loseTargetTimer = loseTargetTime;
            if (currentState != ZombieState.Chase && currentState != ZombieState.Attack)
                currentState = ZombieState.Chase;
        }
        else if (target != null)
        {
            loseTargetTimer -= Time.deltaTime;
            if (loseTargetTimer <= 0)
            {
                target = null;
                currentState = ZombieState.Patrol;
            }
        }
    }

    // ═══════════ IDLE ═══════════
    void UpdateIdle()
    {
        patrolWaitTimer -= Time.deltaTime;
        if (patrolWaitTimer <= 0)
        {
            SetNewPatrolPoint();
            currentState = ZombieState.Patrol;
        }
    }

    // ═══════════ ПАТРУЛЬ ═══════════
    void UpdatePatrol()
    {
        Vector2 dir = (patrolTarget - (Vector2)transform.position);
        float dist = dir.magnitude;

        if (dist < waypointThreshold)
        {
            currentState = ZombieState.Idle;
            patrolWaitTimer = Random.Range(patrolWaitMin, patrolWaitMax);
            return;
        }

        Vector2 newPos = rb.position + dir.normalized * patrolSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
    }

    void SetNewPatrolPoint()
    {
        Vector2 randomDir = Random.insideUnitCircle * patrolRadius;
        patrolTarget = (Vector2)spawnPoint + randomDir;
    }

    // ═══════════ ПОГОНЯ ═══════════
    void UpdateChase()
    {
        if (target == null)
        {
            currentState = ZombieState.Patrol;
            return;
        }

        float dist = Vector2.Distance(transform.position, target.position);

        // Слишком далеко — потерял
        if (dist > chaseRange)
        {
            loseTargetTimer -= Time.deltaTime;
            if (loseTargetTimer <= 0)
            {
                target = null;
                currentState = ZombieState.Patrol;
                return;
            }
        }

        // В зоне атаки
        if (dist <= attackRange && attackTimer <= 0)
        {
            currentState = ZombieState.Attack;
            PerformAttack();
            return;
        }

        // Бежим к цели
        Vector2 dir = ((Vector2)target.position - rb.position).normalized;
        float speed = dist > detectRange * 0.5f ? runSpeed : moveSpeed;
        rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);
    }

    // ═══════════ АТАКА ═══════════
    void PerformAttack()
    {
        attackTimer = attackCooldown;

        if (animator != null)
            animator.SetTrigger("attack");

        // Наносим урон с задержкой (в середине анимации)
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
        currentState = target != null ? ZombieState.Chase : ZombieState.Patrol;
    }

    // ═══════════ ПОЛУЧЕНИЕ УРОНА ═══════════
    public void TakeDamage(float damage, Vector2 knockbackDir)
    {
        if (isDead) return;

        currentHealth -= damage;

        // Вспышка
        StartCoroutine(HitFlash());

        // Нокбэк
        StartCoroutine(Knockback(knockbackDir));

        // Камера
        ArenaCamera.Shake(damage * 0.03f, 0.1f);

        if (currentHealth <= 0)
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

    void RecoverFromHit()
    {
        if (isDead) return;
        currentState = target != null ? ZombieState.Chase : ZombieState.Patrol;
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
            rb.MovePosition(rb.position + dir * knockbackForce * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
    }

    // ═══════════ СМЕРТЬ ═══════════
    void Die()
    {
        isDead = true;
        currentState = ZombieState.Dead;

        if (animator != null) animator.SetTrigger("die");

        // Отключаем коллайдер
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        rb.bodyType = RigidbodyType2D.Kinematic;

        // Исчезаем через 3 секунды
        Destroy(gameObject, 3f);
    }

    // ═══════════ АНИМАТОР ═══════════
    void UpdateAnimator()
    {
        if (animator == null) return;

        bool isMoving = currentState == ZombieState.Patrol ||
                        currentState == ZombieState.Chase;
        bool isRunning = currentState == ZombieState.Chase &&
                         target != null &&
                         Vector2.Distance(transform.position, target.position) > detectRange * 0.5f;

        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isRunning", isRunning);
    }

    // ═══════════ НАПРАВЛЕНИЕ ═══════════
    void UpdateFacing()
    {
        if (spriteRenderer == null) return;

        if (currentState == ZombieState.Chase && target != null)
        {
            spriteRenderer.flipX = target.position.x < transform.position.x;
        }
        else if (currentState == ZombieState.Patrol)
        {
            Vector2 dir = patrolTarget - (Vector2)transform.position;
            if (Mathf.Abs(dir.x) > 0.1f)
                spriteRenderer.flipX = dir.x < 0;
        }
    }

    // ═══════════ ГИЗМО ═══════════
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}
