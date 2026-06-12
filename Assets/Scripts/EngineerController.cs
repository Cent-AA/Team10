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
    public float hopHeight = 0.15f;         // Высота прыжка
    public float hopSpeed = 10f;            // Скорость прыжков
    public float hopSquashX = 1.1f;         // Сжатие по X при приземлении
    public float hopSquashY = 0.85f;        // Сжатие по Y при приземлении
    public float squashSpeed = 12f;         // Скорость возврата формы
    public float hopTilt = 5f;              // Наклон при ходьбе

    [Header("═══ Здоровье ═══")]
    public float maxHealth = 80f;
    public float currentHealth;

    [Header("═══ Ключ (атака) ═══")]
    public Transform wrenchPivot;           // Точка вращения ключа (пустой объект)
    public float wrenchSwingAngle = 120f;   // Угол замаха
    public float swingDuration = 0.3f;
    public float swingRecovery = 0.2f;
    public float attackDamage = 12f;
    public float attackRange = 1.5f;
    public float attackCooldown = 0.5f;
    public float knockbackForce = 6f;

    [Header("═══ Компоненты ═══")]
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rb;
    public Transform attackPoint;
    public LayerMask enemyLayer;

    [Header("═══ Звуки ═══")]
    public AudioClip wrenchHitSound;
    public AudioClip hopSound;
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

    // События
    public System.Action<float, float> OnHealthChanged;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (spriteRenderer != null)
            originalScale = spriteRenderer.transform.localScale;

        originalPos = spriteRenderer != null ? spriteRenderer.transform.localPosition : Vector3.zero;
    }

    void Update()
    {
        if (currentHealth <= 0) return;

        attackTimer -= Time.deltaTime;
        moveInput = GetMovementInput();
        isRunning = GetRunInput();

        // Направление
        if (moveInput.magnitude > 0.1f)
            facingDir = Mathf.Sign(moveInput.x != 0 ? moveInput.x : facingDir);

        // Флип спрайта
        if (spriteRenderer != null && moveInput.x != 0)
            spriteRenderer.flipX = moveInput.x < 0;

        // Атака
        if (GetAttackInput() && !isAttacking && attackTimer <= 0)
            StartCoroutine(WrenchSwing());

        // Прыжковая анимация
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

    // ═══════════ ПРЫЖКОВАЯ АНИМАЦИЯ ═══════════
    void AnimateHop()
    {
        if (spriteRenderer == null) return;

        bool isMoving = moveInput.magnitude > 0.1f;

        if (isMoving)
        {
            float speed = isRunning ? hopSpeed * 1.5f : hopSpeed;
            float height = isRunning ? hopHeight * 1.5f : hopHeight;

            hopPhase += Time.deltaTime * speed;

            // Подпрыгивание — абсолютное значение синуса (всегда вверх)
            float hop = Mathf.Abs(Mathf.Sin(hopPhase)) * height;
            spriteRenderer.transform.localPosition = originalPos + Vector3.up * hop;

            // Сжатие при приземлении
            float sinVal = Mathf.Sin(hopPhase);
            if (sinVal < 0.1f && sinVal > -0.1f)
            {
                // Момент касания земли — сжимаемся
                currentSquash = new Vector3(hopSquashX, hopSquashY, 1f);
            }

            // Лёгкий наклон в сторону движения
            float tilt = Mathf.Sin(hopPhase) * hopTilt * facingDir;
            spriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, tilt);

            // Звук приземления
            if (!wasMoving || (Mathf.Abs(sinVal) < 0.05f && hopPhase > 0.5f))
            {
                // Можно добавить звук шага
            }
        }
        else
        {
            // Стоим — плавно возвращаемся
            hopPhase = 0f;
            spriteRenderer.transform.localPosition = Vector3.Lerp(
                spriteRenderer.transform.localPosition, originalPos, Time.deltaTime * 10f);
            spriteRenderer.transform.localRotation = Quaternion.Lerp(
                spriteRenderer.transform.localRotation, Quaternion.identity, Time.deltaTime * 10f);
        }

        // Плавный возврат squash к нормальному
        currentSquash = Vector3.Lerp(currentSquash, Vector3.one, Time.deltaTime * squashSpeed);
        spriteRenderer.transform.localScale = new Vector3(
            originalScale.x * currentSquash.x,
            originalScale.y * currentSquash.y,
            originalScale.z);

        wasMoving = isMoving;
    }

    // ═══════════ ЗАМАХ КЛЮЧОМ ═══════════
    IEnumerator WrenchSwing()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        float startAngle = 0f;
        float endAngle = wrenchSwingAngle * facingDir;

        // Замах
        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swingDuration;
            float eased = t * t;  // Быстрый замах

            if (wrenchPivot != null)
                wrenchPivot.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startAngle, -endAngle, eased));

            // Тело наклоняется в сторону удара
            if (spriteRenderer != null)
                spriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, -10f * facingDir * eased);

            // Проверка хита в середине замаха
            if (t > 0.4f && t < 0.5f)
                DealDamage();

            yield return null;
        }

        // Возврат
        elapsed = 0f;
        while (elapsed < swingRecovery)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swingRecovery;
            float eased = 1f - (1f - t) * (1f - t);

            if (wrenchPivot != null)
                wrenchPivot.localRotation = Quaternion.Lerp(
                    Quaternion.Euler(0, 0, -endAngle), Quaternion.identity, eased);

            if (spriteRenderer != null)
                spriteRenderer.transform.localRotation = Quaternion.Lerp(
                    spriteRenderer.transform.localRotation, Quaternion.identity, eased);

            yield return null;
        }

        if (wrenchPivot != null)
            wrenchPivot.localRotation = Quaternion.identity;

        isAttacking = false;
    }

    // ═══════════ УРОН ═══════════
    void DealDamage()
    {
        if (attackPoint == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;

            Vector2 knockDir = (hit.transform.position - transform.position).normalized;

            ZombieAI zombie = hit.GetComponent<ZombieAI>();
            if (zombie != null)
            {
                zombie.TakeDamage(attackDamage, knockDir * knockbackForce);
                PlaySound(wrenchHitSound);
                ArenaCamera.Shake(0.3f, 0.1f);
            }

            PlayerController player = hit.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(attackDamage, knockDir);
                PlaySound(wrenchHitSound);
                ArenaCamera.Shake(0.3f, 0.1f);
            }
        }
    }

    public void TakeDamage(float damage, Vector2 knockbackDir)
    {
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Вспышка
        StartCoroutine(HitFlash());

        if (currentHealth <= 0)
            Die();
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
        // Простая смерть — падает на бок
        StartCoroutine(DeathAnimation());
    }

    IEnumerator DeathAnimation()
    {
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, 0, elapsed * 90f);
            yield return null;
        }
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    void OnEnable()
    {
        Registry.Register(transform);
    }

    void OnDestroy()
    {
        Registry.Unregister(transform);
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
                if (Input.GetKey(KeyCode.UpArrow)) input.y += 1;
                if (Input.GetKey(KeyCode.DownArrow)) input.y -= 1;
                if (Input.GetKey(KeyCode.LeftArrow)) input.x -= 1;
                if (Input.GetKey(KeyCode.RightArrow)) input.x += 1;
                break;
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

    bool GetAttackInput()
    {
        var type = playerNumber == 1 ? InputJoinManager.player1Input : InputJoinManager.player2Input;
        switch (type)
        {
            case InputJoinManager.InputType.KeyboardWASD: return Input.GetKeyDown(KeyCode.Space);
            case InputJoinManager.InputType.KeyboardArrows: return Input.GetKeyDown(KeyCode.Keypad1);
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}