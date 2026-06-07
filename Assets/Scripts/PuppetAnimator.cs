using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PuppetAnimator : MonoBehaviour
{
    [Header("═══ Части тела ═══")]
    public Transform head;
    public Transform torso;
    public Transform leftArm;
    public Transform rightArm;
    public Transform leftLeg;
    public Transform rightLeg;

    [Header("═══ Idle ═══")]
    public float idleBobSpeed = 2f;
    public float idleBobAmount = 0.02f;
    public float idleBreathAmount = 0.005f;
    public float idleArmSway = 2f;

    [Header("═══ Ходьба / Бег ═══")]
    public float walkLegAngle = 25f;
    public float walkLegSpeed = 10f;
    public float walkArmAngle = 20f;
    public float walkBob = 0.04f;
    public float runLegAngle = 50f;
    public float runLegSpeed = 18f;
    public float runArmAngle = 40f;
    public float runBob = 0.1f;

    [Header("═══ Джеб / Кросс / Апперкот ═══")]
    public float jabDuration = 0.3f;
    public float jabArmScale = 1.3f;
    public float jabFlyDistance = 1.5f;

    [Header("═══ Heavy (ракетные руки) ═══")]
    public float heavyAnticipation = 0.3f;
    public float heavyStrike = 0.2f;
    public float heavyRecovery = 0.4f;
    public float heavyArmScale = 1.5f;
    public float heavyFlyDistance = 3f;

    [Header("═══ БАРРАЖ ═══")]
    public float barrageChargeTime = 7f;
    public float barrageCircleAppearTime = 2f;
    public float barrageDuration = 5f;
    public float barrageHitInterval = 0.06f;
    public int barrageArmClones = 4;
    public float barrageArmScale = 1f;
    public float barrageFlyDistance = 3f;
    public float barrageDamagePerHit = 3f;
    public Sprite circleSprite;              // Перетащи свой белый круг сюда

    [Header("═══ Рывок ═══")]
    public float dashDuration = 0.25f;

    [Header("═══ Блок / Кувырок ═══")]
    public float blockArmAngle = 60f;
    public float rollDuration = 0.5f;

    [Header("═══ Хит / Смерть ═══")]
    public float hitDuration = 0.25f;
    public float deathDuration = 1f;

    public enum AnimState
    {
        Idle, Walk, Run,
        Jab, Cross, Uppercut, Heavy,
        Dash, Block, Roll,
        Hit, Death, Barrage, BarrageCharging
    }

    private AnimState currentState = AnimState.Idle;
    private float stateTimer = 0f;
    private bool isBlocking = false;

    // Таргет
    private Transform currentTarget;
    private Vector2 targetDir = Vector2.right;

    // Барраж
    private float chargeTimer = 0f;
    private bool isCharging = false;
    private GameObject chargeCircle;
    private SpriteRenderer chargeCircleRenderer;
    private List<GameObject> barrageClones = new List<GameObject>();
    private List<SpriteRenderer> barrageCloneRenderers = new List<SpriteRenderer>();

    // Начальные позиции
    private Vector3 headStartPos;
    private Vector3 torsoStartScale;
    private Quaternion torsoStartRot;
    private Vector3 leftArmStartPos, rightArmStartPos;
    private Quaternion leftArmStartRot, rightArmStartRot;
    private Vector3 leftArmStartScale, rightArmStartScale;
    private Quaternion leftLegStartRot, rightLegStartRot;

    // События
    public System.Action OnHitFrame;
    public System.Action<float> OnAttackEnd;
    public System.Action<Vector2, float> OnBarrageHit;

    void Start()
    {
        if (head != null) headStartPos = head.localPosition;
        if (torso != null) { torsoStartScale = torso.localScale; torsoStartRot = torso.localRotation; }
        if (leftArm != null) { leftArmStartPos = leftArm.localPosition; leftArmStartRot = leftArm.localRotation; leftArmStartScale = leftArm.localScale; }
        if (rightArm != null) { rightArmStartPos = rightArm.localPosition; rightArmStartRot = rightArm.localRotation; rightArmStartScale = rightArm.localScale; }
        if (leftLeg != null) leftLegStartRot = leftLeg.localRotation;
        if (rightLeg != null) rightLegStartRot = rightLeg.localRotation;
    }

    void Update()
    {
        stateTimer += Time.deltaTime;
        switch (currentState)
        {
            case AnimState.Idle: AnimateIdle(); break;
            case AnimState.Walk: AnimateWalk(false); break;
            case AnimState.Run: AnimateWalk(true); break;
            case AnimState.Jab: AnimateArmFly(rightArm, rightArmStartPos, rightArmStartRot, rightArmStartScale, jabArmScale, jabFlyDistance, jabDuration); break;
            case AnimState.Cross: AnimateArmFly(leftArm, leftArmStartPos, leftArmStartRot, leftArmStartScale, jabArmScale, jabFlyDistance, jabDuration); break;
            case AnimState.Uppercut: AnimateArmFly(rightArm, rightArmStartPos, rightArmStartRot, rightArmStartScale, jabArmScale * 1.5f, jabFlyDistance * 1.3f, jabDuration * 1.2f); break;
            case AnimState.Heavy: AnimateHeavy(); break;
            case AnimState.BarrageCharging: AnimateCharge(); break;
            case AnimState.Barrage: break;
            case AnimState.Dash: AnimateDash(); break;
            case AnimState.Block: AnimateBlock(); break;
            case AnimState.Roll: AnimateRoll(); break;
            case AnimState.Hit: AnimateHit(); break;
            case AnimState.Death: AnimateDeath(); break;
        }
    }

    void SetState(AnimState s) { currentState = s; stateTimer = 0f; }

    // ═══════════ IDLE ═══════════
    void AnimateIdle()
    {
        float t = Time.time * idleBobSpeed;
        if (head != null) head.localPosition = headStartPos + Vector3.up * Mathf.Sin(t) * idleBobAmount;
        if (torso != null) { torso.localScale = new Vector3(torsoStartScale.x, torsoStartScale.y * (1f + Mathf.Sin(t) * idleBreathAmount), torsoStartScale.z); torso.localRotation = torsoStartRot; }
        ResetArms();
        SmoothReturnLegs();
    }

    // ═══════════ ХОДЬБА ═══════════
    void AnimateWalk(bool run)
    {
        float spd = run ? runLegSpeed : walkLegSpeed;
        float leg = run ? runLegAngle : walkLegAngle;
        float arm = run ? runArmAngle : walkArmAngle;
        float bob = run ? runBob : walkBob;
        float t = Time.time * spd;

        if (leftLeg != null) leftLeg.localRotation = leftLegStartRot * Quaternion.Euler(0, 0, Mathf.Sin(t) * leg);
        if (rightLeg != null) rightLeg.localRotation = rightLegStartRot * Quaternion.Euler(0, 0, -Mathf.Sin(t) * leg);
        if (leftArm != null) { leftArm.localRotation = leftArmStartRot * Quaternion.Euler(0, 0, -Mathf.Sin(t) * arm); leftArm.localScale = leftArmStartScale; leftArm.localPosition = leftArmStartPos; }
        if (rightArm != null) { rightArm.localRotation = rightArmStartRot * Quaternion.Euler(0, 0, Mathf.Sin(t) * arm); rightArm.localScale = rightArmStartScale; rightArm.localPosition = rightArmStartPos; }
        if (torso != null) { torso.localRotation = torsoStartRot; torso.localScale = torsoStartScale; }
        if (head != null) head.localPosition = headStartPos + Vector3.up * Mathf.Abs(Mathf.Sin(t * 2f)) * bob;
    }

    // ═══════════ РУКА-РАКЕТА (Jab/Cross/Uppercut) ═══════════
    void AnimateArmFly(Transform arm, Vector3 startPos, Quaternion startRot, Vector3 startScale, float scale, float distance, float duration)
    {
        if (arm == null) { EndAttack(); return; }

        float half = duration * 0.4f;
        float returnTime = duration * 0.6f;

        Vector3 flyTarget = (Vector3)(targetDir * distance);

        if (stateTimer < half)
        {
            // Летит к врагу
            float t = stateTimer / half;
            float e = t * t * t; // Быстрый удар

            arm.localPosition = Vector3.Lerp(startPos, startPos + flyTarget, e);
            arm.localScale = Vector3.Lerp(startScale, startScale * scale, e);

            // Поворот в сторону врага
            float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
            arm.localRotation = Quaternion.Euler(0, 0, angle);

            if (t > 0.7f && t < 0.8f)
            {
                OnHitFrame?.Invoke();
                ArenaCamera.Shake(0.3f, 0.1f);
            }
        }
        else if (stateTimer < half + returnTime)
        {
            // Возврат
            float t = (stateTimer - half) / returnTime;
            float e = 1f - (1f - t) * (1f - t);

            arm.localPosition = Vector3.Lerp(startPos + flyTarget, startPos, e);
            arm.localScale = Vector3.Lerp(startScale * scale, startScale, e);
            arm.localRotation = Quaternion.Lerp(arm.localRotation, startRot, e);
        }
        else EndAttack();
    }

    // ═══════════ HEAVY — ОБЕ РУКИ ЛЕТЯТ КАК РАКЕТЫ ═══════════
    void AnimateHeavy()
    {
        float total = heavyAnticipation + heavyStrike + heavyRecovery;
        Vector3 flyTarget = (Vector3)(targetDir * heavyFlyDistance);

        if (stateTimer < heavyAnticipation)
        {
            // Замах назад
            float t = stateTimer / heavyAnticipation;
            float e = 1f - (1f - t) * (1f - t);
            Vector3 pullBack = (Vector3)(-targetDir * 0.5f);

            if (leftArm != null) { leftArm.localPosition = Vector3.Lerp(leftArmStartPos, leftArmStartPos + pullBack, e); leftArm.localScale = Vector3.Lerp(leftArmStartScale, leftArmStartScale * heavyArmScale * 0.5f, e); }
            if (rightArm != null) { rightArm.localPosition = Vector3.Lerp(rightArmStartPos, rightArmStartPos + pullBack, e); rightArm.localScale = Vector3.Lerp(rightArmStartScale, rightArmStartScale * heavyArmScale * 0.5f, e); }

            // Тряска нарастает
            if (torso != null) torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 30f) * t * 3f);
        }
        else if (stateTimer < heavyAnticipation + heavyStrike)
        {
            // ЗАПУСК — руки летят к врагу
            float t = (stateTimer - heavyAnticipation) / heavyStrike;
            float e = t * t * t;

            float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

            if (leftArm != null)
            {
                leftArm.localPosition = Vector3.Lerp(leftArmStartPos + (Vector3)(-targetDir * 0.5f), leftArmStartPos + flyTarget + Vector3.up * 0.3f, e);
                leftArm.localScale = leftArmStartScale * Mathf.Lerp(heavyArmScale * 0.5f, heavyArmScale, e);
                leftArm.localRotation = Quaternion.Euler(0, 0, angle + Mathf.Sin(Time.time * 20f) * 10f);
            }
            if (rightArm != null)
            {
                rightArm.localPosition = Vector3.Lerp(rightArmStartPos + (Vector3)(-targetDir * 0.5f), rightArmStartPos + flyTarget - Vector3.up * 0.3f, e);
                rightArm.localScale = rightArmStartScale * Mathf.Lerp(heavyArmScale * 0.5f, heavyArmScale, e);
                rightArm.localRotation = Quaternion.Euler(0, 0, angle - Mathf.Sin(Time.time * 20f) * 10f);
            }

            if (t > 0.6f && t < 0.7f)
            {
                OnHitFrame?.Invoke();
                ArenaCamera.Shake(0.8f, 0.25f);
            }
        }
        else if (stateTimer < total)
        {
            // Возврат
            float t = (stateTimer - heavyAnticipation - heavyStrike) / heavyRecovery;
            float e = 1f - (1f - t) * (1f - t);
            if (leftArm != null) { leftArm.localPosition = Vector3.Lerp(leftArm.localPosition, leftArmStartPos, e); leftArm.localScale = Vector3.Lerp(leftArm.localScale, leftArmStartScale, e); leftArm.localRotation = Quaternion.Lerp(leftArm.localRotation, leftArmStartRot, e); }
            if (rightArm != null) { rightArm.localPosition = Vector3.Lerp(rightArm.localPosition, rightArmStartPos, e); rightArm.localScale = Vector3.Lerp(rightArm.localScale, rightArmStartScale, e); rightArm.localRotation = Quaternion.Lerp(rightArm.localRotation, rightArmStartRot, e); }
            if (torso != null) torso.localRotation = Quaternion.Lerp(torso.localRotation, torsoStartRot, e);
        }
        else EndAttack();
    }

    // ═══════════ ЗАРЯДКА БАРРАЖА ═══════════
    void AnimateCharge()
    {
        chargeTimer += Time.deltaTime;
        float intensity = Mathf.Clamp01(chargeTimer / barrageChargeTime);

        // Тряска нарастает
        float shake = Mathf.Sin(Time.time * 40f * intensity) * intensity * 5f;
        if (torso != null) torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, shake);

        // Руки сжимаются и трясутся
        float fist = Mathf.Sin(Time.time * 10f) * 3f * intensity;
        if (leftArm != null) leftArm.localRotation = leftArmStartRot * Quaternion.Euler(0, 0, 30f * intensity + fist);
        if (rightArm != null) rightArm.localRotation = rightArmStartRot * Quaternion.Euler(0, 0, -30f * intensity - fist);

        // Руки увеличиваются при зарядке
        float armGrow = 1f + intensity * 0.5f;
        if (leftArm != null) leftArm.localScale = leftArmStartScale * armGrow;
        if (rightArm != null) rightArm.localScale = rightArmStartScale * armGrow;

        // Круг через 2 сек
        if (chargeTimer >= barrageCircleAppearTime && chargeCircle == null)
            CreateChargeCircle();

        if (chargeCircleRenderer != null)
        {
            float circleT = Mathf.Clamp01((chargeTimer - barrageCircleAppearTime) / (barrageChargeTime - barrageCircleAppearTime));
            chargeCircleRenderer.color = Color.Lerp(new Color(1, 1, 1, 0.3f), new Color(1, 0, 0, 0.6f), circleT);
            float s = 0.5f + circleT * 1f + Mathf.Sin(Time.time * 6f) * 0.1f;
            chargeCircle.transform.localScale = Vector3.one * s;
        }
    }

    public void StartBarrageCharge()
    {
        if (IsBusy()) return;
        chargeTimer = 0f;
        isCharging = true;
        SetState(AnimState.BarrageCharging);
    }

    public void ReleaseBarrageCharge()
    {
        if (!isCharging) return;
        isCharging = false;

        if (chargeTimer >= barrageChargeTime)
        {
            SetState(AnimState.Barrage);
            StartCoroutine(BarrageRoutine());
        }
        else
        {
            DestroyChargeCircle();
            SetState(AnimState.Heavy);
        }
    }

    // ═══════════ БАРРАЖ — ШКВАЛ ЛЕТЯЩИХ РУК СО ВСЕХ СТОРОН ═══════════
    IEnumerator BarrageRoutine()
    {
        DestroyChargeCircle();

        // Скрываем оригинальные руки
        if (leftArm != null) leftArm.gameObject.SetActive(false);
        if (rightArm != null) rightArm.gameObject.SetActive(false);

        ArenaCamera.Shake(1.5f, barrageDuration);

        // Создаём клоны рук
        SpriteRenderer armSprite = rightArm != null ? rightArm.GetComponent<SpriteRenderer>() : null;
        Sprite armSpr = armSprite != null ? armSprite.sprite : null;
        int armOrder = armSprite != null ? armSprite.sortingOrder : 5;

        EnsureBarrageClones(armSpr, armOrder);
        ActivateBarrageClones(armSpr, armOrder);
        int activeCloneCount = Mathf.Min(barrageArmClones, barrageClones.Count);

        float elapsed = 0f;
        float hitTimer = 0f;

        while (elapsed < barrageDuration)
        {
            elapsed += Time.deltaTime;

            // Торс трясётся
            if (torso != null)
                torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 50f) * 5f);

            // Голова трясётся
            if (head != null)
                head.localPosition = headStartPos + (Vector3)(Random.insideUnitCircle * 0.02f);

            Vector3 targetPos = currentTarget != null ? currentTarget.position : transform.position + (Vector3)(targetDir * 3f);

            // Каждый клон летит к врагу с разных сторон
            for (int i = 0; i < activeCloneCount; i++)
            {
                GameObject clone = barrageClones[i];
                if (clone == null) continue;

                float phase = i * 1.3f + elapsed * 8f;
                float cycleT = Mathf.Repeat(phase, 1f);

                // Стартовая позиция — вокруг игрока
                float angle = (360f / activeCloneCount) * i + elapsed * 200f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 startPos = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * 1f;

                // Летит к врагу и обратно
                if (cycleT < 0.5f)
                {
                    float t = cycleT / 0.5f;
                    clone.transform.position = Vector3.Lerp(startPos, targetPos, t * t);
                    clone.transform.localScale = Vector3.one * Mathf.Lerp(barrageArmScale * 0.5f, barrageArmScale, t);
                }
                else
                {
                    float t = (cycleT - 0.5f) / 0.5f;
                    clone.transform.position = Vector3.Lerp(targetPos, startPos, t);
                    clone.transform.localScale = Vector3.one * Mathf.Lerp(barrageArmScale, barrageArmScale * 0.3f, t);
                }

                // Поворот к врагу
                Vector2 dir = (targetPos - clone.transform.position).normalized;
                float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                clone.transform.rotation = Quaternion.Euler(0, 0, a - 270);

                // Прозрачность
                SpriteRenderer sr = barrageCloneRenderers[i];
                if (sr != null) sr.color = new Color(1, 1, 1, 0.3f + Mathf.Sin(phase * 5f) * 0.3f);
            }

            // Удары
            hitTimer += Time.deltaTime;
            if (hitTimer >= barrageHitInterval)
            {
                hitTimer = 0f;
                OnBarrageHit?.Invoke(targetDir, barrageDamagePerHit);
            }

            yield return null;
        }

        // Очистка
        DeactivateBarrageClones();

        // Возвращаем оригинальные руки
        if (leftArm != null) { leftArm.gameObject.SetActive(true); leftArm.localPosition = leftArmStartPos; leftArm.localScale = leftArmStartScale; leftArm.localRotation = leftArmStartRot; }
        if (rightArm != null) { rightArm.gameObject.SetActive(true); rightArm.localPosition = rightArmStartPos; rightArm.localScale = rightArmStartScale; rightArm.localRotation = rightArmStartRot; }
        if (torso != null) torso.localRotation = torsoStartRot;
        if (head != null) head.localPosition = headStartPos;

        EndAttack();
    }

    // ═══════════ РЫВОК ═══════════
    void EnsureBarrageClones(Sprite armSprite, int armOrder)
    {
        Transform cloneParent = transform.parent != null ? transform.parent : transform;

        while (barrageClones.Count < barrageArmClones)
        {
            int index = barrageClones.Count;
            GameObject clone = new GameObject("BarrageArm" + index);
            clone.transform.SetParent(cloneParent);

            SpriteRenderer sr = clone.AddComponent<SpriteRenderer>();
            sr.sprite = armSprite;
            sr.color = new Color(1, 1, 1, 0.6f);
            sr.sortingOrder = armOrder + index;

            clone.SetActive(false);
            barrageClones.Add(clone);
            barrageCloneRenderers.Add(sr);
        }

        for (int i = 0; i < barrageClones.Count; i++)
        {
            GameObject clone = barrageClones[i];
            if (clone == null) continue;

            clone.transform.SetParent(cloneParent);

            SpriteRenderer sr = barrageCloneRenderers[i];
            if (sr == null)
            {
                sr = clone.GetComponent<SpriteRenderer>();
                if (sr == null) sr = clone.AddComponent<SpriteRenderer>();
                barrageCloneRenderers[i] = sr;
            }

            sr.sprite = armSprite;
            sr.sortingOrder = armOrder + i;
        }
    }

    void ActivateBarrageClones(Sprite armSprite, int armOrder)
    {
        for (int i = 0; i < barrageClones.Count; i++)
        {
            GameObject clone = barrageClones[i];
            if (clone == null) continue;

            bool active = i < barrageArmClones;
            clone.SetActive(active);
            if (!active) continue;

            SpriteRenderer sr = barrageCloneRenderers[i];
            if (sr != null)
            {
                sr.sprite = armSprite;
                sr.color = new Color(1, 1, 1, 0.6f);
                sr.sortingOrder = armOrder + i;
            }
        }
    }

    void DeactivateBarrageClones()
    {
        for (int i = 0; i < barrageClones.Count; i++)
        {
            if (barrageClones[i] != null)
            {
                barrageClones[i].SetActive(false);
            }
        }
    }

    void AnimateDash()
    {
        if (stateTimer < dashDuration)
        {
            float t = stateTimer / dashDuration;
            float pulse = Mathf.Sin(t * Mathf.PI);
            if (torso != null) torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, 20f * pulse * Mathf.Sign(targetDir.x));
            float legSwing = Mathf.Sin(t * 30f) * 30f;
            if (leftLeg != null) leftLeg.localRotation = leftLegStartRot * Quaternion.Euler(0, 0, legSwing);
            if (rightLeg != null) rightLeg.localRotation = rightLegStartRot * Quaternion.Euler(0, 0, -legSwing);
        }
        else EndAttack();
    }

    void AnimateBlock()
    {
        if (leftArm != null) { leftArm.localRotation = Quaternion.Lerp(leftArm.localRotation, leftArmStartRot * Quaternion.Euler(0, 0, blockArmAngle), Time.deltaTime * 15f); leftArm.localPosition = leftArmStartPos; leftArm.localScale = leftArmStartScale; }
        if (rightArm != null) { rightArm.localRotation = Quaternion.Lerp(rightArm.localRotation, rightArmStartRot * Quaternion.Euler(0, 0, -blockArmAngle), Time.deltaTime * 15f); rightArm.localPosition = rightArmStartPos; rightArm.localScale = rightArmStartScale; }
    }

    void AnimateRoll()
    {
        if (stateTimer < rollDuration) { float t = stateTimer / rollDuration; transform.localRotation = Quaternion.Euler(0, 0, -t * 360f); }
        else { transform.localRotation = Quaternion.identity; EndAttack(); }
    }

    void AnimateHit()
    {
        if (stateTimer < hitDuration) { float i = 1f - stateTimer / hitDuration; if (torso != null) torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, Mathf.Sin(stateTimer * 60f) * 8f * i); }
        else EndAttack();
    }

    void AnimateDeath()
    {
        if (stateTimer < deathDuration) { float t = 1f - (1f - stateTimer / deathDuration) * (1f - stateTimer / deathDuration); transform.localRotation = Quaternion.Euler(0, 0, 90f * t); }
    }

    void ResetArms()
    {
        if (leftArm != null) { leftArm.localPosition = leftArmStartPos; leftArm.localScale = leftArmStartScale; leftArm.localRotation = Quaternion.Lerp(leftArm.localRotation, leftArmStartRot * Quaternion.Euler(0, 0, Mathf.Sin(Time.time * idleBobSpeed * 0.8f) * idleArmSway), Time.deltaTime * 5f); }
        if (rightArm != null) { rightArm.localPosition = rightArmStartPos; rightArm.localScale = rightArmStartScale; rightArm.localRotation = Quaternion.Lerp(rightArm.localRotation, rightArmStartRot * Quaternion.Euler(0, 0, -Mathf.Sin(Time.time * idleBobSpeed * 0.8f) * idleArmSway), Time.deltaTime * 5f); }
    }

    void SmoothReturnLegs()
    {
        if (leftLeg != null) leftLeg.localRotation = Quaternion.Lerp(leftLeg.localRotation, leftLegStartRot, Time.deltaTime * 5f);
        if (rightLeg != null) rightLeg.localRotation = Quaternion.Lerp(rightLeg.localRotation, rightLegStartRot, Time.deltaTime * 5f);
    }

    void EndAttack()
    {
        ResetArms();
        if (torso != null) { torso.localRotation = torsoStartRot; torso.localScale = torsoStartScale; }
        transform.localRotation = Quaternion.identity;
        DestroyChargeCircle();
        OnAttackEnd?.Invoke(stateTimer);
        SetState(AnimState.Idle);
    }

    void CreateChargeCircle()
    {
        chargeCircle = new GameObject("ChargeCircle");
        chargeCircle.transform.SetParent(transform);
        chargeCircle.transform.localPosition = Vector3.zero;
        chargeCircleRenderer = chargeCircle.AddComponent<SpriteRenderer>();
        chargeCircleRenderer.sprite = circleSprite != null ? circleSprite : CreateCircleSprite(64);
        chargeCircleRenderer.color = new Color(1, 1, 1, 0.3f);
        chargeCircleRenderer.sortingOrder = 50;
    }

    void DestroyChargeCircle() { if (chargeCircle != null) Destroy(chargeCircle); chargeCircle = null; chargeCircleRenderer = null; }

    Sprite CreateCircleSprite(int r)
    {
        Texture2D t = new Texture2D(r, r, TextureFormat.RGBA32, false);
        float c = r / 2f;
        for (int y = 0; y < r; y++) for (int x = 0; x < r; x++) { float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c; t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d))); }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, r, r), new Vector2(0.5f, 0.5f), r);
    }

    // ═══════════ ПУБЛИЧНЫЕ ═══════════
    public bool IsBusy() => currentState >= AnimState.Jab && currentState <= AnimState.Barrage || currentState == AnimState.Hit || currentState == AnimState.Death || currentState == AnimState.BarrageCharging;
    public bool IsDead() => currentState == AnimState.Death;
    public bool IsBarraging() => currentState == AnimState.Barrage;
    public bool IsBlocking() => isBlocking;
    public AnimState CurrentState => currentState;

    public void SetMoving(bool m, bool run = false) { if (IsBusy() || isBlocking) return; SetState(m ? (run ? AnimState.Run : AnimState.Walk) : AnimState.Idle); }
    public void SetTarget(Transform t, Vector2 dir) { currentTarget = t; if (dir.magnitude > 0.1f) targetDir = dir.normalized; }
    public void SetFacing(float dir) { if (Mathf.Abs(dir) > 0.1f) targetDir = new Vector2(Mathf.Sign(dir), 0); }

    public void Jab() { if (!IsBusy() && !isBlocking) SetState(AnimState.Jab); }
    public void Cross() { if (!IsBusy() && !isBlocking) SetState(AnimState.Cross); }
    public void Uppercut() { if (!IsBusy() && !isBlocking) SetState(AnimState.Uppercut); }
    public void HeavyAttack() { if (!IsBusy() && !isBlocking) SetState(AnimState.Heavy); }
    public void Dash() { if (!IsBusy() && !isBlocking) SetState(AnimState.Dash); }
    public void Roll() { if (!IsBusy() && !isBlocking) SetState(AnimState.Roll); }
    public void StartBlock() { if (IsBusy()) return; isBlocking = true; SetState(AnimState.Block); }
    public void StopBlock() { isBlocking = false; if (currentState == AnimState.Block) SetState(AnimState.Idle); }
    public void TakeHit() { if (!IsDead()) SetState(AnimState.Hit); }
    public void Die() { SetState(AnimState.Death); }
}
