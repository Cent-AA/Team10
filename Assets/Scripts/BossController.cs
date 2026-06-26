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

    [Header("Animation")]
    public float screamDuration = 3.05f;

    [Header("Parts")]
    public Transform leftArm;
    public Transform rightArm;
    public Transform head;
    public Transform torso;
    public Transform legs;

    [Header("Dash — Timing (must match animation)")]
    [Tooltip("Полная длительность анимации рывка")]
    public float dashAnimDuration = 3f;
    [Tooltip("Когда происходит сам удар (момент кулака с земли)")]
    public float dashHitTime = 2.5f;
    [Tooltip("Длительность фазы скольжения (до резкого замаха)")]
    public float dashSlideDuration = 2.5f;
    [Tooltip("Длительность резкого замаха перед ударом (2.5 → 2.75)")]
    public float dashWindupDuration = 0.25f;

    [Header("Dash — Behavior")]
    public float dashCooldown = 15f;
    [Tooltip("Если игрок дальше — летит к нему. Если ближе — кружит.")]
    public float dashCircleDistance = 7f;
    [Tooltip("На каком расстоянии от игрока кружится")]
    public float dashCircleRadius = 4.5f;
    [Tooltip("Скорость подлёта (юнитов в секунду)")]
    public float dashApproachSpeed = 14f;
    [Tooltip("Скорость кружения (градусов в секунду) — НЕ слишком много")]
    public float dashCircleSpeed = 120f;
    [Tooltip("Замедление кружения к концу (0..1) — ноль = не замедляется")]
    [Range(0f, 1f)] public float dashCircleSlowdown = 0.5f;
    [Tooltip("Радиус удара в момент кулака")]
    public float dashHitRadius = 4f;
    [Tooltip("Урон рывка")]
    public float dashDamage = 30f;
    [Tooltip("Сила отбрасывания")]
    public float dashKnockback = 14f;
    [Tooltip("Длительность отбрасывания")]
    public float dashKnockbackDuration = 0.25f;

    [Header("Hurt & Stun")]
    public int hitsToStun = 3;
    public float stunDuration = 1f;

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
    private bool hasStateParameter;
    private bool hasHitTrigger;
    private float dashTimer;
    private int hitCounter;
    private bool isStunned;
    private bool isDashing;
    private float approachElapsed; // используется вместо ref

    // UI
    private Canvas barCanvas;
    private RectTransform hpBarFill;
    private RectTransform stunBarFill;
    private RectTransform stunBarRoot;

    public float ScreamDuration => Mathf.Max(0.01f, screamDuration);

    void Awake()
    {
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        ValidateAnimator();
        currentHealth = maxHealth;
        SetState(IdleState);
        CreateBarsUI();
        dashTimer = dashCooldown;
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

        currentTarget = GetClosestPlayer();
        if (currentTarget == null) return;

        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0f)
        {
            dashTimer = dashCooldown;
            StartDashAttack();
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

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

    IEnumerator DashAttackRoutine()
    {
        isDashing = true;
        SetState(DashState);

        Transform target = GetClosestPlayer();
        if (target == null) { EndDash(); yield break; }

        approachElapsed = 0f;

        // Фаза 1: подлёт если далеко
        float distToTarget = Vector3.Distance(transform.position, target.position);
        if (distToTarget > dashCircleDistance)
        {
            yield return ApproachTarget(target);
        }

        // Фаза 2: кружение оставшееся время
        float circleTime = Mathf.Max(0f, dashSlideDuration - approachElapsed);
        yield return CircleAroundTarget(target, circleTime);

        // Фаза 3: резкий замах
        yield return WindupBeforeHit(target);

        // Фаза 4: УДАР
        DoDashHit(target);

        // Фаза 5: восстановление
        float remainder = dashAnimDuration - dashHitTime;
        if (remainder > 0f) yield return new WaitForSeconds(remainder);

        EndDash();
    }

    IEnumerator ApproachTarget(Transform target)
    {
        if (target == null) yield break;

        while (approachElapsed < dashSlideDuration)
        {
            if (target == null) yield break;

            Vector3 toTarget = target.position - transform.position;
            float distNow = toTarget.magnitude;

            if (distNow <= dashCircleRadius + 0.1f) yield break;

            Vector3 dir = toTarget.normalized;
            transform.position += dir * dashApproachSpeed * Time.deltaTime;
            FlipTowards(target.position.x);

            approachElapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator CircleAroundTarget(Transform target, float duration)
    {
        if (target == null || duration <= 0f) yield break;

        Vector3 toBoss = transform.position - target.position;
        if (toBoss.sqrMagnitude < 0.01f)
            toBoss = new Vector3(dashCircleRadius, 0f, 0f);

        float angle = Mathf.Atan2(toBoss.y, toBoss.x) * Mathf.Rad2Deg;
        float dirSign = Random.value > 0.5f ? 1f : -1f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float progress = t / duration;
            float speedMul = Mathf.Lerp(1f, 1f - dashCircleSlowdown, progress);
            angle += dashCircleSpeed * dirSign * speedMul * Time.deltaTime;

            if (target != null)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector3 newPos = target.position + new Vector3(
                    Mathf.Cos(rad) * dashCircleRadius,
                    Mathf.Sin(rad) * dashCircleRadius,
                    0f
                );
                transform.position = newPos;
                FlipTowards(target.position.x);
            }

            yield return null;
        }
    }

    IEnumerator WindupBeforeHit(Transform target)
    {
        if (dashWindupDuration <= 0f) yield break;

        Vector3 startPos = transform.position;
        Vector3 targetPos = target != null
            ? target.position - (target.position - startPos).normalized * (dashHitRadius * 0.5f)
            : startPos;

        float t = 0f;
        while (t < dashWindupDuration)
        {
            t += Time.deltaTime;
            float eased = 1f - Mathf.Pow(1f - t / dashWindupDuration, 3f);
            transform.position = Vector3.LerpUnclamped(startPos, targetPos, eased);
            if (target != null) FlipTowards(target.position.x);
            yield return null;
        }
    }

    void DoDashHit(Transform mainTarget)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, dashHitRadius);
        foreach (var hit in hits)
        {
            var psh = hit.GetComponent<PlayerSharedHealth>();
            if (psh == null) continue;

            psh.TakeDamage(dashDamage);

            Vector2 knockDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            if (knockDir.sqrMagnitude < 0.01f) knockDir = Vector2.right;

            StartCoroutine(KnockbackPlayer(hit.transform, knockDir));
        }
        ArenaCamera.Shake(0.5f, 0.3f);
    }

    IEnumerator KnockbackPlayer(Transform player, Vector2 dir)
    {
        if (player == null) yield break;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) yield break;

        float t = 0f;
        while (t < dashKnockbackDuration && player != null && rb != null)
        {
            t += Time.deltaTime;
            float falloff = 1f - (t / dashKnockbackDuration);
            rb.linearVelocity = dir * dashKnockback * falloff;
            yield return null;
        }
    }

    void EndDash()
    {
        isDashing = false;
        SetState(WalkState);
        dashRoutine = null;
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

        if (hitCounter >= hitsToStun)
        {
            hitCounter = 0;
            EnterStun();
        }

        if (currentHealth <= 0f) Die();
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
        isActive = false;
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
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (Transform player in Registry.Players)
        {
            if (player == null) continue;
            float d = Vector3.Distance(transform.position, player.position);
            if (d < minDist) { minDist = d; closest = player; }
        }
        return closest;
    }

    public float GetHealthPercent() => maxHealth > 0f ? currentHealth / maxHealth : 0f;

    // ============================================================
    // UI BARS
    // ============================================================

    void CreateBarsUI()
    {
        GameObject canvasObj = new GameObject("BossBarsCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = new Vector3(hpBarWorldOffset.x, hpBarWorldOffset.y, 0f);

        barCanvas = canvasObj.AddComponent<Canvas>();
        barCanvas.renderMode = RenderMode.WorldSpace;
        barCanvas.sortingOrder = 100;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(hpBarPixelSize.x, hpBarPixelSize.y + stunBarPixelSize.y + 10f);
        canvasRect.localScale = Vector3.one * barCanvasScale;

        GameObject hpBg = CreateBarPart("HPBg", canvasObj.transform, hpBarPixelSize, hpBarBgColor);
        RectTransform hpBgRect = hpBg.GetComponent<RectTransform>();
        hpBgRect.anchoredPosition = new Vector2(0f, (stunBarPixelSize.y + 10f) * 0.5f);

        GameObject hpFill = CreateBarPart("HPFill", hpBg.transform, hpBarPixelSize, hpBarColor);
        hpBarFill = hpFill.GetComponent<RectTransform>();
        hpBarFill.anchorMin = new Vector2(0f, 0.5f);
        hpBarFill.anchorMax = new Vector2(0f, 0.5f);
        hpBarFill.pivot = new Vector2(0f, 0.5f);
        hpBarFill.sizeDelta = hpBarPixelSize;
        hpBarFill.anchoredPosition = new Vector2(-hpBarPixelSize.x * 0.5f, 0f);

        GameObject stunBg = CreateBarPart("StunBg", canvasObj.transform, stunBarPixelSize, stunBarBgColor);
        stunBarRoot = stunBg.GetComponent<RectTransform>();
        stunBarRoot.anchoredPosition = new Vector2(0f, -(hpBarPixelSize.y + 4f) * 0.5f);

        GameObject stunFill = CreateBarPart("StunFill", stunBg.transform, stunBarPixelSize, stunBarColor);
        stunBarFill = stunFill.GetComponent<RectTransform>();
        stunBarFill.anchorMin = new Vector2(0f, 0.5f);
        stunBarFill.anchorMax = new Vector2(0f, 0.5f);
        stunBarFill.pivot = new Vector2(0f, 0.5f);
        stunBarFill.sizeDelta = stunBarPixelSize;
        stunBarFill.anchoredPosition = new Vector2(-stunBarPixelSize.x * 0.5f, 0f);

        stunBarRoot.gameObject.SetActive(false);
        UpdateHpBar();
    }

    GameObject CreateBarPart(string objName, Transform parent, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        return obj;
    }

    void UpdateHpBar()
    {
        if (hpBarFill == null) return;
        float pct = Mathf.Clamp01(GetHealthPercent());
        hpBarFill.localScale = new Vector3(pct, 1f, 1f);
    }

    void UpdateStunBar(float pct)
    {
        if (stunBarFill == null) return;
        stunBarFill.localScale = new Vector3(Mathf.Clamp01(pct), 1f, 1f);
    }

    void LateUpdate()
    {
        // Канвас не флипается вместе с боссом
        if (barCanvas != null)
        {
            Vector3 local = barCanvas.transform.localScale;
            float absX = Mathf.Abs(local.x);
            if (transform.localScale.x < 0f)
                barCanvas.transform.localScale = new Vector3(-absX, local.y, local.z);
            else
                barCanvas.transform.localScale = new Vector3(absX, local.y, local.z);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dashHitRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, dashCircleDistance);
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, dashCircleRadius);
    }
}