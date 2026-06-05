using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float starthp;
    private float hp;
    static float xp;

    void Start()
    {
        hp = starthp;
    }

    public void TakeDamage(float damage)
    {
        if (gameObject.CompareTag("Player"))
        {
            PlayerControl player = GetComponent<PlayerControl>();
            float roll = Random.value;
            
            // Если игрок увернулся — выходим, урон не наносится и полоска не дергается
            if (roll < player.dodgeChance) {
                return;
            }

            // Считаем чистый урон с учетом брони
            float findamage = damage * (1f - player.damageAbsorption);
            hp = Mathf.Clamp(hp - findamage, 0, starthp);

            // Тот самый мостик: отправляем ЧИСТЫЙ урон в нашу пиксельную полоску!
            if (UIBarsManager.Instance != null)
            {
                UIBarsManager.Instance.TakeDamage(findamage);
            }
        }
        else
        {
            // Урон для врагов (остается прежним)
            hp = Mathf.Clamp(hp - damage, 0, starthp);
        }

        if (hp > 0)
        {
            // do something
        }
        else
        {
            Debug.Log("death");
            Die();
        }
    }

    void Die()
    {
        if (gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
            Registry.Players.Remove(transform);
        }
        else
        {
            xp += 10;
            Debug.Log(xp);
            if (xp > 49)
            {
                FindAnyObjectByType<Cards>().TestCard();
            }
            Destroy(gameObject);
            Debug.Log("enemy down");
        }
    }
}