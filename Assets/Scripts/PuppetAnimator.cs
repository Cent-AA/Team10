using UnityEngine;

public class PuppetAnimator : MonoBehaviour
{
    [Header("Части тела")]
    public Transform head;
    public Transform torso;
    public Transform leftArm;
    public Transform rightArm;
    public Transform leftLeg;
    public Transform rightLeg;

    [Header("Idle — дыхание")]
    public float idleBobSpeed = 2f;
    public float idleBobAmount = 0.02f;
    public float idleBreathAmount = 0.005f;
    public float idleArmSway = 2f;

    [Header("Ходьба")]
    public float walkLegAngle = 20f;
    public float walkLegSpeed = 8f;
    public float walkArmAngle = 15f;
    public float walkBodyTilt = 3f;
    public float walkBob = 0.03f;

    [Header("Атака")]
    public float attackArmAngle = -90f;
    public float attackDuration = 0.3f;
    public float attackReturnDuration = 0.2f;

    private bool isWalking = false;
    private bool isAttacking = false;
    private float attackTimer = 0f;
    private bool attackRight = true;

    // Сохраняем ВСЕ начальные значения
    private Vector3 headStartPos;
    private Vector3 torsoStartPos;
    private Vector3 torsoStartScale;
    private Quaternion leftArmStartRot;
    private Quaternion rightArmStartRot;
    private Quaternion leftLegStartRot;
    private Quaternion rightLegStartRot;
    private Quaternion torsoStartRot;

    void Start()
    {
        if (head != null) headStartPos = head.localPosition;
        if (torso != null)
        {
            torsoStartPos = torso.localPosition;
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
        if (isAttacking)
            AnimateAttack();
        else if (isWalking)
            AnimateWalk();
        else
            AnimateIdle();
    }

    void AnimateIdle()
    {
        float t = Time.time * idleBobSpeed;

        if (head != null)
            head.localPosition = headStartPos + Vector3.up * Mathf.Sin(t) * idleBobAmount;

        if (torso != null)
        {
            float breath = 1f + Mathf.Sin(t) * idleBreathAmount;
            torso.localScale = new Vector3(torsoStartScale.x, torsoStartScale.y * breath, torsoStartScale.z);
            torso.localRotation = torsoStartRot;
        }

        if (leftArm != null)
            leftArm.localRotation = leftArmStartRot * Quaternion.Euler(0, 0, Mathf.Sin(t * 0.8f) * idleArmSway);
        if (rightArm != null)
            rightArm.localRotation = rightArmStartRot * Quaternion.Euler(0, 0, -Mathf.Sin(t * 0.8f) * idleArmSway);

        if (leftLeg != null)
            leftLeg.localRotation = Quaternion.Lerp(leftLeg.localRotation, leftLegStartRot, Time.deltaTime * 5f);
        if (rightLeg != null)
            rightLeg.localRotation = Quaternion.Lerp(rightLeg.localRotation, rightLegStartRot, Time.deltaTime * 5f);
    }

    void AnimateWalk()
    {
        float t = Time.time * walkLegSpeed;

        if (leftLeg != null)
            leftLeg.localRotation = leftLegStartRot * Quaternion.Euler(0, 0, Mathf.Sin(t) * walkLegAngle);
        if (rightLeg != null)
            rightLeg.localRotation = rightLegStartRot * Quaternion.Euler(0, 0, -Mathf.Sin(t) * walkLegAngle);

        if (leftArm != null)
            leftArm.localRotation = leftArmStartRot * Quaternion.Euler(0, 0, -Mathf.Sin(t) * walkArmAngle);
        if (rightArm != null)
            rightArm.localRotation = rightArmStartRot * Quaternion.Euler(0, 0, Mathf.Sin(t) * walkArmAngle);

        if (torso != null)
        {
            torso.localRotation = torsoStartRot * Quaternion.Euler(0, 0, Mathf.Sin(t) * walkBodyTilt);
            torso.localScale = torsoStartScale;
        }

        if (head != null)
            head.localPosition = headStartPos + Vector3.up * Mathf.Abs(Mathf.Sin(t * 2f)) * walkBob;
    }

    void AnimateAttack()
    {
        attackTimer += Time.deltaTime;
        float totalDuration = attackDuration + attackReturnDuration;

        Transform arm = attackRight ? rightArm : leftArm;
        Quaternion startRot = attackRight ? rightArmStartRot : leftArmStartRot;
        float direction = attackRight ? -1f : 1f;

        if (arm != null)
        {
            if (attackTimer < attackDuration)
            {
                float t = attackTimer / attackDuration;
                arm.localRotation = startRot * Quaternion.Euler(0, 0, attackArmAngle * direction * t * t);
            }
            else if (attackTimer < totalDuration)
            {
                float t = (attackTimer - attackDuration) / attackReturnDuration;
                float eased = 1f - (1f - t) * (1f - t);
                arm.localRotation = Quaternion.Lerp(
                    startRot * Quaternion.Euler(0, 0, attackArmAngle * direction),
                    startRot, eased);
            }
            else
            {
                arm.localRotation = startRot;
                isAttacking = false;
            }
        }
    }

    public void SetWalking(bool walking)
    {
        isWalking = walking;
    }

    public void Attack(bool rightHand = true)
    {
        if (isAttacking) return;
        isAttacking = true;
        attackTimer = 0f;
        attackRight = rightHand;
    }
}