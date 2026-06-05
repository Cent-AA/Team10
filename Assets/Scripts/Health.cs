using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float starthp;
    [SerializeField] private BG3PortraitHealthBar portraitHealthBar;

    private float hp;
    static float xp;

    void Awake()
    {
        hp = starthp;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateHealthBar();
    }

    public void TakeDamage(float damage, float attackerLuck = 0f)
    {
        float finalDamage = damage;

        PlayerControl player = GetPlayerControl();

        if (player != null)
        {
            float roll =Random.value;
            if(roll < player.dodgeChance){
                //Debug.Log("dodged");
                return;
            }
            //Debug.Log("didnt dodge");
            finalDamage = damage* (1f-player.damageAbsorption );
            //Debug.Log("took" + findamage);
            //Debug.Log("used to be" + hp);
            //Debug.Log("now" + hp);
        }

        hp =Mathf.Clamp(hp -finalDamage,0,starthp);
        UpdateHealthBar();
        //hp =Mathf.Clamp(hp -damage ,0,starthp);
        if(hp >0)
        {
            // do something
            //Debug.Log("took" + damage);
        }
        else
        {
            //die  death animation maybe delete object remove controls etc
            Debug.Log("death");
            Die();
        }
    }

    public void Heal(float amount)
    {
        hp = Mathf.Clamp(hp + amount, 0, starthp);
        UpdateHealthBar();
    }

    public void SetPortraitHealthBar(BG3PortraitHealthBar newPortraitHealthBar)
    {
        portraitHealthBar = newPortraitHealthBar;
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (portraitHealthBar != null)
        {
            portraitHealthBar.SetHealth(hp, starthp);
        }
    }

    PlayerControl GetPlayerControl()
    {
        PlayerControl player = GetComponent<PlayerControl>();
        if (player == null)
        {
            player = GetComponentInParent<PlayerControl>();
        }

        return player;
    }

    void Die()
    {
         //do something
        //Destroy(gameObject);
        if (GetPlayerControl() != null || gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
            Registry.Unregister(transform);
            Registry.Unregister(transform.root);
        }
        else
        {
            xp+=10;
            Debug.Log(xp);
            if(xp > 49)
            {
                //FindObjectOfType<Cards>().TestCard();
                Cards cards = FindAnyObjectByType<Cards>();
                if (cards != null)
                {
                    cards.TestCard();
                }
            }
            Destroy(gameObject);
            Debug.Log("enemy down");
        }
        

    }
    // Update is called once per frame
    void Update()
    {
    }
}
