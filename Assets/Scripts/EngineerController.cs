using UnityEngine;
using System.Collections;

public class EngineerController : MonoBehaviour
{
    [Header("═══ Игрок ═══")]
    public int playerNumber = 1;

    [Header("═══ Движение ═══")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;

    [Header("═══ Прыжки при ходьбе ═══")]
    public float hopHeight = 0.15f;
    public float hopSpeed = 10f;
    public float hopSquashX = 1.1f;
    public float hopSquashY = 0.85f;
    public float squashSpeed = 12f;

    [Header("═══ Здоровье ═══")]
    public float maxHealth = 80f;
    public float currentHealth;

    [Header("═══ Ключ ═══")]
    public Transform wrenchPivot;           // На позиции инженера, крутится
    public SpriteRenderer wrenchSprite;     // Спрайт ключа (дочерний от wrenchPivot)
    public float wrenchOffset = 0.5f;       // Расстояние ключа от центра

    [Header("═══ Обычная атака ═══")]
    public float swingAnticipation = 0.15f; // Замах назад
    public float swingDuration = 0.2f;      // Удар
    public float swingRecovery = 0.15f;     // Возврат
    public float swingAngle = 220f;         // Градусов
    public float attackDamage = 12f;
    public float attackRange = 1.5f;
    public float attackCooldown = 0.4f;
    public float knockbackForce = 3f;

    [Header("═══ Заряженная атака (3 сек) ═══")]
    public float chargeTime = 3f;
    public float chargedDamage = 40f;
    public float chargedKnockback = 6f;
    public float chargedSwingAngle = 300f;
    public float chargedWrenchScale = 2f;   // Увеличение ключа
    public float parryFreezeDuration = 0.3f;

    [Header("═══ Таргет ═══")]
    public TargetingSystem targeting;

    [Header("═══ Компоненты ═══")]
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rb;
    public Transform attackPoint;
    public LayerMask enemyLayer;
    public PlayerSharedInput sharedInput;
    public PlayerSharedHealth sharedHealth;
    public PlayerMotor2D motor;

    [Header("═══ Звуки ═══")]
    public AudioClip wrenchHitSound;
    public AudioClip onHitVoiceLine;
    public AudioClip buildTurretVoiceLine;
    public AudioClip buildDispenserVoiceLine;
    public AudioClip sentryKillVoiceLine;
    private AudioSource audioSource;
    // Внутреннее
    private Vector2 moveInput;
    private bool isRunning;
    private bool isAttacking = false;
    private float attackTimer = 0f;
    private Vector3 originalScale;
    private Vector3 originalPos;
    private Vector3 currentSquash = Vector3.one;
    private float hopPhase = 0f;
    private bool wasMoving = false;
    private float facingDir = 1f;
    private Vector2 targetDir = Vector2.right;

    // Зарядка
    private bool isHoldingAttack = false;
    private float holdTime = 0f;
    private Vector3 wrenchOriginalScale;
    private Coroutine deathRoutine;
    private bool sharedHealthSubscribed;
    private readonly Collider2D[] attackHitBuffer = new Collider2D[32];
    private readonly Collider2D[] reviveHitBuffer = new Collider2D[16];

    public System.Action<float, float> OnHealthChanged;

    void Awake()
    {
        EnsureSharedComponents();
        SyncSharedSettings();
    }

    void OnEnable()
    {
        EnsureSharedComponents();
        Registry.Register(transform);
        SubscribeSharedHealth();
    }

    void OnDisable()
    {
        Registry.Unregister(transform);
        UnsubscribeSharedHealth();
    }

    void OnDestroy()
    {
        Registry.Unregister(transform);
        UnsubscribeSharedHealth();
    }

    void Start()
    {
        EnsureSharedComponents();
        SyncSharedSettings();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (spriteRenderer != null) originalScale = spriteRenderer.transform.localScale;
        originalPos = spriteRenderer != null ? spriteRenderer.transform.localPosition : Vector3.zero;

        // Ключ скрыт по умолчанию
        if (wrenchSprite != null)
        {
            wrenchOriginalScale = wrenchSprite.transform.localScale;
            wrenchSprite.enabled = false;
        }
    }

    void Update()
    {
        if (currentHealth <= 0) return;

        attackTimer -= Time.deltaTime;
        moveInput = sharedInput != null ? sharedInput.Movement : Vector2.zero;
        isRunning = sharedInput != null && sharedInput.RunHeld;

        if (motor != null)
        {
            motor.walkSpeed = moveSpeed;
            motor.runSpeed = runSpeed;
            motor.SetMovement(moveInput, isRunning);
            motor.SetMovementLocked(isAttacking || currentHealth <= 0f);
        }

        // Направление
        if (targeting != null && targeting.currentTarget != null)
        {
            targetDir = (targeting.currentTarget.position - transform.position).normalized;
            facingDir = Mathf.Sign(targetDir.x);
        }
        else if (moveInput.magnitude > 0.1f)
        {
            targetDir = moveInput.normalized;
            facingDir = Mathf.Sign(moveInput.x != 0 ? moveInput.x : facingDir);
        }

        if (spriteRenderer != null && targetDir.x != 0)
            spriteRenderer.flipX = targetDir.x < 0;

        // Атака — зажатие
        if (GetAttackHeld() && !isAttacking)
        {
            if (!isHoldingAttack)
            {
                isHoldingAttack = true;
                holdTime = 0f;
                // Показываем ключ сразу при зажатии
                ShowWrench();
            }
            holdTime += Time.deltaTime;

            // Ключ увеличивается при зарядке
            AnimateCharge();
        }
        else if (isHoldingAttack)
        {
            isHoldingAttack = false;
            if (attackTimer <= 0)
            {
                if (holdTime >= chargeTime)
                    StartCoroutine(ChargedSwing());
                else
                    StartCoroutine(NormalSwing());
            }
            else
            {
                HideWrench();
            }
        }

        AnimateHop();
    }

    void FixedUpdate()
    {
        if (motor != null)
            motor.SetMovementLocked(isAttacking || currentHealth <= 0f);
    }

    // ═══════════ КЛЮЧ — ПОКАЗАТЬ/СКРЫТЬ ═══════════
    void ShowWrench()
    {
        if (wrenchSprite != null)
        {
            wrenchSprite.enabled = true;
            wrenchSprite.transform.localScale = wrenchOriginalScale;
        }
        if (wrenchPivot != null)
            wrenchPivot.localRotation = Quaternion.identity;
    }

    void HideWrench()
    {
        if (wrenchSprite != null) wrenchSprite.enabled = false;
        if (wrenchPivot != null)
        {
            wrenchPivot.localRotation = Quaternion.identity;
            wrenchPivot.localScale = Vector3.one;
        }
    }

    // ═══════════ ЗАРЯДКА — ключ виден и растёт ═══════════
    void AnimateCharge()
    {
        if (wrenchPivot == null || wrenchSprite == null) return;

        float t = Mathf.Clamp01(holdTime / chargeTime);

        // Ключ направлен к цели
        float baseAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

        // Лёгкая тряска нарастает
        float shake = Mathf.Sin(Time.time * 30f * t) * t * 8f;
        wrenchPivot.localRotation = Quaternion.Euler(0, 0, baseAngle + shake);

        // Ключ увеличивается
        float scale = Mathf.Lerp(1f, chargedWrenchScale, t);
        wrenchSprite.transform.localScale = wrenchOriginalScale * scale;

        // Цвет при полной зарядке
        if (t >= 1f)
            wrenchSprite.color = new Color(1f, 1f, 0.5f, 1f); // Жёлтый
        else
            wrenchSprite.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.7f), t);
    }

    // ═══════════ ОБЫЧНЫЙ ЗАМАХ ═══════════
    IEnumerator NormalSwing()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        ShowWrench();

        // Начальный угол — в сторону цели
        float baseAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle + 40f; // Чуть назад
        float endAngle = baseAngle - swingAngle;
        bool hasHit = false;

        // Фаза 1: Замах назад (anticipation)
        float elapsed = 0f;
        while (elapsed < swingAnticipation)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(elapsed / swingAnticipation);
            wrenchPivot.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(baseAngle, startAngle, t));
            yield return null;
        }

        // Фаза 2: Удар (быстрый, EaseInOut)
        elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseInOutSine(elapsed / swingDuration);
            wrenchPivot.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startAngle, endAngle, t));

            if (t > 0.3f && !hasHit)
            {
                hasHit = true;
                DealDamage(attackDamage, knockbackForce);
            }
            yield return null;
        }

        // Фаза 3: Возврат
        elapsed = 0f;
        while (elapsed < swingRecovery)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(elapsed / swingRecovery);
            wrenchPivot.localRotation = Quaternion.Lerp(
                Quaternion.Euler(0, 0, endAngle), Quaternion.identity, t);
            yield return null;
        }

        HideWrench();
        isAttacking = false;
    }

    // ═══════════ ЗАРЯЖЕННЫЙ ЗАМАХ — мощный + парирование ═══════════
    IEnumerator ChargedSwing()
    {
        isAttacking = true;
        attackTimer = attackCooldown * 2f;

        float baseAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle + 60f;
        float endAngle = baseAngle - chargedSwingAngle;
        bool hasHit = false;

        // Замах назад
        float elapsed = 0f;
        while (elapsed < swingAnticipation * 1.5f)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(elapsed / (swingAnticipation * 1.5f));
            wrenchPivot.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(baseAngle, startAngle, t));
            yield return null;
        }

        // МОЩНЫЙ удар
        elapsed = 0f;
        while (elapsed < swingDuration * 0.7f)
        {
            elapsed += Time.deltaTime;
            float t = EaseInOutSine(elapsed / (swingDuration * 0.7f));
            wrenchPivot.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startAngle, endAngle, t));

            if (t > 0.4f && !hasHit)
            {
                hasHit = true;
                bool hit = DealDamage(chargedDamage, chargedKnockback);
                if (hit) yield return StartCoroutine(ParryEffect());
            }
            yield return null;
        }

        // Возврат
        elapsed = 0f;
        while (elapsed < swingRecovery * 1.5f)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(elapsed / (swingRecovery * 1.5f));
            wrenchPivot.localRotation = Quaternion.Lerp(
                Quaternion.Euler(0, 0, endAngle), Quaternion.identity, t);
            wrenchSprite.transform.localScale = Vector3.Lerp(
                wrenchOriginalScale * chargedWrenchScale, wrenchOriginalScale, t);
            wrenchSprite.color = Color.Lerp(new Color(1f, 1f, 0.5f), Color.white, t);
            yield return null;
        }

        HideWrench();
        isAttacking = false;
    }

    // ═══════════ ПАРИРОВАНИЕ (Ultrakill) ═══════════
    IEnumerator ParryEffect()
    {
        SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>();
        Color[] origColors = new Color[allSprites.Length];
        for (int i = 0; i < allSprites.Length; i++)
        {
            origColors[i] = allSprites[i].color;
            allSprites[i].color = Color.white;
        }

        ArenaCamera.Shake(1f, 0.3f);
        Time.timeScale = 0.02f;
        yield return new WaitForSecondsRealtime(parryFreezeDuration);
        Time.timeScale = 1f;

        for (int i = 0; i < allSprites.Length; i++)
            if (allSprites[i] != null) allSprites[i].color = origColors[i];
    }

    // ═══════════ УРОН ═══════════
    bool DealDamage(float damage, float knockback)
    {
        if (attackPoint == null) return false;
        bool hitSomething = false;

        int hitCount = Physics2D.OverlapCircleNonAlloc(attackPoint.position, attackRange, attackHitBuffer, enemyLayer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = attackHitBuffer[i];
            if (hit == null) continue;
            if (hit.transform == transform) continue;
            Vector2 knockDir = (hit.transform.position - transform.position).normalized;

            ZombieAI zombie = hit.GetComponent<ZombieAI>();
            if (zombie != null)
            {
                zombie.TakeDamage(damage, knockDir * knockback, transform);
                PlaySound(wrenchHitSound);
                hitSomething = true;
            }

            PlayerController player = hit.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage, knockDir);
                PlaySound(wrenchHitSound);
                hitSomething = true;
            }
        }

        if (TryDealReviveDamage(damage))
        {
            PlaySound(wrenchHitSound);
            hitSomething = true;
        }

        return hitSomething;
    }

    bool TryDealReviveDamage(float damage)
    {
        if (attackPoint == null) return false;

        int hitCount = Physics2D.OverlapCircleNonAlloc(attackPoint.position, attackRange, reviveHitBuffer);
        bool revivedProgress = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = reviveHitBuffer[i];
            if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

            PrototypeReviveTarget reviveTarget = hit.GetComponent<PrototypeReviveTarget>();
            if (reviveTarget == null)
                reviveTarget = hit.GetComponentInParent<PrototypeReviveTarget>();

            if (reviveTarget != null && reviveTarget.IsDowned)
                revivedProgress |= reviveTarget.ReceiveReviveDamage(damage, transform);
        }

        return revivedProgress;
    }

    // ═══════════ ПРЫЖКИ ═══════════
    void AnimateHop()
    {
        if (spriteRenderer == null) return;
        bool isMoving = moveInput.magnitude > 0.1f;

        if (isMoving)
        {
            float speed = isRunning ? hopSpeed * 1.5f : hopSpeed;
            float height = isRunning ? hopHeight * 1.5f : hopHeight;
            hopPhase += Time.deltaTime * speed;

            float hop = Mathf.Abs(Mathf.Sin(hopPhase)) * height;
            spriteRenderer.transform.localPosition = originalPos + Vector3.up * hop;

            float sinVal = Mathf.Sin(hopPhase);
            if (Mathf.Abs(sinVal) < 0.1f)
                currentSquash = new Vector3(hopSquashX, hopSquashY, 1f);
        }
        else
        {
            hopPhase = 0f;
            spriteRenderer.transform.localPosition = Vector3.Lerp(
                spriteRenderer.transform.localPosition, originalPos, Time.deltaTime * 10f);
        }

        currentSquash = Vector3.Lerp(currentSquash, Vector3.one, Time.deltaTime * squashSpeed);
        spriteRenderer.transform.localScale = new Vector3(
            originalScale.x * currentSquash.x, originalScale.y * currentSquash.y, originalScale.z);
        wasMoving = isMoving;
    }

    // ═══════════ ПОЛУЧЕНИЕ УРОНА ═══════════
    public void TakeDamage(float damage, Vector2 knockbackDir)
    { TakeDamage(damage, knockbackDir, null); }

    public void TakeDamage(float damage, Vector2 knockbackDir, Transform attacker)
    {
        EnsureSharedComponents();
        if (sharedHealth != null)
        {
            if (sharedHealth.isDowned)
                return;

            sharedHealth.TakeDamage(damage);
        }
        else
        {
            if (currentHealth <= 0f)
                return;

            SetHealth(currentHealth - damage, maxHealth);
        }

        StartCoroutine(HitFlash());
    }

    IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;
        Color orig = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = orig;
    }

    void Die()
    {
        HandleDowned();
    }

    IEnumerator DeathAnim()
    {
        float e = 0f;
        while (e < 1f) { e += Time.deltaTime; transform.rotation = Quaternion.Euler(0, 0, e * 90f); yield return null; }
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void SetHealth(float current, float max)
    {
        EnsureSharedComponents();
        if (sharedHealth != null)
        {
            sharedHealth.SetHealth(current, max);
            return;
        }

        maxHealth = Mathf.Max(1f, max);
        currentHealth = Mathf.Clamp(current, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void MultiplyHealth(float multiplier)
    {
        EnsureSharedComponents();
        if (sharedHealth != null)
        {
            sharedHealth.MultiplyHealth(multiplier);
            return;
        }

        multiplier = Mathf.Max(0.01f, multiplier);
        SetHealth(currentHealth * multiplier, maxHealth * multiplier);
    }

    public void Revive(float healthPercent)
    {
        gameObject.layer = LayerMask.NameToLayer("Player");
        EnsureSharedComponents();
        if (sharedHealth != null)
            sharedHealth.Revive(healthPercent);
        else
            SetHealth(maxHealth * healthPercent, maxHealth);
    }

    void HandleSharedHealthChanged(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void HandleDowned()
    {
        currentHealth = 0f;
        if (motor != null)
            motor.SetMovementLocked(true);

        HideWrench();
            HideWrench();

    if (rb != null)
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        gameObject.layer = LayerMask.NameToLayer("DeadPlayer");
    }
        if (deathRoutine == null)
            deathRoutine = StartCoroutine(DeathAnim());
    }

    void HandleRevived()
    {
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        StopAllCoroutines();
        transform.rotation = Quaternion.identity;

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

        if (motor != null)
            motor.SetMovementLocked(false);

        HideWrench();
    }

    void EnsureSharedComponents()
    {
        if (sharedInput == null)
            sharedInput = GetComponent<PlayerSharedInput>();
        if (sharedInput == null)
            sharedInput = gameObject.AddComponent<PlayerSharedInput>();

        if (sharedHealth == null)
            sharedHealth = GetComponent<PlayerSharedHealth>();
        if (sharedHealth == null)
            sharedHealth = gameObject.AddComponent<PlayerSharedHealth>();

        if (motor == null)
            motor = GetComponent<PlayerMotor2D>();
        if (motor == null)
            motor = gameObject.AddComponent<PlayerMotor2D>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (motor != null)
            motor.rb = rb;
    }

    void SyncSharedSettings()
    {
        if (sharedInput != null)
            sharedInput.playerNumber = playerNumber;

        if (sharedHealth != null)
        {
            sharedHealth.maxHealth = Mathf.Max(1f, maxHealth);
            sharedHealth.currentHealth = currentHealth > 0f
                ? Mathf.Clamp(currentHealth, 0f, sharedHealth.maxHealth)
                : sharedHealth.maxHealth;
            currentHealth = sharedHealth.currentHealth;
            maxHealth = sharedHealth.maxHealth;
            sharedHealth.NotifyHealthChanged();
        }

        if (motor != null)
        {
            motor.walkSpeed = moveSpeed;
            motor.runSpeed = runSpeed;
        }
    }

    void SubscribeSharedHealth()
    {
        if (sharedHealth == null || sharedHealthSubscribed)
            return;

        sharedHealth.OnHealthChanged += HandleSharedHealthChanged;
        sharedHealth.OnDowned += HandleDowned;
        sharedHealth.OnRevived += HandleRevived;
        sharedHealth.OnHit += OnHitDialogue;
        sharedHealth.OnDowned += OnDeathDialogue;

        sharedHealthSubscribed = true;
    }

    void OnHitDialogue()
    {
        GetComponent<DialogueTrigger>()?.OnDamaged();
        if (Random.value < 0.0006f) PlaySound(onHitVoiceLine);
    }
    void OnDeathDialogue() => GetComponent<DialogueTrigger>()?.OnDeath();

    void UnsubscribeSharedHealth()
    {
        if (sharedHealth == null || !sharedHealthSubscribed)
            return;

        sharedHealth.OnHealthChanged -= HandleSharedHealthChanged;
        sharedHealth.OnDowned -= HandleDowned;
        sharedHealth.OnRevived -= HandleRevived;
        sharedHealth.OnHit -= OnHitDialogue;
        sharedHealth.OnDowned -= OnDeathDialogue;

        sharedHealthSubscribed = false;
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    // ═══════════ ВВОД ═══════════
    Vector2 GetMovementInput()
    {
        return sharedInput != null ? sharedInput.Movement : Vector2.zero;
    }

    bool GetRunInput()
    {
        return sharedInput != null && sharedInput.RunHeld;
    }

    bool GetAttackHeld()
    {
        return sharedInput != null &&
               (sharedInput.GetAction(PlayerControlAction.LightAttack) ||
                sharedInput.GetAction(PlayerControlAction.HeavyAttack));
    }

    // ═══════════ EASING ═══════════
    float EaseInOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f; }
    float EaseOutQuad(float t) { return 1f - (1f - t) * (1f - t); }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(attackPoint.position, attackRange); }
    }
}
