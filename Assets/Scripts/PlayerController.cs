using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    public float blockDamageReduction = 0.7f;  // 70% урона блокируется

    [Header("═══ Атаки ═══")]
    public float jabDamage = 8f;
    public float crossDamage = 12f;
    public float uppercutDamage = 25f;
    public float heavyDamage = 35f;
    public float spinDamage = 18f;
    public float dashDamage = 15f;

    [Header("═══ Комбо ═══")]
    public float comboWindow = 0.8f;          // Время на продолжение комбо

    [Header("═══ Hit-Stop & Knockback ═══")]
    public float hitStopDuration = 0.08f;
    public float knockbackForce = 5f;
    public float invulnerabilityTime = 0.3f;

    [Header("═══ Компоненты ═══")]
    public PuppetAnimator puppet;
    public Rigidbody2D rb;
    public SpriteRenderer[] spriteRenderers;  // Все спрайты для подсветки урона
    public Transform attackPoint;
    public float attackRange = 1.2f;
    public LayerMask enemyLayer;

    [Header("═══ Эффекты ═══")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.1f;

    // События
    public System.Action<float, float> OnHealthChanged;  // current, max
    public System.Action OnDeath;
    public System.Action<float> OnHit;  // damage

    // Внутреннее состояние
    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.right;
    private bool isRunning = false;
    private bool isDashing = false;
    private bool isInvulnerable = false;
    private bool isHitStopped = false;
    private float dashCooldownTimer = 0f;

    // Комбо
    private int comboStep = 0;
    private float comboTimer = 0f;

    // Подсветка
    private Color[] originalColors;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Запоминаем оригинальные цвета спрайтов
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
                if (spriteRenderers[i] != null)
                    originalColors[i] = spriteRenderers[i].color;
        }

        // Подписываемся на хитфреймы анимации
        if (puppet != null)
            puppet.OnHitFrame += DealAttackDamage;
    }

    void Update()
    {
        if (puppet != null && puppet.IsDead()) return;
        if (isHitStopped) return;

        // Кулдауны
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (comboTimer > 0) comboTimer -= Time.deltaTime;
        else comboStep = 0;

        // Ввод
        moveInput = GetMovementInput();
        isRunning = GetRunInput();
        bool blocking = GetBlockInput();

        // Обработка состояний
        if (!puppet.IsBusy())
        {
            // Атаки имеют приоритет
            if (GetLightAttackInput()) PerformComboAttack();
            else if (GetHeavyAttackInput()) PerformHeavyAttack();
            else if (GetSpinAttackInput()) PerformSpinAttack();
            else if (GetDashInput() && dashCooldownTimer <= 0) StartCoroutine(DashRoutine());
            else if (GetRollInput()) puppet.Roll();
            else
            {
                // Блок или движение
                if (blocking)
                {
                    puppet.StartBlock();
                }
                else
                {
                    if (puppet.IsBlocking()) puppet.StopBlock();
                    UpdateMovement();
                }
            }
        }
        else if (puppet.CurrentState == PuppetAnimator.AnimState.Block && !blocking)
        {
            puppet.StopBlock();
        }

        // Обновление направления
        if (moveInput.magnitude > 0.1f)
        {
            lastMoveDir = moveInput.normalized;
            puppet.SetFacing(moveInput.x);
        }
    }

    void FixedUpdate()
    {
        if (isDashing || isHitStopped) return;
        if (puppet != null && (puppet.IsDead() || puppet.IsBusy())) return;

        // Движение через MovePosition
        if (rb != null)
        {
            float speed = isRunning ? runSpeed : walkSpeed;
            Vector2 newPos = rb.position + moveInput * speed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
        }
    }

    void UpdateMovement()
    {
        bool moving = moveInput.magnitude > 0.1f;
        puppet.SetMoving(moving, moving && isRunning);
    }

    // ═══════════ КОМБО АТАКА ═══════════
    void PerformComboAttack()
    {
        comboTimer = comboWindow;
        comboStep = (comboStep % 3) + 1;

        switch (comboStep)
        {
            case 1: puppet.Jab(); break;
            case 2: puppet.Cross(); break;
            case 3: puppet.Uppercut(); comboStep = 0; break;
        }
    }

    void PerformHeavyAttack()
    {
        puppet.HeavyAttack();
        comboStep = 0;
    }

    void PerformSpinAttack()
    {
        puppet.SpinAttack();
        comboStep = 0;
    }

    // ═══════════ РЫВОК С I-FRAMES ═══════════
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
            if (rb != null)
                rb.MovePosition(rb.position + dashDir * dashSpeed * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
        yield return new WaitForSeconds(0.1f);
        isInvulnerable = false;
    }

    // ═══════════ НАНЕСЕНИЕ УРОНА ═══════════
    void DealAttackDamage()
    {
        if (attackPoint == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        float damage = GetCurrentAttackDamage();

        foreach (var hit in hits)
        {
            PlayerController enemy = hit.GetComponent<PlayerController>();
            if (enemy != null && enemy != this)
            {
                Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                enemy.TakeDamage(damage, knockDir);
                StartCoroutine(HitStopRoutine());
            }
        }
    }

    float GetCurrentAttackDamage()
    {
        if (puppet == null) return jabDamage;
        switch (puppet.CurrentState)
        {
            case PuppetAnimator.AnimState.Jab: return jabDamage;
            case PuppetAnimator.AnimState.Cross: return crossDamage;
            case PuppetAnimator.AnimState.Uppercut: return uppercutDamage;
            case PuppetAnimator.AnimState.Heavy: return heavyDamage;
            case PuppetAnimator.AnimState.Spin: return spinDamage;
            case PuppetAnimator.AnimState.Dash: return dashDamage;
            default: return jabDamage;
        }
    }

    // ═══════════ ПОЛУЧЕНИЕ УРОНА ═══════════
    public void TakeDamage(float damage, Vector2 knockbackDir)
    {
        if (isInvulnerable || puppet.IsDead()) return;

        // Блок снижает урон
        if (puppet.IsBlocking())
        {
            damage *= (1f - blockDamageReduction);
            knockbackDir *= 0.3f;  // Меньше нокбэк при блоке
        }

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHit?.Invoke(damage);

        // Нокбэк
        if (rb != null)
            StartCoroutine(KnockbackRoutine(knockbackDir));

        // Эффекты
        StartCoroutine(HitFlashRoutine());
        StartCoroutine(HitStopRoutine());
        ArenaCamera.Shake(damage * 0.05f, 0.15f);

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (!puppet.IsBlocking())
        {
            puppet.TakeHit();
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    IEnumerator KnockbackRoutine(Vector2 dir)
    {
        float elapsed = 0f;
        float duration = 0.15f;
        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            if (rb != null)
                rb.MovePosition(rb.position + dir * knockbackForce * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator HitStopRoutine()
    {
        isHitStopped = true;
        float prevScale = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = prevScale;
        isHitStopped = false;
    }

    IEnumerator HitFlashRoutine()
    {
        if (spriteRenderers == null) yield break;

        foreach (var sr in spriteRenderers)
            if (sr != null) sr.color = hitFlashColor;

        yield return new WaitForSeconds(hitFlashDuration);

        for (int i = 0; i < spriteRenderers.Length; i++)
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = originalColors[i];
    }

    IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;

        // Мерцание
        float elapsed = 0f;
        while (elapsed < invulnerabilityTime)
        {
            elapsed += 0.1f;
            foreach (var sr in spriteRenderers)
                if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
        }

        foreach (var sr in spriteRenderers)
            if (sr != null) sr.enabled = true;

        isInvulnerable = false;
    }

    void Die()
    {
        puppet.Die();
        OnDeath?.Invoke();
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
    }

    // ═══════════ ВВОД ═══════════
    Vector2 GetMovementInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        Vector2 input = Vector2.zero;

        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD:
                if (Input.GetKey(KeyCode.W)) input.y += 1;
                if (Input.GetKey(KeyCode.S)) input.y -= 1;
                if (Input.GetKey(KeyCode.A)) input.x -= 1;
                if (Input.GetKey(KeyCode.D)) input.x += 1;
                break;
            case InputJoinManager.InputType.KeyboardArrows:
                if (Input.GetKey(KeyCode.UpArrow)) input.y += 1;
                if (Input.GetKey(KeyCode.DownArrow)) input.y -= 1;
                if (Input.GetKey(KeyCode.LeftArrow)) input.x -= 1;
                if (Input.GetKey(KeyCode.RightArrow)) input.x += 1;
                break;
            case InputJoinManager.InputType.Gamepad:
                input.x = Input.GetAxis("Horizontal");
                input.y = Input.GetAxis("Vertical");
                break;
        }
        return input.normalized;
    }

    bool GetRunInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKey(KeyCode.LeftShift);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKey(KeyCode.RightControl);
            case InputJoinManager.InputType.Gamepad:
                int pad = playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;
                return GetPadButton(pad, 4);  // L1 / LB
        }
        return false;
    }

    bool GetLightAttackInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKeyDown(KeyCode.Space);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKeyDown(KeyCode.Alpha1);
            case InputJoinManager.InputType.Gamepad:
                int pad = playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;
                return GetPadButtonDown(pad, 0);
        }
        return false;
    }

    bool GetHeavyAttackInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKeyDown(KeyCode.Q);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKeyDown(KeyCode.Alpha2);
            case InputJoinManager.InputType.Gamepad:
                int pad = playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;
                return GetPadButtonDown(pad, 2);
        }
        return false;
    }

    bool GetSpinAttackInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKeyDown(KeyCode.E);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKeyDown(KeyCode.Alpha3);
            case InputJoinManager.InputType.Gamepad:
                int pad = playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;
                return GetPadButtonDown(pad, 3);
        }
        return false;
    }

    bool GetDashInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKeyDown(KeyCode.R);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKeyDown(KeyCode.Alpha4);
            case InputJoinManager.InputType.Gamepad:
                int pad = playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;
                return GetPadButtonDown(pad, 1);
        }
        return false;
    }

    bool GetRollInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKeyDown(KeyCode.F);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKeyDown(KeyCode.Alpha5);
            case InputJoinManager.InputType.Gamepad:
                int pad = playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;
                return GetPadButtonDown(pad, 5);
        }
        return false;
    }

    bool GetBlockInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKey(KeyCode.C);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKey(KeyCode.Alpha6);
            case InputJoinManager.InputType.Gamepad:
                int pad = playerNumber == 1 ? InputJoinManager.player1GamepadIndex : InputJoinManager.player2GamepadIndex;
                return GetPadButton(pad, 6);  // L2
        }
        return false;
    }

    bool GetPadButton(int pad, int button)
    {
        if (pad < 0) return false;
        try
        {
            KeyCode kc = (KeyCode)System.Enum.Parse(typeof(KeyCode), "Joystick" + pad + "Button" + button);
            return Input.GetKey(kc);
        }
        catch { return false; }
    }

    bool GetPadButtonDown(int pad, int button)
    {
        if (pad < 0) return false;
        try
        {
            KeyCode kc = (KeyCode)System.Enum.Parse(typeof(KeyCode), "Joystick" + pad + "Button" + button);
            return Input.GetKeyDown(kc);
        }
        catch { return false; }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}