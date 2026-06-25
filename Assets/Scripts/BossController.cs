using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("Stats")]
    public float moveSpeed = 2f;
    public float stopRange = 2.5f;
    public float maxHealth = 500f;

    [Header("Parts")]
    public Transform leftArm;
    public Transform rightArm;
    public Transform head;
    public Transform torso;
    public Transform legs;

    private Animator anim;
    private float currentHealth;
    private bool isActive = false;
    private int currentState = -1;
    private Transform currentTarget;

    void Start()
    {
        anim = GetComponent<Animator>();
        Debug.Log("Animator найден: " + (anim != null));
        if (anim != null)
        {
            // Проверяем что параметр существует
            foreach (var p in anim.parameters)
                Debug.Log("Параметр: " + p.name + " тип: " + p.type);
        }
        currentHealth = maxHealth;
        SetState(0);
    }

    public void Activate()
    {
        Debug.Log("Boss Activated!");
        isActive = true;
        StartCoroutine(ScreamThenWalk());
    }

    IEnumerator ScreamThenWalk()
    {
        SetState(2);
        yield return new WaitForSeconds(1.5f);
        SetState(1);
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
            SetState(1);

            float dirX = currentTarget.position.x - transform.position.x;
            transform.localScale = new Vector3(
                Mathf.Sign(dirX) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
        else
        {
            SetState(0);
        }
    }

    void SetState(int state)
    {
        if (currentState == state) return;
        currentState = state;
        if (anim != null)
        {
            Debug.Log("SetState: " + state);
            anim.SetInteger("State", state);
        }
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;
        currentHealth -= amount;
        ArenaCamera.Shake(0.15f, 0.1f);
        if (currentHealth <= 0f) Die();
    }

    void Die()
    {
        isActive = false;
        SetState(0);
        Destroy(gameObject, 2f);
    }

    Transform GetClosestPlayer()
    {
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (Transform p in Registry.Players)
        {
            if (p == null) continue;
            float d = Vector3.Distance(transform.position, p.position);
            if (d < minDist) { minDist = d; closest = p; }
        }
        return closest;
    }

    public float GetHealthPercent() => currentHealth / maxHealth;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopRange);
    }
}