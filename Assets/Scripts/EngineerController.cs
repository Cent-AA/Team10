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

    [Header("═══ Звуки ═══")]
    public AudioClip wrenchHitSound;
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

    public System.Action<float, float> OnHealthChanged;

    void OnEnable() { Registry.Register(transform); }
    void OnDestroy() { Registry.Unregister(transform); }

    void Start()
    {
        currentHealth = maxHealth;
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
        moveInput = GetMovementInput();
        isRunning = GetRunInput();

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
        if (isAttacking || currentHealth <= 0) return;
        if (rb != null)
        {
            float speed = isRunning ? runSpeed : moveSpeed;
            rb.MovePosition(rb.position + moveInput * speed * Time.fixedDeltaTime);
        }
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

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        foreach (var hit in hits)
        {
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
        return hitSomething;
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
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        StartCoroutine(HitFlash());
        if (currentHealth <= 0) Die();
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
        StartCoroutine(DeathAnim());
    }

    IEnumerator DeathAnim()
    {
        float e = 0f;
        while (e < 1f) { e += Time.deltaTime; transform.rotation = Quaternion.Euler(0, 0, e * 90f); yield return null; }
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    // ═══════════ ВВОД ═══════════
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
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKey(KeyCode.RightShift);
        }
        return false;
    }

    bool GetAttackHeld()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKey(KeyCode.Space);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKey(KeyCode.Keypad1);
        }
        return false;
    }

    // ═══════════ EASING ═══════════
    float EaseInOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f; }
    float EaseOutQuad(float t) { return 1f - (1f - t) * (1f - t); }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(attackPoint.position, attackRange); }
    }
}