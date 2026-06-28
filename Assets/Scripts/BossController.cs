using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossController : MonoBehaviour
{
    const int IdleState = 0;
    const int WalkState = 1;
    const int ScreamState = 2;
    const int DashState = 3;
    const int StunState = 4;

    [Header("Stats")]
    public float moveSpeed = 2f;
    public float stopRange = 2.5f;
    public float maxHealth = 500f;

    [Header("Phases")]
    [Range(0.05f, 0.95f)] public float phaseTwoHealth = 0.7f;
    [Range(0.05f, 0.95f)] public float phaseThreeHealth = 0.35f;
    public float phaseTwoSpeedMultiplier = 1.15f;
    public float phaseThreeSpeedMultiplier = 1.3f;
    public float phaseTwoDashCooldownMultiplier = 0.85f;
    public float phaseThreeDashCooldownMultiplier = 0.7f;
    public float phasePulseTelegraph = 0.75f;
    public float phasePulseRadius = 5f;
    public float phaseTwoPulseDamage = 8f;
    public float phaseThreePulseDamage = 12f;
    public float phasePulseKnockback = 18f;

    [Header("Decision Budget")]
    [Min(0.05f)] public float targetDecisionInterval = 0.25f;
    [Min(0.02f)] public float contactCheckInterval = 0.1f;

    [Header("Animation")]
    public float screamDuration = 3.05f;

    [Header("Parts")]
    public Transform leftArm;
    public Transform rightArm;
    public Transform head;
    public Transform torso;
    public Transform legs;

    [Header("Dash — Trigger")]
    [Tooltip("Перезарядка рывка (сек)")]
    public float dashCooldown = 8f;
    [Tooltip("Максимальная дистанция, с которой босс может начать рывок")]
    public float dashRange = 13f;
    [Tooltip("Ближе этого рывок не запускается (игрок вплотную)")]
    public float dashMinRange = 1.5f;

    [Header("Dash — Choreography")]
    [Tooltip("Замах/телеграф перед рывком — босс отшатывается назад")]
    public float telegraphDuration = 0.42f;
    [Tooltip("Насколько босс отшатывается назад на замахе")]
    public float telegraphRecoil = 1.1f;
    [Tooltip("Скорость рывка (юнитов/сек) — резкий бросок вперёд")]
    public float lungeSpeed = 34f;
    [Tooltip("Страховочный лимит длительности рывка")]
    public float lungeMaxDuration = 0.5f;
    [Tooltip("Насколько далеко за игрока целится бросок (перелёт)")]
    public float lungeOvershoot = 2f;
    [Tooltip("Восстановление после удара")]
    public float recoverDuration = 0.5f;

    [Header("Dash — Hit")]
    [Tooltip("Радиус хитбокса удара (вокруг руки LeftArm)")]
    public float dashHitRadius = 2.6f;
    [Tooltip("Запасное смещение хитбокса вперёд, если LeftArm не назначена")]
    public float dashHitForwardOffset = 1.6f;
    [Tooltip("Урон рывка")]
    public float dashDamage = 22f;
    [Tooltip("Сила броска при ударе рывком (летит далеко)")]
    public float dashKnockback = 26f;
    [Tooltip("Сколько секунд игрок лежит перевёрнутым после броска")]
    public float dashThrowDownDuration = 2f;
    [Tooltip("Подсветка телеграфа (цвет вспышки перед рывком)")]
    public Color telegraphFlash = new Color(1f, 0.35f, 0.2f);

    [Header("Hurt & Stun")]
    public int hitsToStun = 5;
    public float stunDuration = 2f;

    [Header("Contact Damage")]
    [Tooltip("Урон при простом касании игрока")]
    public float contactDamage = 12f;
    [Tooltip("Сила лёгкого толчка при касании")]
    public float contactKnockback = 14f;
    public float contactKnockbackDuration = 0.25f;
    [Tooltip("Минимальный интервал между касаниями одного и того же игрока")]
    public float contactCooldown = 0.75f;

    [Header("UI Bars")]
    public Vector2 hpBarWorldOffset = new Vector2(0f, 2.5f);
    public Vector2 hpBarPixelSize = new Vector2(220f, 22f);
    public Vector2 stunBarPixelSize = new Vector2(160f, 8f);
    public float barCanvasScale = 0.01f;
    public Color hpBarColor = new Color(0.85f, 0.15f, 0.15f);
    public Color hpBarBgColor = new Color(0f, 0f, 0f, 0.75f);
    public Color stunBarColor = new Color(1f, 0.85f, 0.15f);
    public Color stunBarBgColor = new Color(0f, 0f, 0f, 0.75f);

    private Animator anim;
    private float currentHealth;
    private bool isActive;
    private int currentState = -1;
    private Transform currentTarget;
    private Coroutine activationRoutine;
    private Coroutine dashRoutine;
    private Coroutine stunRoutine;
    private Coroutine phaseRoutine;
    private bool hasStateParameter;
    private bool hasHitTrigger;
    private float dashTimer;
    private int hitCounter;
    private bool isStunned;
    private bool isDashing;
    private int currentPhase = 1;
    private float baseMoveSpeed;
    private float baseDashCooldown;
    private float targetDecisionTimer;
    private float contactCheckTimer;
    private readonly System.Collections.Generic.Dictionary<Transform, float> contactHitTimers = new System.Collections.Generic.Dictionary<Transform, float>();
    private readonly System.Collections.Generic.HashSet<Transform> dashHitThisLunge = new System.Collections.Generic.HashSet<Transform>();
    private readonly Collider2D[] hitBuffer = new Collider2D[16];
    private SpriteRenderer[] bodySprites;
    private Color[] bodyOriginalColors;

    [Header("Contact — Detection")]
    [Tooltip("Радиус зоны касания вокруг тела босса")]
    public float contactRadius = 1.9f;
    [Tooltip("Смещение зоны касания по Y (центр тела)")]
    public float contactYOffset = 0f;

    [Header("Arena Bounds")]
    [Tooltip("Спрайт арены (ArenaFoet_0) — босс не выйдет за его границы")]
    public SpriteRenderer arenaBounds;
    [Tooltip("Отступ от края арены")]
    public float arenaMargin = 0.5f;

    // UI
    private Canvas barCanvas;
    private RectTransform hpBarFill;
    private RectTransform stunBarFill;
    private RectTransform stunBarRoot;
    private Transform bossVisualForBars;

    public float ScreamDuration => Mathf.Max(0.01f, screamDuration);

    void Awake()
    {
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        baseMoveSpeed = moveSpeed;
        baseDashCooldown = dashCooldown;
        CacheBodySprites();
    }

    void CacheBodySprites()
    {
        bodySprites = GetComponentsInChildren<SpriteRenderer>(true);
        bodyOriginalColors = new Color[bodySprites.Length];
        for (int i = 0; i < bodySprites.Length; i++)
            bodyOriginalColors[i] = bodySprites[i].color;
    }

    void TintBody(Color color, float lerp)
    {
        if (bodySprites == null) return;
        for (int i = 0; i < bodySprites.Length; i++)
        {
            if (bodySprites[i] == null) continue;
            bodySprites[i].color = Color.Lerp(bodyOriginalColors[i], color, lerp);
        }
    }

    void RestoreBodyTint()
    {
        if (bodySprites == null) return;
        for (int i = 0; i < bodySprites.Length; i++)
            if (bodySprites[i] != null) bodySprites[i].color = bodyOriginalColors[i];
    }

    void Start()
    {
        ValidateAnimator();
        currentHealth = maxHealth;
        baseMoveSpeed = moveSpeed;
        baseDashCooldown = dashCooldown;
        currentPhase = 1;
        SetState(IdleState);
        CreateBarsUI();
        dashTimer = dashCooldown;

        // Босс спавнится в рантайме и не может ссылаться на сцену — берём границы у камеры.
        if (arenaBounds == null) arenaBounds = ArenaCamera.MapSprite;
    }

    public void ConfigureForWave(int wave)
    {
        currentHealth = maxHealth;
        currentPhase = 1;
        moveSpeed = baseMoveSpeed;
        dashCooldown = baseDashCooldown;
        dashTimer = dashCooldown;
        targetDecisionTimer = Random.Range(0f, Mathf.Max(0.05f, targetDecisionInterval));
        contactCheckTimer = 0f;
        UpdateHpBar();
    }

    public void Activate()
    {
        if (activationRoutine != null) StopCoroutine(activationRoutine);
        activationRoutine = StartCoroutine(ScreamThenWalk());
    }

    IEnumerator ScreamThenWalk()
    {
        isActive = false;
        SetState(ScreamState);
        yield return new WaitForSeconds(ScreamDuration);
        StartChaseState();
        activationRoutine = null;
    }

    public void PlayIntroScream()
    {
        StopActivationRoutine();
        isActive = false;
        SetState(ScreamState);
    }

    public void BeginChase()
    {
        StopActivationRoutine();
        StartChaseState();
    }

    void Update()
    {
        if (!isActive || isDashing || isStunned) return;

        targetDecisionTimer -= Time.deltaTime;
        if (currentTarget == null || targetDecisionTimer <= 0f)
        {
            targetDecisionTimer = Mathf.Max(0.05f, targetDecisionInterval);
            currentTarget = GetClosestPlayer();
        }
        if (currentTarget == null) return;

        contactCheckTimer -= Time.deltaTime;
        if (contactCheckTimer <= 0f)
        {
            contactCheckTimer = Mathf.Max(0.02f, contactCheckInterval);
            CheckContactDamage();
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0f && dist <= dashRange && dist >= dashMinRange)
        {
            dashTimer = dashCooldown;
            StartDashAttack();
            return;
        }

        if (dist > stopRange)
        {
            Vector3 dir = (currentTarget.position - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            SetState(WalkState);
            FlipTowards(currentTarget.position.x);
        }
        else
        {
            SetState(IdleState);
        }
    }

    void FlipTowards(float targetX)
    {
        float dirX = targetX - transform.position.x;
        if (Mathf.Approximately(dirX, 0f)) return;

        transform.localScale = new Vector3(
            Mathf.Sign(dirX) * Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );
    }

    void StartChaseState()
    {
        isActive = true;
        SetState(WalkState);
    }

    // ============================================================
    // DASH ATTACK
    // ============================================================

    void StartDashAttack()
    {
        if (dashRoutine != null) StopCoroutine(dashRoutine);
        dashRoutine = StartCoroutine(DashAttackRoutine());
    }

    // Рывок = ТЕЛЕГРАФ (замах назад + вспышка) → БРОСОК сквозь игрока → удар по ходу → восстановление.
    IEnumerator DashAttackRoutine()
    {
        isDashing = true;
        dashHitThisLunge.Clear();
        SetState(DashState);

        Transform target = GetClosestPlayer();
        if (target == null) { EndDash(); yield break; }

        FlipTowards(target.position.x);

        // ── Фаза 1: ТЕЛЕГРАФ — босс отшатывается назад, наливается цветом (читаемый замах) ──
        Vector3 dir = AimDir(target);
        Vector3 recoilStart = transform.position;
        Vector3 recoilEnd = recoilStart - dir * telegraphRecoil;

        float t = 0f;
        while (t < telegraphDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / telegraphDuration);
            float ease = 1f - Mathf.Pow(1f - p, 2f);          // плавный замах с торможением
            transform.position = Vector3.Lerp(recoilStart, recoilEnd, ease);
            TintBody(telegraphFlash, Mathf.PingPong(p * 4f, 1f)); // пульсация вспышки
            if (target != null) FlipTowards(target.position.x);
            yield return null;
        }
        RestoreBodyTint();

        // ── Фаза 2: БРОСОК — резкий рывок к точке за игроком (перелёт), удар проверяется по ходу ──
        Vector3 aimPoint = target != null ? target.position : transform.position + dir * 5f;
        dir = ((Vector2)aimPoint - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = transform.localScale.x >= 0f ? Vector3.right : Vector3.left;
        Vector3 lungeTarget = aimPoint + dir * lungeOvershoot;
        FlipTowards(aimPoint.x);

        float lungeT = 0f;
        while (lungeT < lungeMaxDuration)
        {
            lungeT += Time.deltaTime;
            Vector3 toTarget = lungeTarget - transform.position;
            float step = lungeSpeed * Time.deltaTime;

            if (toTarget.magnitude <= step)
            {
                transform.position = lungeTarget;
                DoDashHit(dir);
                break;
            }

            transform.position += dir * step;
            DoDashHit(dir);   // непрерывная проверка — быстрый игрок не проскочит
            yield return null;
        }

        DoDashHit(dir); // финальная контрольная проверка на месте приземления

        // ── Фаза 3: ВОССТАНОВЛЕНИЕ ──
        if (recoverDuration > 0f) yield return new WaitForSeconds(recoverDuration);

        EndDash();
    }

    Vector3 AimDir(Transform target)
    {
        Vector3 d = target != null
            ? ((Vector2)target.position - (Vector2)transform.position).normalized
            : Vector3.zero;
        if (d.sqrMagnitude < 0.01f) d = transform.localScale.x >= 0f ? Vector3.right : Vector3.left;
        return d;
    }

    // Хитбокс удара — окружность вокруг РУКИ босса (LeftArm). Каждый игрок бьётся раз за рывок и улетает с переворотом.
    void DoDashHit(Vector3 forwardDir)
    {
        Vector2 center = leftArm != null
            ? (Vector2)leftArm.position
            : (Vector2)transform.position + (Vector2)forwardDir * dashHitForwardOffset;

        int count = Physics2D.OverlapCircleNonAlloc(center, dashHitRadius, hitBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hitBuffer[i];
            if (hit == null) continue;

            Transform root = ResolvePlayerRoot(hit);
            if (root == null || dashHitThisLunge.Contains(root)) continue;

            dashHitThisLunge.Add(root);
            HitPlayer(root, dashDamage, dashKnockback, dashThrowDownDuration, true);
            ArenaCamera.Shake(0.5f, 0.3f);
        }
    }

    // Нокбэк через MovePosition: оба типа игроков сами двигаются MovePosition в FixedUpdate,
    // а наше продолжение после WaitForFixedUpdate идёт ПОСЛЕ него — поэтому перебивает управление.
    IEnumerator KnockbackPlayer(Transform player, Vector2 dir, float force, float duration)
    {
        if (player == null || duration <= 0f) yield break;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) yield break;

        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        float t = 0f;
        while (t < duration && player != null && rb != null)
        {
            float falloff = 1f - (t / duration);                 // затухание к концу
            rb.MovePosition(rb.position + dir * force * falloff * Time.fixedDeltaTime);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    void EndDash()
    {
        isDashing = false;
        RestoreBodyTint();
        dashHitThisLunge.Clear();
        SetState(WalkState);
        dashRoutine = null;
    }

    // ============================================================
    // CONTACT DAMAGE — проактивная проверка касания (надёжнее OnCollision)
    // ============================================================

    void CheckContactDamage()
    {
        Vector2 center = (Vector2)transform.position + Vector2.up * contactYOffset;
        int count = Physics2D.OverlapCircleNonAlloc(center, contactRadius, hitBuffer);
        float now = Time.time;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hitBuffer[i];
            if (hit == null) continue;

            Transform root = ResolvePlayerRoot(hit);
            if (root == null) continue;

            if (contactHitTimers.TryGetValue(root, out float nextAllowed) && now < nextAllowed)
                continue;

            contactHitTimers[root] = now + contactCooldown;
            HitPlayer(root, contactDamage, contactKnockback, contactKnockbackDuration, false);
            ArenaCamera.Shake(0.18f, 0.14f);
        }
    }

    // ============================================================
    // ОБЩЕЕ НАНЕСЕНИЕ УРОНА ИГРОКУ (любой тип: боксёр / инженер)
    // ============================================================

    // Любой коллайдер → корневой объект игрока, если это игрок.
    Transform ResolvePlayerRoot(Collider2D col)
    {
        PlayerController pc = col.GetComponentInParent<PlayerController>();
        if (pc != null) return pc.transform;
        EngineerController ec = col.GetComponentInParent<EngineerController>();
        if (ec != null) return ec.transform;
        return null;
    }

    // Урон проходит независимо от типа игрока. knockdown=true → дальний бросок с переворотом и подъёмом;
    // knockdown=false → лёгкий толчок (касание).
    void HitPlayer(Transform playerRoot, float damage, float knockForce, float knockTime, bool knockdown)
    {
        if (playerRoot == null) return;

        Vector2 knockDir = ((Vector2)playerRoot.position - (Vector2)transform.position).normalized;
        if (knockDir.sqrMagnitude < 0.01f)
            knockDir = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;

        PlayerController pc = playerRoot.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage(damage, Vector2.zero);
            if (knockdown) pc.ApplyKnockback(knockDir, knockForce, knockTime);
            else StartCoroutine(KnockbackPlayer(playerRoot, knockDir, knockForce, knockTime));
            return;
        }

        EngineerController ec = playerRoot.GetComponent<EngineerController>();
        if (ec != null)
        {
            ec.TakeDamage(damage, Vector2.zero);
            if (knockdown) ec.ApplyKnockback(knockDir, knockForce, knockTime);
            else StartCoroutine(KnockbackPlayer(playerRoot, knockDir, knockForce, knockTime));
            return;
        }

        PlayerSharedHealth psh = playerRoot.GetComponent<PlayerSharedHealth>();
        if (psh != null) psh.TakeDamage(damage);
        StartCoroutine(KnockbackPlayer(playerRoot, knockDir, knockForce, knockTime));
    }

    // ============================================================
    // DAMAGE & STUN
    // ============================================================

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;

        currentHealth -= amount;
        ArenaCamera.Shake(0.15f, 0.1f);
        UpdateHpBar();

        hitCounter++;
        if (hasHitTrigger) anim.SetTrigger("Hit");

        bool phaseChanged = EvaluatePhaseTransition();

        if (!phaseChanged && hitCounter >= hitsToStun)
        {
            hitCounter = 0;
            EnterStun();
        }

        if (currentHealth <= 0f) Die();
    }

    bool EvaluatePhaseTransition()
    {
        if (currentHealth <= 0f || maxHealth <= 0f)
            return false;

        float healthPercent = currentHealth / maxHealth;
        int nextPhase = healthPercent <= phaseThreeHealth ? 3 : healthPercent <= phaseTwoHealth ? 2 : 1;
        if (nextPhase <= currentPhase)
            return false;

        currentPhase = nextPhase;
        float speedMultiplier = currentPhase >= 3 ? phaseThreeSpeedMultiplier : phaseTwoSpeedMultiplier;
        float cooldownMultiplier = currentPhase >= 3 ? phaseThreeDashCooldownMultiplier : phaseTwoDashCooldownMultiplier;
        moveSpeed = baseMoveSpeed * speedMultiplier;
        dashCooldown = Mathf.Max(1f, baseDashCooldown * cooldownMultiplier);
        dashTimer = Mathf.Min(dashTimer, dashCooldown);

        if (phaseRoutine != null)
            StopCoroutine(phaseRoutine);
        phaseRoutine = StartCoroutine(PhasePulseRoutine(currentPhase));
        return true;
    }

    IEnumerator PhasePulseRoutine(int phase)
    {
        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
            stunRoutine = null;
        }
        isStunned = false;
        if (stunBarRoot != null)
            stunBarRoot.gameObject.SetActive(false);

        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }
        isDashing = false;
        dashHitThisLunge.Clear();
        RestoreBodyTint();

        bool wasActive = isActive;
        isActive = false;
        SetState(ScreamState);

        float duration = Mathf.Max(0.1f, phasePulseTelegraph);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 6f, 1f);
            TintBody(telegraphFlash, pulse);
            yield return null;
        }

        RestoreBodyTint();
        dashHitThisLunge.Clear();
        float damage = phase >= 3 ? phaseThreePulseDamage : phaseTwoPulseDamage;
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, phasePulseRadius, hitBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hitBuffer[i];
            if (hit == null)
                continue;

            Transform root = ResolvePlayerRoot(hit);
            if (root == null || !dashHitThisLunge.Add(root))
                continue;

            HitPlayer(root, damage, phasePulseKnockback, 0.35f, false);
        }

        dashHitThisLunge.Clear();
        ArenaCamera.Shake(0.65f, 0.35f);
        isActive = wasActive;
        SetState(isActive ? WalkState : IdleState);
        phaseRoutine = null;
    }

    void EnterStun()
    {
        if (stunRoutine != null) StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunRoutine());
    }

    IEnumerator StunRoutine()
    {
        if (dashRoutine != null) { StopCoroutine(dashRoutine); dashRoutine = null; }
        isDashing = false;
        RestoreBodyTint();
        dashHitThisLunge.Clear();
        isStunned = true;
        SetState(StunState);

        if (stunBarRoot != null) stunBarRoot.gameObject.SetActive(true);

        float t = 0f;
        while (t < stunDuration)
        {
            t += Time.deltaTime;
            UpdateStunBar(1f - t / stunDuration);
            yield return null;
        }

        if (stunBarRoot != null) stunBarRoot.gameObject.SetActive(false);
        isStunned = false;
        SetState(WalkState);
        stunRoutine = null;
    }

    void Die()
    {
        StopActivationRoutine();
        if (dashRoutine != null) StopCoroutine(dashRoutine);
        if (stunRoutine != null) StopCoroutine(stunRoutine);
        if (phaseRoutine != null) StopCoroutine(phaseRoutine);
        isActive = false;
        isDashing = false;
        RestoreBodyTint();
        SetState(IdleState);
        if (barCanvas != null) Destroy(barCanvas.gameObject);
        Destroy(gameObject, 2f);
    }

    void StopActivationRoutine()
    {
        if (activationRoutine == null) return;
        StopCoroutine(activationRoutine);
        activationRoutine = null;
    }

    // ============================================================
    // ANIMATOR HELPERS
    // ============================================================

    void SetState(int state)
    {
        if (currentState == state) return;
        currentState = state;
        if (!hasStateParameter) return;
        anim.SetFloat("State", state);
    }

    void ValidateAnimator()
    {
        if (anim == null)
        {
            Debug.LogWarning($"{nameof(BossController)} on {name} has no Animator.", this);
            return;
        }

        hasStateParameter = false;
        hasHitTrigger = false;

        foreach (var p in anim.parameters)
        {
            if (p.name == "State" && p.type == AnimatorControllerParameterType.Float)
                hasStateParameter = true;
            if (p.name == "Hit" && p.type == AnimatorControllerParameterType.Trigger)
                hasHitTrigger = true;
        }

        if (!hasStateParameter)
            Debug.LogWarning($"{nameof(BossController)}: нужен float параметр 'State'.", this);
        if (!hasHitTrigger)
            Debug.LogWarning($"{nameof(BossController)}: нужен trigger параметр 'Hit'.", this);
    }

    Transform GetClosestPlayer()
    {
        Registry.CleanupPlayers();
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (Transform player in Registry.Players)
        {
            if (!IsLivingPlayer(player)) continue;
            float d = Vector3.Distance(transform.position, player.position);
            if (d < minDist) { minDist = d; closest = player; }
        }
        return closest;
    }

    bool IsLivingPlayer(Transform player)
    {
        if (player == null || !player.gameObject.activeInHierarchy)
            return false;

        PlayerController heavy = Registry.GetPlayerController(player);
        if (heavy != null)
            return heavy.currentHealth > 0f;

        EngineerController engineer = player.GetComponent<EngineerController>();
        if (engineer == null) engineer = player.GetComponentInChildren<EngineerController>();
        if (engineer == null) engineer = player.GetComponentInParent<EngineerController>();
        return engineer != null && engineer.currentHealth > 0f;
    }

    public float GetHealthPercent() => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsAlive => currentHealth > 0f;

    // ============================================================
    // UI BARS
    // ============================================================

    const float BarBorder = 2f;

    void CreateBarsUI()
    {
        // Канвас — ОТДЕЛЬНЫЙ объект (не дочерний боссу), чтобы не наследовать флип/масштаб тела.
        GameObject canvasObj = new GameObject("BossBarsCanvas");
        barCanvas = canvasObj.AddComponent<Canvas>();
        barCanvas.renderMode = RenderMode.WorldSpace;
        barCanvas.sortingOrder = 100;

        RectTransform canvasRect = barCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(hpBarPixelSize.x, hpBarPixelSize.y + stunBarPixelSize.y + 8f);
        canvasRect.localScale = Vector3.one * barCanvasScale;

        // HP-бар: фон + заливка
        GameObject hpBg = CreateBarPart("HPBg", canvasObj.transform, hpBarPixelSize, hpBarBgColor);
        ((RectTransform)hpBg.transform).anchoredPosition = new Vector2(0f, (stunBarPixelSize.y + 8f) * 0.5f);
        hpBarFill = CreateFill("HPFill", hpBg.transform, hpBarPixelSize, hpBarColor);

        // Stun-бар: фон + заливка (под HP)
        GameObject stunBg = CreateBarPart("StunBg", canvasObj.transform, stunBarPixelSize, stunBarBgColor);
        stunBarRoot = (RectTransform)stunBg.transform;
        stunBarRoot.anchoredPosition = new Vector2(0f, -(hpBarPixelSize.y + 8f) * 0.5f);
        stunBarFill = CreateFill("StunFill", stunBg.transform, stunBarPixelSize, stunBarColor);

        bossVisualForBars = transform;
        stunBarRoot.gameObject.SetActive(false);
        UpdateHpBar();
    }

    GameObject CreateBarPart(string objName, Transform parent, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        return obj;
    }

    // Заливка: прижата к ЛЕВОМУ краю фона, убывает изменением ширины (без перекосов и localScale).
    RectTransform CreateFill(string objName, Transform bg, Vector2 barSize, Color color)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(bg, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(-barSize.x * 0.5f + BarBorder, 0f);
        rect.sizeDelta = new Vector2(barSize.x - BarBorder * 2f, barSize.y - BarBorder * 2f);

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        return rect;
    }

    void UpdateHpBar()
    {
        if (hpBarFill == null) return;
        float inner = hpBarPixelSize.x - BarBorder * 2f;
        hpBarFill.sizeDelta = new Vector2(inner * Mathf.Clamp01(GetHealthPercent()), hpBarPixelSize.y - BarBorder * 2f);
    }

    void UpdateStunBar(float pct)
    {
        if (stunBarFill == null) return;
        float inner = stunBarPixelSize.x - BarBorder * 2f;
        stunBarFill.sizeDelta = new Vector2(inner * Mathf.Clamp01(pct), stunBarPixelSize.y - BarBorder * 2f);
    }

    void LateUpdate()
    {
        ClampToArena();

        if (barCanvas == null || bossVisualForBars == null) return;
        // Канвас просто следует за боссом — без наследования флипа и масштаба.
        barCanvas.transform.position = bossVisualForBars.position + (Vector3)hpBarWorldOffset;
        barCanvas.transform.rotation = Quaternion.identity;
    }

    // Босс не покидает прямоугольник спрайта арены (с отступом).
    void ClampToArena()
    {
        if (arenaBounds == null) return;
        Bounds b = arenaBounds.bounds;
        Vector3 p = transform.position;
        float minX = b.min.x + arenaMargin, maxX = b.max.x - arenaMargin;
        float minY = b.min.y + arenaMargin, maxY = b.max.y - arenaMargin;
        if (minX <= maxX) p.x = Mathf.Clamp(p.x, minX, maxX);
        if (minY <= maxY) p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;
    }

    void OnDestroy()
    {
        if (barCanvas != null) Destroy(barCanvas.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, dashRange);
        Gizmos.color = Color.yellow;
        Vector3 facing = transform.localScale.x >= 0f ? Vector3.right : Vector3.left;
        Gizmos.DrawWireSphere(transform.position + facing * dashHitForwardOffset, dashHitRadius);
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * contactYOffset, contactRadius);
        Gizmos.color = new Color(1f, 0.2f, 0.8f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, phasePulseRadius);
    }
}
