using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    const int IdleState = 0;
    const int WalkState = 1;
    const int ScreamState = 2;

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

    private Animator anim;
    private float currentHealth;
    private bool isActive;
    private int currentState = -1;
    private Transform currentTarget;
    private Coroutine activationRoutine;
    private bool hasStateParameter;

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
    }

    public void Activate()
    {
        if (activationRoutine != null)
            StopCoroutine(activationRoutine);

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
        if (!isActive) return;

        currentTarget = GetClosestPlayer();
        if (currentTarget == null) return;

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        if (dist > stopRange)
        {
            Vector3 dir = (currentTarget.position - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            SetState(WalkState);

            float dirX = currentTarget.position.x - transform.position.x;
            if (!Mathf.Approximately(dirX, 0f))
            {
                transform.localScale = new Vector3(
                    Mathf.Sign(dirX) * Mathf.Abs(transform.localScale.x),
                    transform.localScale.y,
                    transform.localScale.z
                );
            }
        }
        else
        {
            SetState(IdleState);
        }
    }

    void StartChaseState()
    {
        isActive = true;
        SetState(WalkState);
    }

    void StopActivationRoutine()
    {
        if (activationRoutine == null) return;

        StopCoroutine(activationRoutine);
        activationRoutine = null;
    }

    void SetState(int state)
    {
        if (currentState == state) return;
        currentState = state;

        if (!hasStateParameter)
            return;

        anim.SetFloat("State", state);
    }

    bool CacheStateParameter()
    {
        if (anim == null) return false;

        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.name != "State")
                continue;

            if (parameter.type == AnimatorControllerParameterType.Float)
                return true;
        }

        return false;
    }

    void ValidateAnimator()
    {
        if (anim == null)
        {
            Debug.LogWarning($"{nameof(BossController)} on {name} has no Animator.", this);
            return;
        }

        hasStateParameter = CacheStateParameter();
        if (!hasStateParameter)
            Debug.LogWarning($"{nameof(BossController)} on {name} needs a float Animator parameter named State.", this);
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;

        currentHealth -= amount;
        ArenaCamera.Shake(0.15f, 0.1f);

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        StopActivationRoutine();
        isActive = false;
        SetState(IdleState);
        Destroy(gameObject, 2f);
    }

    Transform GetClosestPlayer()
    {
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (Transform player in Registry.Players)
        {
            if (player == null) continue;

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance < minDist)
            {
                minDist = distance;
                closest = player;
            }
        }

        return closest;
    }

    public float GetHealthPercent()
    {
        return maxHealth > 0f ? currentHealth / maxHealth : 0f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopRange);
    }
}
