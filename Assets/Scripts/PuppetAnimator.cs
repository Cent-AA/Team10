using UnityEngine;
using System.Collections;

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
    public float idleHeadTurn = 1.5f;

    [Header("═══ Ходьба ═══")]
    public float walkLegAngle = 25f;
    public float walkLegSpeed = 10f;
    public float walkArmAngle = 20f;
    public float walkBodyTilt = 4f;
    public float walkBob = 0.04f;

    [Header("═══ Бег ═══")]
    public float runLegAngle = 50f;
    public float runLegSpeed = 18f;
    public float runArmAngle = 40f;
    public float runBodyTilt = 10f;
    public float runBob = 0.1f;
    public float runLean = 8f;

    [Header("═══ Лёгкая атака (правый джеб) ═══")]
    public float jabAnticipation = 0.08f;
    public float jabStrike = 0.1f;
    public float jabRecovery = 0.2f;
    public float jabAngle = -110f;
    public float jabSquash = 1.1f;

    [Header("═══ Комбо (2-й удар, левый кросс) ═══")]
    public float crossAngle = -120f;
    public float crossLean = 8f;

    [Header("═══ Финальный удар комбо (3-й, апперкот) ═══")]
    public float uppercutAnticipation = 0.15f;
    public float uppercutStrike = 0.15f;
    public float uppercutRecovery = 0.35f;
    public float uppercutAngle = -150f;
    public float uppercutLift = 0.15f;

    [Header("═══ Тяжёлый удар (двумя руками сверху) ═══")]
    public float heavyAnticipation = 0.4f;
    public float heavyStrike = 0.12f;
    public float heavyRecovery = 0.5f;
    public float heavyAngle = -140f;
    public float heavySquash = 0.85f;

    [Header("═══ Спин-атака ═══")]
    public float spinDuration = 0.7f;
    public int spinRotations = 2;
    public float spinArmExtend = 80f;

    [Header("═══ Рывок ═══")]
    public float dashDuration = 0.25f;
    public float dashLean = 25f;
    public float dashArmForward = -75f;
    public float dashStretch = 1.15f;

    [Header("═══ Блок ═══")]
    public float blockArmAngle = 60f;
    public float blockSpeed = 15f;

    [Header("═══ Уклонение/кувырок ═══")]
    public float rollDuration = 0.5f;
    public int rollRotations = 1;

    [Header("═══ Получение урона ═══")]
    public float hitDuration = 0.25f;
    public float hitShake = 8f;
    public float hitFlashDuration = 0.1f;

    [Header("═══ Смерть ═══")]
    public float deathDuration = 1f;
    public float deathFallAngle = 90f;

    [Header("═══ Squash & Stretch ═══")]
    public bool useSquashStretch = true;
    public float squashSpeed = 12f;

    public enum AnimState
    {
        Idle, Walk, Run,
        Jab, Cross, Uppercut, Heavy,
        Spin, Dash, Block, Roll,
        Hit, Death
    }

    private AnimState currentState = AnimState.Idle;
    private float stateTimer = 0f;
    private bool isBlocking = false;
    private float facingDir = 1f;  // 1 = вправо, -1 = влево

    // Начальные трансформации
    private Vector3 headStartPos;
    private Vector3 torsoStartScale;
    private Quaternion torsoStartRot;
    private Quaternion leftArmStartRot, rightArmStartRot;
    private Quaternion leftLegStartRot, rightLegStartRot;

    // Текущий squash/stretch
    private Vector3 currentSquash = Vector3.one;
    private Vector3 targetSquash = Vector3.one;

    // События для эффектов
    public System.Action OnHitFrame;       // Момент удара (для камеры/эффектов)
    public System.Action<float> OnAttackEnd;

    void Start()
    {
        CacheStartTransforms();
    }

    void CacheStartTransforms()
    {
        if (head != null) headStartPos = head.localPosition;
        if (torso != null)
        {
            torsoStartScale = torso.localScale;
            torsoStartRot = torso.localRotation;
        }
        if (leftArm != null) leftArmStartRot = leftArm.localRotation;
        if (rightArm != null) rightArmStartRot = rightArm.localRotation;
        if (leftLeg != null) leftLegStartRot = leftLeg.localRotation;
        if (rightLeg != null) rightLegStartRot = rightLeg.localRotation;
    }

    void Update()
    {
        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case AnimState.Idle: AnimateIdle(); break;
            case AnimState.Walk: AnimateLocomotion(false); break;
            case AnimState.Run: AnimateLocomotion(true); break;
            case AnimState.Jab: AnimateJab(); break;
            case AnimState.Cross: AnimateCross(); break;
            case AnimState.Uppercut: AnimateUppercut(); break;
            case AnimState.Heavy: AnimateHeavy(); break;
            case AnimState.Spin: AnimateSpin(); break;
            case AnimState.Dash: AnimateDash(); break;
            case AnimState.Block: AnimateBlock(); break;
            case AnimState.Roll: AnimateRoll(); break;
            case AnimState.Hit: AnimateHit(); break;
            case AnimState.Death: AnimateDeath(); break;
        }

        // Плавный squash & stretch
        if (useSquashStretch && torso != null)
        {
            currentSquash = Vector3.Lerp(currentSquash, targetSquash, Time.deltaTime * squashSpeed);
            targetSquash = Vector3.Lerp(targetSquash, Vector3.one, Time.deltaTime * squashSpeed * 0.5f);
        }
    }

    void SetState(AnimState newState)
    {
        if (currentState == newState && newState != AnimState.Idle) return;
        currentState = newState;
        stateTimer = 0f;
    }

    // ═══════════ IDLE ═══════════
    void AnimateIdle()
    {
        float t = Time.time * idleBobSpeed;
        float bob = Mathf.Sin(t);
        float headSway = Mathf.Sin(t * 0.5f);

        if (head != null)
        {
            head.localPosition = headStartPos + Vector3.up * bob * idleBobAmount;
            head.localRotation = Quaternion.Euler(0, 0, headSway * idleHeadTurn);
        }

        if (torso != null)
        {
            float breath = 1f + bob * idleBreathAmount;
            torso.localScale = new Vector3(
                torsoStartScale.x * currentSquash.x,
                torsoStartScale.y * breath * currentSquash.y,
                torsoStartScale.z);
            torso.localRotation = torsoStartRot;
        }

        if (leftArm != null)
            leftArm.localRotation = leftArmStartRot * Quaternion.Euler(0, 0, Mathf.Sin(t * 0.8f) * idleArmSway);
        if (rightArm != null)
            rightArm.localRotation = rightArmStartRot * Quaternion.Euler(0, 0, -Mathf.Sin(t * 0.8f) * idleArmSway);

        SmoothReturnLegs();
    }

    // ═══════════ ХОДЬБА / БЕГ ═══════════
    void AnimateLocomotion(bool running)
    {
        float speed = running ? runLegSpeed : walkLegSpeed;
        float legAngle = running ? runLegAngle : walkLegAngle;
        float armAngle = running ? runArmAngle : walkArmAngle;
        float tilt = running ? runBodyTilt : walkBodyTilt;
        float bob = running ? runBob : walkBob;
        float lean = running ? runLean : 0f;

        float t = Time.time * speed;
        float legSin = Mathf.Sin(t);

        if (leftLeg != null)
            leftLeg.localRotation = leftLegStartRot * Quaternion.Euler(0, 0, legSin * legAngle);
        if (rightLeg != null)
            rightLeg.localRotation = rightLegStartRot * Quaternion.Euler(0, 0, -legSin * legAngle);

        if (leftArm != null)
            leftArm.localRotation = leftArmStartRot * Quaternion.Euler(0, 0, -legSin * armAngle);
        if (rightArm != null)
            rightArm.localRotation = rightArmStartRot * Quaternion.Euler(0, 0, legSin * armAngle);

        if (torso != null)
        {
            torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, legSin * tilt - lean * facingDir);
            torso.localScale = Vector3.Scale(torsoStartScale, currentSquash);
        }

        if (head != null)
        {
            head.localPosition = headStartPos + Vector3.up * Mathf.Abs(Mathf.Sin(t * 2f)) * bob;
            head.localRotation = Quaternion.Euler(0, 0, -lean * 0.5f * facingDir);
        }
    }

    // ═══════════ ДЖЕБ (быстрый удар правой) ═══════════
    void AnimateJab()
    {
        float total = jabAnticipation + jabStrike + jabRecovery;

        if (rightArm == null) { EndAttack(); return; }

        if (stateTimer < jabAnticipation)
        {
            // Anticipation — рука назад
            float t = stateTimer / jabAnticipation;
            rightArm.localRotation = rightArmStartRot * Quaternion.Euler(0, 0, 25f * t);
            if (torso != null)
                torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, 5f * facingDir * t);
        }
        else if (stateTimer < jabAnticipation + jabStrike)
        {
            // Strike — резкий выпад
            float t = (stateTimer - jabAnticipation) / jabStrike;
            float eased = t * t;
            rightArm.localRotation = Quaternion.Lerp(
                rightArmStartRot * Quaternion.Euler(0, 0, 25f),
                rightArmStartRot * Quaternion.Euler(0, 0, -jabAngle * facingDir),
                eased);

            if (torso != null)
                torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, Mathf.Lerp(5f, -8f, eased) * facingDir);

            // Squash & stretch в момент удара
            if (t > 0.5f && t < 0.6f)
            {
                targetSquash = new Vector3(0.95f, jabSquash, 1f);
                OnHitFrame?.Invoke();
            }
        }
        else if (stateTimer < total)
        {
            // Recovery — плавный возврат
            float t = (stateTimer - jabAnticipation - jabStrike) / jabRecovery;
            float eased = 1f - (1f - t) * (1f - t);
            rightArm.localRotation = Quaternion.Lerp(
                rightArmStartRot * Quaternion.Euler(0, 0, -jabAngle * facingDir),
                rightArmStartRot, eased);
            if (torso != null)
                torso.localRotation = Quaternion.Lerp(
                    torsoStartRot * Quaternion.Euler(0, 0, -8f * facingDir),
                    torsoStartRot, eased);
        }
        else EndAttack();
    }

    // ═══════════ КРОСС (левый, 2-й удар комбо) ═══════════
    void AnimateCross()
    {
        float total = jabAnticipation + jabStrike + jabRecovery;

        if (leftArm == null) { EndAttack(); return; }

        if (stateTimer < jabAnticipation)
        {
            float t = stateTimer / jabAnticipation;
            leftArm.localRotation = leftArmStartRot * Quaternion.Euler(0, 0, -25f * t);
            if (torso != null)
                torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, -crossLean * facingDir * t);
        }
        else if (stateTimer < jabAnticipation + jabStrike)
        {
            float t = (stateTimer - jabAnticipation) / jabStrike;
            float eased = t * t;
            leftArm.localRotation = Quaternion.Lerp(
                leftArmStartRot * Quaternion.Euler(0, 0, -25f),
                leftArmStartRot * Quaternion.Euler(0, 0, crossAngle * facingDir),
                eased);
            if (torso != null)
                torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0,
                    Mathf.Lerp(-crossLean, crossLean, eased) * facingDir);

            if (t > 0.5f && t < 0.6f)
            {
                targetSquash = new Vector3(0.93f, 1.12f, 1f);
                OnHitFrame?.Invoke();
            }
        }
        else if (stateTimer < total)
        {
            float t = (stateTimer - jabAnticipation - jabStrike) / jabRecovery;
            float eased = 1f - (1f - t) * (1f - t);
            leftArm.localRotation = Quaternion.Lerp(
                leftArmStartRot * Quaternion.Euler(0, 0, crossAngle * facingDir),
                leftArmStartRot, eased);
            if (torso != null)
                torso.localRotation = Quaternion.Lerp(
                    torsoStartRot * Quaternion.Euler(0, 0, crossLean * facingDir),
                    torsoStartRot, eased);
        }
        else EndAttack();
    }

    // ═══════════ АППЕРКОТ (3-й удар комбо, мощный финиш) ═══════════
    void AnimateUppercut()
    {
        float total = uppercutAnticipation + uppercutStrike + uppercutRecovery;

        if (rightArm == null) { EndAttack(); return; }

        if (stateTimer < uppercutAnticipation)
        {
            // Глубокий замах вниз и в сторону
            float t = stateTimer / uppercutAnticipation;
            float eased = 1f - (1f - t) * (1f - t);
            rightArm.localRotation = rightArmStartRot * Quaternion.Euler(0, 0, 60f * eased);
            if (torso != null)
            {
                torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, 15f * facingDir * eased);
                torso.localScale = Vector3.Lerp(torsoStartScale,
                    new Vector3(torsoStartScale.x * 1.05f, torsoStartScale.y * 0.92f, torsoStartScale.z), eased);
            }
        }
        else if (stateTimer < uppercutAnticipation + uppercutStrike)
        {
            // Мощный апперкот снизу вверх
            float t = (stateTimer - uppercutAnticipation) / uppercutStrike;
            float eased = t * t;
            rightArm.localRotation = Quaternion.Lerp(
                rightArmStartRot * Quaternion.Euler(0, 0, 60f),
                rightArmStartRot * Quaternion.Euler(0, 0, -uppercutAngle * facingDir),
                eased);

            if (torso != null)
            {
                torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0,
                    Mathf.Lerp(15f, -20f, eased) * facingDir);
                torso.localScale = Vector3.Lerp(
                    new Vector3(torsoStartScale.x * 1.05f, torsoStartScale.y * 0.92f, torsoStartScale.z),
                    new Vector3(torsoStartScale.x * 0.95f, torsoStartScale.y * 1.15f, torsoStartScale.z),
                    eased);
            }

            if (head != null)
                head.localPosition = headStartPos + Vector3.up * uppercutLift * eased;

            if (t > 0.6f && t < 0.7f)
            {
                targetSquash = new Vector3(0.85f, 1.25f, 1f);
                OnHitFrame?.Invoke();
            }
        }
        else if (stateTimer < total)
        {
            float t = (stateTimer - uppercutAnticipation - uppercutStrike) / uppercutRecovery;
            float eased = 1f - (1f - t) * (1f - t);
            rightArm.localRotation = Quaternion.Lerp(
                rightArmStartRot * Quaternion.Euler(0, 0, -uppercutAngle * facingDir),
                rightArmStartRot, eased);
            if (torso != null)
            {
                torso.localRotation = Quaternion.Lerp(
                    torsoStartRot * Quaternion.Euler(0, 0, -20f * facingDir),
                    torsoStartRot, eased);
                torso.localScale = Vector3.Lerp(
                    new Vector3(torsoStartScale.x * 0.95f, torsoStartScale.y * 1.15f, torsoStartScale.z),
                    torsoStartScale, eased);
            }
            if (head != null)
                head.localPosition = Vector3.Lerp(
                    headStartPos + Vector3.up * uppercutLift, headStartPos, eased);
        }
        else EndAttack();
    }

    // ═══════════ ТЯЖЁЛЫЙ УДАР (двумя руками сверху) ═══════════
    void AnimateHeavy()
    {
        float total = heavyAnticipation + heavyStrike + heavyRecovery;

        if (leftArm == null || rightArm == null) { EndAttack(); return; }

        if (stateTimer < heavyAnticipation)
        {
            // Долгий замах — руки вверх и назад
            float t = stateTimer / heavyAnticipation;
            float eased = 1f - (1f - t) * (1f - t);
            leftArm.localRotation = leftArmStartRot * Quaternion.Euler(0, 0, 130f * eased);
            rightArm.localRotation = rightArmStartRot * Quaternion.Euler(0, 0, -130f * eased);

            if (torso != null)
            {
                torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, -10f * eased);
                torso.localScale = Vector3.Lerp(torsoStartScale,
                    new Vector3(torsoStartScale.x * 0.95f, torsoStartScale.y * heavySquash, torsoStartScale.z),
                    eased);
            }
            if (head != null)
                head.localPosition = headStartPos + Vector3.up * 0.08f * eased;
        }
        else if (stateTimer < heavyAnticipation + heavyStrike)
        {
            // МОЩНЫЙ удар вниз
            float t = (stateTimer - heavyAnticipation) / heavyStrike;
            float eased = t * t * t;  // Очень быстрый
            leftArm.localRotation = Quaternion.Lerp(
                leftArmStartRot * Quaternion.Euler(0, 0, 130f),
                leftArmStartRot * Quaternion.Euler(0, 0, -heavyAngle),
                eased);
            rightArm.localRotation = Quaternion.Lerp(
                rightArmStartRot * Quaternion.Euler(0, 0, -130f),
                rightArmStartRot * Quaternion.Euler(0, 0, heavyAngle),
                eased);

            if (torso != null)
            {
                torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, Mathf.Lerp(-10f, 8f, eased));
                torso.localScale = Vector3.Lerp(
                    new Vector3(torsoStartScale.x * 0.95f, torsoStartScale.y * heavySquash, torsoStartScale.z),
                    new Vector3(torsoStartScale.x * 1.15f, torsoStartScale.y * 0.85f, torsoStartScale.z),
                    eased);
            }
            if (head != null)
                head.localPosition = Vector3.Lerp(
                    headStartPos + Vector3.up * 0.08f,
                    headStartPos - Vector3.up * 0.05f, eased);

            if (t > 0.8f && t < 0.9f)
            {
                targetSquash = new Vector3(1.3f, 0.7f, 1f);
                OnHitFrame?.Invoke();
            }
        }
        else if (stateTimer < total)
        {
            float t = (stateTimer - heavyAnticipation - heavyStrike) / heavyRecovery;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            leftArm.localRotation = Quaternion.Lerp(
                leftArmStartRot * Quaternion.Euler(0, 0, -heavyAngle),
                leftArmStartRot, eased);
            rightArm.localRotation = Quaternion.Lerp(
                rightArmStartRot * Quaternion.Euler(0, 0, heavyAngle),
                rightArmStartRot, eased);
            if (torso != null)
            {
                torso.localRotation = Quaternion.Lerp(
                    torsoStartRot * Quaternion.Euler(0, 0, 8f), torsoStartRot, eased);
                torso.localScale = Vector3.Lerp(
                    new Vector3(torsoStartScale.x * 1.15f, torsoStartScale.y * 0.85f, torsoStartScale.z),
                    torsoStartScale, eased);
            }
            if (head != null)
                head.localPosition = Vector3.Lerp(
                    headStartPos - Vector3.up * 0.05f, headStartPos, eased);
        }
        else EndAttack();
    }

    // ═══════════ СПИН-АТАКА ═══════════
    void AnimateSpin()
    {
        if (stateTimer < spinDuration)
        {
            float t = stateTimer / spinDuration;
            float spinAngle = -t * 360f * spinRotations * facingDir;
            transform.localRotation = Quaternion.Euler(0, 0, spinAngle);

            if (leftArm != null)
                leftArm.localRotation = leftArmStartRot * Quaternion.Euler(0, 0, spinArmExtend);
            if (rightArm != null)
                rightArm.localRotation = rightArmStartRot * Quaternion.Euler(0, 0, -spinArmExtend);

            if (torso != null)
                torso.localScale = Vector3.Scale(torsoStartScale,
                    new Vector3(1f + Mathf.Sin(t * Mathf.PI) * 0.05f, 1f, 1f));

            // Тригер хитфреймов каждые 90 градусов
            float prevAngle = -(stateTimer - Time.deltaTime) / spinDuration * 360f * spinRotations;
            if (Mathf.FloorToInt(prevAngle / 90f) != Mathf.FloorToInt(spinAngle / 90f))
                OnHitFrame?.Invoke();
        }
        else
        {
            transform.localRotation = Quaternion.identity;
            EndAttack();
        }
    }

    // ═══════════ РЫВОК ═══════════
    void AnimateDash()
    {
        if (stateTimer < dashDuration)
        {
            float t = stateTimer / dashDuration;
            float pulse = Mathf.Sin(t * Mathf.PI);

            if (torso != null)
            {
                torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, dashLean * pulse * facingDir);
                torso.localScale = Vector3.Lerp(torsoStartScale,
                    new Vector3(torsoStartScale.x * dashStretch, torsoStartScale.y / dashStretch, torsoStartScale.z),
                    pulse);
            }

            if (rightArm != null)
                rightArm.localRotation = rightArmStartRot * Quaternion.Euler(0, 0, dashArmForward * pulse * facingDir);

            if (head != null)
                head.localPosition = headStartPos + Vector3.up * 0.03f * pulse;

            // Размытие ног — быстрое мерцание
            float legSwing = Mathf.Sin(t * 30f) * 30f;
            if (leftLeg != null)
                leftLeg.localRotation = leftLegStartRot * Quaternion.Euler(0, 0, legSwing);
            if (rightLeg != null)
                rightLeg.localRotation = rightLegStartRot * Quaternion.Euler(0, 0, -legSwing);
        }
        else EndAttack();
    }

    // ═══════════ БЛОК ═══════════
    void AnimateBlock()
    {
        // Руки скрещены перед собой
        if (leftArm != null)
            leftArm.localRotation = Quaternion.Lerp(leftArm.localRotation,
                leftArmStartRot * Quaternion.Euler(0, 0, blockArmAngle), Time.deltaTime * blockSpeed);
        if (rightArm != null)
            rightArm.localRotation = Quaternion.Lerp(rightArm.localRotation,
                rightArmStartRot * Quaternion.Euler(0, 0, -blockArmAngle), Time.deltaTime * blockSpeed);
        if (torso != null)
            torso.localRotation = Quaternion.Lerp(torso.localRotation,
                torsoStartRot * Quaternion.Euler(0, 0, -3f * facingDir), Time.deltaTime * blockSpeed);
    }

    // ═══════════ КУВЫРОК ═══════════
    void AnimateRoll()
    {
        if (stateTimer < rollDuration)
        {
            float t = stateTimer / rollDuration;
            float rollAngle = -t * 360f * rollRotations * facingDir;
            transform.localRotation = Quaternion.Euler(0, 0, rollAngle);

            if (torso != null)
            {
                float squash = 1f - Mathf.Sin(t * Mathf.PI) * 0.2f;
                torso.localScale = new Vector3(
                    torsoStartScale.x * (2f - squash),
                    torsoStartScale.y * squash,
                    torsoStartScale.z);
            }
        }
        else
        {
            transform.localRotation = Quaternion.identity;
            EndAttack();
        }
    }

    // ═══════════ ПОЛУЧЕНИЕ УРОНА ═══════════
    void AnimateHit()
    {
        if (stateTimer < hitDuration)
        {
            float t = stateTimer / hitDuration;
            float intensity = 1f - t;
            float shake = Mathf.Sin(stateTimer * 60f) * hitShake * intensity;

            if (torso != null)
            {
                torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, shake);
                torso.localScale = Vector3.Lerp(
                    new Vector3(torsoStartScale.x * 1.1f, torsoStartScale.y * 0.9f, torsoStartScale.z),
                    torsoStartScale, t);
            }
            if (head != null)
                head.localPosition = headStartPos + Vector3.right * shake * 0.005f;
        }
        else EndAttack();
    }

    // ═══════════ СМЕРТЬ ═══════════
    void AnimateDeath()
    {
        if (stateTimer < deathDuration)
        {
            float t = stateTimer / deathDuration;
            float eased = 1f - (1f - t) * (1f - t);

            // Падение
            transform.localRotation = Quaternion.Euler(0, 0, deathFallAngle * facingDir * eased);

            if (torso != null)
                torso.localScale = Vector3.Lerp(torsoStartScale,
                    new Vector3(torsoStartScale.x, torsoStartScale.y * 0.5f, torsoStartScale.z), eased);
        }
        // Остаётся лежать
    }

    // ═══════════ ВСПОМОГАТЕЛЬНЫЕ ═══════════
    void EndAttack()
    {
        if (torso != null)
        {
            torso.localRotation = torsoStartRot;
            torso.localScale = Vector3.Scale(torsoStartScale, currentSquash);
        }
        if (leftArm != null) leftArm.localRotation = leftArmStartRot;
        if (rightArm != null) rightArm.localRotation = rightArmStartRot;
        transform.localRotation = Quaternion.identity;
        OnAttackEnd?.Invoke(stateTimer);
        SetState(AnimState.Idle);
    }

    void SmoothReturnLegs()
    {
        if (leftLeg != null)
            leftLeg.localRotation = Quaternion.Lerp(leftLeg.localRotation, leftLegStartRot, Time.deltaTime * 5f);
        if (rightLeg != null)
            rightLeg.localRotation = Quaternion.Lerp(rightLeg.localRotation, rightLegStartRot, Time.deltaTime * 5f);
    }

    public bool IsBusy()
    {
        return currentState == AnimState.Jab || currentState == AnimState.Cross ||
               currentState == AnimState.Uppercut || currentState == AnimState.Heavy ||
               currentState == AnimState.Spin || currentState == AnimState.Dash ||
               currentState == AnimState.Roll || currentState == AnimState.Hit ||
               currentState == AnimState.Death;
    }

    public bool IsDead() { return currentState == AnimState.Death; }
    public AnimState CurrentState => currentState;

    // ═══════════ ПУБЛИЧНЫЕ МЕТОДЫ ═══════════
    public void SetMoving(bool moving, bool running = false)
    {
        if (IsBusy() || isBlocking) return;
        SetState(moving ? (running ? AnimState.Run : AnimState.Walk) : AnimState.Idle);
    }

    public void SetFacing(float dir)
    {
        if (Mathf.Abs(dir) > 0.1f)
            facingDir = Mathf.Sign(dir);
    }

    public void Jab() { if (!IsBusy() && !isBlocking) SetState(AnimState.Jab); }
    public void Cross() { if (!IsBusy() && !isBlocking) SetState(AnimState.Cross); }
    public void Uppercut() { if (!IsBusy() && !isBlocking) SetState(AnimState.Uppercut); }
    public void HeavyAttack() { if (!IsBusy() && !isBlocking) SetState(AnimState.Heavy); }
    public void SpinAttack() { if (!IsBusy() && !isBlocking) SetState(AnimState.Spin); }
    public void Dash() { if (!IsBusy() && !isBlocking) SetState(AnimState.Dash); }
    public void Roll() { if (!IsBusy() && !isBlocking) SetState(AnimState.Roll); }

    public void StartBlock()
    {
        if (IsBusy()) return;
        isBlocking = true;
        SetState(AnimState.Block);
    }
    public void StopBlock()
    {
        isBlocking = false;
        if (currentState == AnimState.Block) SetState(AnimState.Idle);
    }
    public bool IsBlocking() => isBlocking;

    public void TakeHit()
    {
        if (!IsDead()) SetState(AnimState.Hit);
    }

    public void Die()
    {
        SetState(AnimState.Death);
    }
}