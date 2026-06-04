using UnityEngine;

public class EnemyTest3ThisIsForTestingDontUseInFinishedGame : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
[SerializeField] float speed ;
Rigidbody2D rb;
Transform target;
[SerializeField] private float damage;
[SerializeField] float attackCooldown ;
float nextAttackTime;

void Start()
{
    rb = GetComponent<Rigidbody2D>();
}
//private void OnTriggerStay2D(Collider2D other)
//{
//    Health player = other.GetComponent<Health>();

 //   if (player != null && Time.time >= nextAttackTime)
 //   {
 //       player.TakeDamage(damage);
 //       nextAttackTime = Time.time + attackCooldown;
 //   }
//}
//private void OnCollisionEnter2D(Collision2D collision)
//{
 //   if (!collision.gameObject.CompareTag("Player")) return;
//
 //   Health player = collision.gameObject.GetComponentInParent<Health>();
//
 //   if (player != null && Time.time >= nextAttackTime)
 //   {
 //       player.TakeDamage(damage);
 //       nextAttackTime = Time.time + attackCooldown;
 //   }
//}
private void OnTriggerStay2D(Collider2D other)
{
    if (!other.CompareTag("Player")) return;

    Health player = other.GetComponentInParent<Health>();

    if (player != null && Time.time >= nextAttackTime)
    {
        player.TakeDamage(damage);
        nextAttackTime = Time.time + attackCooldown;
    }
}
void FixedUpdate()
{
    GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

    if (players.Length == 0) return;

    Transform closest = null;
    float minDist = Mathf.Infinity;

    foreach (GameObject p in players)
    {
        float dist = Vector2.Distance(rb.position, p.transform.position);

        if (dist < minDist)
        {
            minDist = dist;
            closest = p.transform;
        }
    }

    target = closest;

    Vector2 newPos = Vector2.MoveTowards(
        rb.position,
        target.position,
        speed * Time.fixedDeltaTime
    );

    rb.MovePosition(newPos);
}
}
