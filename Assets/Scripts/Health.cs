using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float starthp;
    private float hp;
    static float xp;
<<<<<<< Updated upstream

=======
    static float xplvl;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
>>>>>>> Stashed changes
    void Start()
    {
        hp = starthp;
        
    }
<<<<<<< Updated upstream

    public void TakeDamage(float damage)
=======
    public void TakeDamage(float damage,float luck)
>>>>>>> Stashed changes
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
    
            Die(luck);
        }
    }
<<<<<<< Updated upstream

    void Die()
=======
    void Die(float luck)
>>>>>>> Stashed changes
    {
        if (gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
            Registry.Players.Remove(transform);
        }
        else
        {
<<<<<<< Updated upstream
            xp += 10;
            Debug.Log(xp);
            if (xp > 49)
=======
            
            //player
            xp+=luck;
            Debug.Log(xp);
            if(xp > 49 +(49 *xplvl))
>>>>>>> Stashed changes
            {
                FindAnyObjectByType<Cards>().TestCard();
                xp=0;
            }
            Destroy(gameObject);
            Debug.Log("enemy down");
        }
    }
}