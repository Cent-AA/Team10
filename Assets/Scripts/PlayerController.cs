using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("═══ Игрок ═══")]
    public int playerNumber = 1;

    [Header("═══ Движение ═══")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float dashSpeed = 18f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1f;

    [Header("═══ Здоровье ═══")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float blockDamageReduction = 0.7f;

    [Header("═══ Урон атак ═══")]
    public float jabDamage = 8f;
    public float crossDamage = 12f;
    public float uppercutDamage = 25f;
    public float heavyDamage = 35f;
    public float dashDamage = 15f;

    [Header("═══ Комбо ═══")]
    public float comboWindow = 0.8f;

    [Header("═══ Heavy Abilities ═══")]
    public float lightAttackCooldown = 0.6f;
    public float heavyAttackCooldown = 1.5f;
    public float barrageHoldTime = 2f;
    public float barrageDuration = 4f;
    public float barrageCooldown = 8f;
    public float barrageHitSoundInterval = 0.18f;

    [Header("═══ Hit Effects ═══")]
    public float hitStopDuration = 0.08f;
    public float knockbackForce = 5f;
    public float invulnerabilityTime = 0.3f;

    [Header("═══ Звуки ═══")]
    public AudioClip hitSound;
    public AudioClip heavyHitSound;
    public AudioClip chargeSound;           // Звук зарядки барража
    public AudioClip barrageSound;          // Звук барража
    private AudioSource audioSource;

    [Header("═══ Компоненты ═══")]
    public PuppetAnimator puppet;
    public Rigidbody2D rb;
    public SpriteRenderer[] spriteRenderers;
    public Transform attackPoint;
    public float attackRange = 1.2f;
    public LayerMask enemyLayer;
    public TargetingSystem targeting;
    public GameObject bulletPrefab;
    public Transform firePoint;

    // События
    public System.Action<float, float> OnHealthChanged;
    public System.Action OnDeath;

    // Внутреннее
    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.right;
    private bool isRunning = false;
    private bool isDashing = false;
    private bool isInvulnerable = false;
    private bool isHitStopped = false;
    private float dashCooldownTimer = 0f;
    private int comboStep = 0;
    private float comboTimer = 0f;
    private Color[] originalColors;
    private readonly Collider2D[] attackHitBuffer = new Collider2D[32];
    private readonly Collider2D[] reviveHitBuffer = new Collider2D[16];
    private float lightAttackCooldownTimer = 0f;
    private float heavyAttackCooldownTimer = 0f;
    private float barrageCooldownTimer = 0f;
    private float nextBarrageHitSoundTime = 0f;

    // Зарядка Heavy
    private bool isHoldingHeavy = false;
    private float heavyHoldTime = 0f;
    private bool chargeSoundPlayed = false;
    private bool barrageChargeStarted = false;

    void OnEnable()
    {
        Registry.Register(transform);
    }

    void OnDestroy()
    {
        Registry.Unregister(transform);
    }

    void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
    }

    void Start()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        NotifyHealthChanged();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        EnsurePuppet();
        IncludeMainRendererForHitFlash();

        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
                if (spriteRenderers[i] != null)
                    originalColors[i] = spriteRenderers[i].color;
        }

        if (puppet != null)
        {
            ApplyHeavyAbilitySettings();
            puppet.OnHitFrame += DealAttackDamage;
            puppet.OnBarrageHit += DealBarrageDamage;
        }
    }

    bool EnsurePuppet()
    {
        if (puppet != null) return true;

        puppet = GetComponent<PuppetAnimator>();
        if (puppet == null)
            puppet = GetComponentInChildren<PuppetAnimator>(true);

        return puppet != null;
    }

    void IncludeMainRendererForHitFlash()
    {
        if (puppet == null) return;

        SpriteRenderer mainRenderer = puppet.GetMainRenderer();
        if (mainRenderer == null) return;

        if (spriteRenderers == null)
        {
            spriteRenderers = new[] { mainRenderer };
            return;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == mainRenderer)
                return;
        }

        SpriteRenderer[] expanded = new SpriteRenderer[spriteRenderers.Length + 1];
        System.Array.Copy(spriteRenderers, expanded, spriteRenderers.Length);
        expanded[expanded.Length - 1] = mainRenderer;
        spriteRenderers = expanded;
    }

    void ApplyHeavyAbilitySettings()
    {
        if (puppet == null) return;

        puppet.barrageChargeTime = Mathf.Max(0.01f, barrageHoldTime);
        puppet.barrageDuration = Mathf.Max(0.01f, barrageDuration);
        float maxCircleAppearTime = Mathf.Max(0f, puppet.barrageChargeTime - 0.01f);
        puppet.barrageCircleAppearTime = Mathf.Clamp(puppet.barrageCircleAppearTime, 0f, maxCircleAppearTime);
    }

    public void SetHealth(float current, float max)
    {
        maxHealth = Mathf.Max(1f, max);
        currentHealth = Mathf.Clamp(current, 0f, maxHealth);
        NotifyHealthChanged();
    }

    public void MultiplyHealth(float multiplier)
    {
        multiplier = Mathf.Max(0f, multiplier);
        SetHealth(currentHealth * multiplier, maxHealth * multiplier);
    }

    public void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        if (isHitStopped) return;
        if (!EnsurePuppet()) return;
        if (puppet.IsDead()) return;

        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        TickAbilityCooldowns();
        if (comboTimer > 0) comboTimer -= Time.deltaTime; else comboStep = 0;

        moveInput = GetMovementInput();
        isRunning = GetRunInput();
        bool blocking = GetBlockInput();

        // Направление к таргету
        if (targeting != null && targeting.currentTarget != null)
        {
            Vector2 targetDir = (targeting.currentTarget.position - transform.position).normalized;
            puppet.SetTarget(targeting.currentTarget, targetDir);
        }
        else if (moveInput.magnitude > 0.1f)
        {
            puppet.SetTarget(null, moveInput.normalized);
        }

        HandleHeavyAttackInput();

        if (!puppet.IsBusy() && !isHoldingHeavy)
        {
            if (GetLightAttackInput() && lightAttackCooldownTimer <= 0f) PerformLightAttack();
            else if (GetDashInput() && dashCooldownTimer <= 0) StartCoroutine(DashRoutine());
            else if (GetRollInput()) puppet.Roll();
            else if (blocking) puppet.StartBlock();
            else
            {
                if (puppet.IsBlocking()) puppet.StopBlock();
                UpdateMovement();
            }
        }
        else if (puppet.CurrentState == PuppetAnimator.AnimState.Block && !blocking)
        {
            puppet.StopBlock();
        }

        if (moveInput.magnitude > 0.1f)
            lastMoveDir = moveInput.normalized;
    }

    void FixedUpdate()
    {
        if (isDashing || isHitStopped) return;
        EnsurePuppet();
        if (puppet != null && (puppet.IsDead() || (puppet.IsBusy() && !puppet.IsBarraging()))) return;

        if (rb != null)
        {
            float speed = isRunning ? runSpeed : walkSpeed;
            rb.MovePosition(rb.position + moveInput * speed * Time.fixedDeltaTime);
        }
    }

    void UpdateMovement()
    {
        if (!EnsurePuppet()) return;

        bool moving = moveInput.magnitude > 0.1f;
        puppet.SetMoving(moving, moving && isRunning);
    }

    void TickAbilityCooldowns()
    {
        if (lightAttackCooldownTimer > 0f) lightAttackCooldownTimer -= Time.deltaTime;
        if (heavyAttackCooldownTimer > 0f) heavyAttackCooldownTimer -= Time.deltaTime;
        if (barrageCooldownTimer > 0f) barrageCooldownTimer -= Time.deltaTime;
    }

    void HandleHeavyAttackInput()
    {
        if (!EnsurePuppet())
        {
            isHoldingHeavy = false;
            barrageChargeStarted = false;
            return;
        }

        bool heavyHeld = GetHeavyAttackHeld();
        bool canReadHeavyInput = !puppet.IsBusy() || puppet.CurrentState == PuppetAnimator.AnimState.BarrageCharging;

        if (heavyHeld && canReadHeavyInput)
        {
            if (!isHoldingHeavy && (heavyAttackCooldownTimer <= 0f || barrageCooldownTimer <= 0f))
                BeginHeavyHold();

            if (!isHoldingHeavy) return;

            heavyHoldTime += Time.deltaTime;

            if (!barrageChargeStarted && barrageCooldownTimer <= 0f && !puppet.IsBusy())
                BeginBarrageCharge();

            if (barrageChargeStarted && heavyHoldTime >= barrageHoldTime)
                LaunchBarrage();
        }
        else if (isHoldingHeavy)
        {
            ReleaseHeavyHoldAsHeavy();
        }
    }

    void BeginHeavyHold()
    {
        isHoldingHeavy = true;
        heavyHoldTime = 0f;
        chargeSoundPlayed = false;
        barrageChargeStarted = false;

        if (barrageCooldownTimer <= 0f)
            BeginBarrageCharge();
    }

    void BeginBarrageCharge()
    {
        if (!EnsurePuppet()) return;

        barrageChargeStarted = true;
        puppet.StartBarrageCharge(heavyHoldTime);

        if (!chargeSoundPlayed)
        {
            chargeSoundPlayed = true;
            PlaySound(chargeSound);
        }
    }

    void LaunchBarrage()
    {
        isHoldingHeavy = false;
        barrageChargeStarted = false;
        barrageCooldownTimer = Mathf.Max(0f, barrageCooldown);
        heavyAttackCooldownTimer = Mathf.Max(0f, heavyAttackCooldown);
        puppet.ReleaseBarrageCharge(true, false);
        PlaySound(barrageSound);
    }

    void ReleaseHeavyHoldAsHeavy()
    {
        if (barrageChargeStarted && heavyHoldTime >= barrageHoldTime && barrageCooldownTimer <= 0f)
        {
            LaunchBarrage();
            return;
        }

        isHoldingHeavy = false;

        if (barrageChargeStarted)
        {
            bool heavyReady = heavyAttackCooldownTimer <= 0f;
            puppet.ReleaseBarrageCharge(false, heavyReady);

            if (heavyReady)
            {
                heavyAttackCooldownTimer = Mathf.Max(0f, heavyAttackCooldown);
            }

            barrageChargeStarted = false;
            return;
        }

        if (heavyAttackCooldownTimer <= 0f && !puppet.IsBusy())
        {
            puppet.HeavyAttack();
            heavyAttackCooldownTimer = Mathf.Max(0f, heavyAttackCooldown);
        }
    }

    void PerformLightAttack()
    {
        if (!EnsurePuppet()) return;

        comboTimer = comboWindow;
        comboStep = (comboStep % 2) + 1;
        lightAttackCooldownTimer = Mathf.Max(0f, lightAttackCooldown);

        switch (comboStep)
        {
            case 1: puppet.Jab(); break;
            case 2: puppet.Cross(); break;
        }
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        isInvulnerable = true;
        dashCooldownTimer = dashCooldown;
        if (EnsurePuppet())
            puppet.Dash();

        Vector2 dashDir = moveInput.magnitude > 0.1f ? moveInput.normalized : lastMoveDir;
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;
            if (rb != null) rb.MovePosition(rb.position + dashDir * dashSpeed * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
        yield return new WaitForSeconds(0.1f);
        isInvulnerable = false;
    }

    // ═══════════ УРОН ═══════════
    void DealAttackDamage()
    {
        if (attackPoint == null) return;
        ContactFilter2D attackFilter = new ContactFilter2D();
        attackFilter.SetLayerMask(enemyLayer);
        attackFilter.useLayerMask = true;
        attackFilter.useTriggers = Physics2D.queriesHitTriggers;

        int hitCount = Physics2D.OverlapCircle(attackPoint.position, attackRange, attackFilter, attackHitBuffer);
        float damage = GetCurrentDamage();
        bool hitSomething = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = attackHitBuffer[i];
            if (hit == null) continue;
            if (hit.transform == transform) continue;

            Vector2 knockDir = (hit.transform.position - transform.position).normalized;

            // Зомби
            ZombieAI zombie = hit.GetComponent<ZombieAI>();
            if (zombie != null)
            {
                zombie.TakeDamage(damage, knockDir, transform);
                hitSomething = true;
                StartCoroutine(HitStopRoutine());
            }

            // Другой игрок
            PlayerController enemy = hit.GetComponent<PlayerController>();
            if (enemy != null && enemy != this)
            {
                enemy.TakeDamage(damage, knockDir);
                hitSomething = true;
                StartCoroutine(HitStopRoutine());
            }

            attackHitBuffer[i] = null;
        }

        if (TryDealReviveDamage(damage))
        {
            hitSomething = true;
            StartCoroutine(HitStopRoutine());
        }

        if (hitSomething)
            PlaySound(GetAttackHitSound());
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

    AudioClip GetAttackHitSound()
    {
        if (puppet != null && puppet.CurrentState == PuppetAnimator.AnimState.Heavy && heavyHitSound != null)
            return heavyHitSound;

        return hitSound;
    }

    void DealBarrageDamage(Vector2 dir, float damage)
    {
        if (!EnsurePuppet()) return;
        if (targeting == null || targeting.currentTarget == null) return;

        float dist = Vector2.Distance(transform.position, targeting.currentTarget.position);
        if (dist > puppet.barrageFlyDistance * 2f) return;
        bool hitSomething = false;

        // Бьём таргет
        ZombieAI zombie = targeting.currentTarget.GetComponent<ZombieAI>();
        if (zombie != null)
        {
            zombie.TakeDamage(damage, dir, transform);
            hitSomething = true;
        }

        PlayerController enemy = targeting.currentTarget.GetComponent<PlayerController>();
        if (enemy != null && enemy != this)
        {
            enemy.TakeDamage(damage, dir * 0.1f);  // Маленький нокбэк — враг стоит на месте
            hitSomething = true;
            // Враг не может двигаться во время барража
        }

        if (hitSomething && Time.time >= nextBarrageHitSoundTime)
        {
            nextBarrageHitSoundTime = Time.time + Mathf.Max(0.01f, barrageHitSoundInterval);
            PlaySound(hitSound);
        }
    }

    float GetCurrentDamage()
    {
        if (!EnsurePuppet()) return jabDamage;

        switch (puppet.CurrentState)
        {
            case PuppetAnimator.AnimState.Jab: return jabDamage;
            case PuppetAnimator.AnimState.Cross: return crossDamage;
            case PuppetAnimator.AnimState.Uppercut: return uppercutDamage;
            case PuppetAnimator.AnimState.Heavy: return heavyDamage;
            case PuppetAnimator.AnimState.Dash: return dashDamage;
            default: return jabDamage;
        }
    }

    public void TakeDamage(float damage, Vector2 knockbackDir)
    {
        EnsurePuppet();
        if (isInvulnerable || (puppet != null && puppet.IsDead())) return;

        bool isBlocking = puppet != null && puppet.IsBlocking();
        if (isBlocking) { damage *= (1f - blockDamageReduction); knockbackDir *= 0.3f; }

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        NotifyHealthChanged();

        if (rb != null) StartCoroutine(KnockbackRoutine(knockbackDir));
        StartCoroutine(HitFlashRoutine());
        ArenaCamera.Shake(damage * 0.04f, 0.12f);

        if (currentHealth <= 0) Die();
        else if (puppet != null && !puppet.IsBlocking() && !puppet.IsBarraging())
        {
            puppet.TakeHit();
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    IEnumerator KnockbackRoutine(Vector2 dir)
    {
        float e = 0f;
        while (e < 0.15f) { e += Time.fixedDeltaTime; if (rb != null) rb.MovePosition(rb.position + dir * knockbackForce * Time.fixedDeltaTime); yield return new WaitForFixedUpdate(); }
    }

    IEnumerator HitStopRoutine()
    {
        if (isHitStopped) yield break;  // Не стакать
        isHitStopped = true;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;  // Всегда возвращаем в 1
        isHitStopped = false;
    }

    IEnumerator HitFlashRoutine()
    {
        if (spriteRenderers == null || originalColors == null) yield break;

        foreach (var sr in spriteRenderers) if (sr != null) sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        int restoreCount = Mathf.Min(spriteRenderers.Length, originalColors.Length);
        for (int i = 0; i < restoreCount; i++) if (spriteRenderers[i] != null) spriteRenderers[i].color = originalColors[i];
    }

    IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        float e = 0f;
        while (e < invulnerabilityTime)
        {
            e += 0.1f;
            if (spriteRenderers != null)
                foreach (var sr in spriteRenderers) if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
        }

        if (spriteRenderers != null)
            foreach (var sr in spriteRenderers) if (sr != null) sr.enabled = true;

        isInvulnerable = false;
    }

    void Die() { if (EnsurePuppet()) puppet.Die(); OnDeath?.Invoke(); if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic; }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // ═══════════ ИЗМЕНЕННЫЙ ВВОД КНОПОК ═══════════
    Vector2 GetMovementInput()
    {
        var type = GetInputType();
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
            case InputJoinManager.InputType.KeyboardArrows:
                return PlayerInputBindings.GetKeyboardMovement(playerNumber);
            case InputJoinManager.InputType.Gamepad:
                return PlayerInputBindings.GetGamepadMovement(playerNumber, GetGamepadIndex());
        }

        return Vector2.zero;
    }

    bool GetRunInput()
    {
        return GetHeldInput(PlayerControlAction.Run);
    }

    bool GetLightAttackInput()
    {
        return GetDownInput(PlayerControlAction.LightAttack);
    }

    bool GetHeavyAttackHeld()
    {
        return GetHeldInput(PlayerControlAction.HeavyAttack);
    }

    bool GetDashInput()
    {
        return GetDownInput(PlayerControlAction.Dash);
    }

    bool GetRollInput()
    {
        return GetDownInput(PlayerControlAction.Roll);
    }

    bool GetBlockInput()
    {
        return GetHeldInput(PlayerControlAction.Block);
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

    bool GetDownInput(PlayerControlAction action)
    {
        var type = GetInputType();
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
            case InputJoinManager.InputType.KeyboardArrows:
                return PlayerInputBindings.GetKeyboardActionDown(playerNumber, action);
            case InputJoinManager.InputType.Gamepad:
                return PlayerInputBindings.GetGamepadActionDown(playerNumber, action, GetGamepadIndex());
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

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(attackPoint.position, attackRange); }
    }

    void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            // 1. Переводим позицию курсора с экрана в координаты игрового мира
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0f; // Зануляем Z, так как игра в 2D

            // 2. Считаем вектор направления от дула пушки (firePoint) до мышки
            Vector2 shootDirection = (mousePosition - firePoint.position).normalized;

            // 3. Высчитываем угол поворота в градусах с помощью тригонометрии
            float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;

            // 4. Создаем вращение на основе этого угла по оси Z
            Quaternion bulletRotation = Quaternion.Euler(0f, 0f, angle);

            // 5. Спавним пулю, сразу развернутую в сторону курсора
            Bullet.Spawn(bulletPrefab, firePoint.position, bulletRotation, transform);
        }
    }
}
