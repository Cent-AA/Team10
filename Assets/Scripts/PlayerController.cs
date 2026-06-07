using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("═══ Стрельба ═══")]
    public GameObject bulletPrefab; // Сюда пойдёт синий префаб из папки проекта
    public Transform firePoint;     // Сюда пойдёт точка со сцены

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

    // Зарядка Heavy
    private bool isHoldingHeavy = false;
    private float heavyHoldTime = 0f;
    private bool chargeSoundPlayed = false;

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

        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
                if (spriteRenderers[i] != null)
                    originalColors[i] = spriteRenderers[i].color;
        }

        if (puppet != null)
        {
            puppet.OnHitFrame += DealAttackDamage;
            puppet.OnBarrageHit += DealBarrageDamage;
        }
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
        if (puppet != null && puppet.IsDead()) return;
        if (isHitStopped) return;

        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
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

        // Heavy зажатие → зарядка барража
        bool heavyHeld = GetHeavyAttackHeld();
        bool canCharge = !puppet.IsBusy() || puppet.CurrentState == PuppetAnimator.AnimState.BarrageCharging;

        if (heavyHeld && canCharge)
        {
            if (!isHoldingHeavy)
            {
                isHoldingHeavy = true;
                heavyHoldTime = 0f;
                chargeSoundPlayed = false;
            }
            heavyHoldTime += Time.deltaTime;

            // Через 2 секунды — начинаем зарядку
            if (heavyHoldTime >= 2f && !chargeSoundPlayed)
            {
                chargeSoundPlayed = true;
                puppet.StartBarrageCharge();
                PlaySound(chargeSound);
            }
        }
        else if (isHoldingHeavy)
        {
            isHoldingHeavy = false;

            if (heavyHoldTime >= 7f)
            {
                // БАРРАЖ!
                puppet.ReleaseBarrageCharge();
                PlaySound(barrageSound);
            }
            else if (heavyHoldTime >= 2f)
            {
                // Не дозарядил — обычный heavy
                puppet.ReleaseBarrageCharge();
                PlaySound(heavyHitSound);
            }
            else if (!puppet.IsBusy())
            {
                // Быстрое нажатие — обычный heavy
                puppet.HeavyAttack();
                PlaySound(heavyHitSound);
            }
        }

        if (!puppet.IsBusy() && !isHoldingHeavy)
        {
            if (GetLightAttackInput()) PerformComboAttack();
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

        if (Input.GetKeyDown(KeyCode.J))
        {
            Shoot();
        }
    }

    void FixedUpdate()
    {
        if (isDashing || isHitStopped) return;
        if (puppet != null && (puppet.IsDead() || (puppet.IsBusy() && !puppet.IsBarraging()))) return;

        if (rb != null)
        {
            float speed = isRunning ? runSpeed : walkSpeed;
            rb.MovePosition(rb.position + moveInput * speed * Time.fixedDeltaTime);
        }
    }

    void UpdateMovement()
    {
        bool moving = moveInput.magnitude > 0.1f;
        puppet.SetMoving(moving, moving && isRunning);
    }

    void PerformComboAttack()
    {
        comboTimer = comboWindow;
        comboStep = (comboStep % 3) + 1;
        PlaySound(hitSound);

        switch (comboStep)
        {
            case 1: puppet.Jab(); break;
            case 2: puppet.Cross(); break;
            case 3: puppet.Uppercut(); comboStep = 0; break;
        }
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        isInvulnerable = true;
        dashCooldownTimer = dashCooldown;
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        float damage = GetCurrentDamage();

        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;

            Vector2 knockDir = (hit.transform.position - transform.position).normalized;

            // Зомби
            ZombieAI zombie = hit.GetComponent<ZombieAI>();
            if (zombie != null)
            {
                zombie.TakeDamage(damage, knockDir);
                PlaySound(hitSound);
                StartCoroutine(HitStopRoutine());
            }

            // Другой игрок
            PlayerController enemy = hit.GetComponent<PlayerController>();
            if (enemy != null && enemy != this)
            {
                enemy.TakeDamage(damage, knockDir);
                PlaySound(hitSound);
                StartCoroutine(HitStopRoutine());
            }
        }
    }

    void DealBarrageDamage(Vector2 dir, float damage)
    {
        if (targeting == null || targeting.currentTarget == null) return;

        float dist = Vector2.Distance(transform.position, targeting.currentTarget.position);
        if (dist > puppet.barrageFlyDistance * 2f) return;

        // Бьём таргет
        ZombieAI zombie = targeting.currentTarget.GetComponent<ZombieAI>();
        if (zombie != null) zombie.TakeDamage(damage, dir);

        PlayerController enemy = targeting.currentTarget.GetComponent<PlayerController>();
        if (enemy != null && enemy != this)
        {
            enemy.TakeDamage(damage, dir * 0.1f);  // Маленький нокбэк — враг стоит на месте
            // Враг не может двигаться во время барража
        }
    }

    float GetCurrentDamage()
    {
        if (puppet == null) return jabDamage;
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
        if (isInvulnerable || puppet.IsDead()) return;
        if (puppet.IsBlocking()) { damage *= (1f - blockDamageReduction); knockbackDir *= 0.3f; }

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        NotifyHealthChanged();

        if (rb != null) StartCoroutine(KnockbackRoutine(knockbackDir));
        StartCoroutine(HitFlashRoutine());
        ArenaCamera.Shake(damage * 0.04f, 0.12f);

        if (currentHealth <= 0) Die();
        else if (!puppet.IsBlocking() && !puppet.IsBarraging())
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
        if (spriteRenderers == null) yield break;
        foreach (var sr in spriteRenderers) if (sr != null) sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < spriteRenderers.Length; i++) if (spriteRenderers[i] != null) spriteRenderers[i].color = originalColors[i];
    }

    IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        float e = 0f;
        while (e < invulnerabilityTime) { e += 0.1f; foreach (var sr in spriteRenderers) if (sr != null) sr.enabled = !sr.enabled; yield return new WaitForSeconds(0.1f); }
        foreach (var sr in spriteRenderers) if (sr != null) sr.enabled = true;
        isInvulnerable = false;
    }

    void Die() { puppet.Die(); OnDeath?.Invoke(); if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic; }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // ═══════════ ИЗМЕНЕННЫЙ ВВОД КНОПОК ═══════════
    Vector2 GetMovementInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        Vector2 input = Vector2.zero;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
                if (Input.GetKey(KeyCode.W)) input.y += 1; if (Input.GetKey(KeyCode.S)) input.y -= 1;
                if (Input.GetKey(KeyCode.A)) input.x -= 1; if (Input.GetKey(KeyCode.D)) input.x += 1; break;
            case InputJoinManager.InputType.KeyboardArrows:
                if (Input.GetKey(KeyCode.UpArrow)) input.y += 1; if (Input.GetKey(KeyCode.DownArrow)) input.y -= 1;
                if (Input.GetKey(KeyCode.LeftArrow)) input.x -= 1; if (Input.GetKey(KeyCode.RightArrow)) input.x += 1; break;
            case InputJoinManager.InputType.Gamepad:
                input.x = Input.GetAxis("Horizontal"); input.y = Input.GetAxis("Vertical"); break;
        }
        return input.normalized;
    }

    bool GetRunInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKey(KeyCode.LeftShift);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKey(KeyCode.RightShift); // Бег на правый Shift
        }
        return false;
    }

    bool GetLightAttackInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKeyDown(KeyCode.Space);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKeyDown(KeyCode.Keypad0); // Простой удар на Numpad 0
        }
        return false;
    }

    bool GetHeavyAttackHeld()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKey(KeyCode.Q);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKey(KeyCode.Keypad1); // Тяжелый удар/супер на Numpad 1
        }
        return false;
    }

    bool GetDashInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKeyDown(KeyCode.R);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKeyDown(KeyCode.Keypad2); // Рывок на Numpad 2
        }
        return false;
    }

    bool GetRollInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKeyDown(KeyCode.F);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKeyDown(KeyCode.Keypad3); // Перекат на Numpad 3
        }
        return false;
    }

    bool GetBlockInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKey(KeyCode.C);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKey(KeyCode.Keypad4); // Блок на Numpad 4
        }
        return false;
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
            Instantiate(bulletPrefab, firePoint.position, bulletRotation);
        }
    }
}
