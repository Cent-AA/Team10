using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float starthp;
    private float hp;
    static float xp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hp = starthp;
    }
    public void TakeDamage(float damage)
    {
        
        if (gameObject.CompareTag("Player"))
        {

            PlayerControl player = GetComponent<PlayerControl>();
            float roll =Random.value;
            if(roll < player.dodgeChance){
                //Debug.Log("dodged");
                return;
            }
            //Debug.Log("didnt dodge");
            float findamage = damage* (1f-player.damageAbsorption );
            //Debug.Log("took" + findamage);
            //Debug.Log("used to be" + hp);
            hp =Mathf.Clamp(hp -findamage,0,starthp);
            //Debug.Log("now" + hp);
        }
        else
        {
        hp =Mathf.Clamp(hp -damage ,0,starthp);
        }
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
    void Die()
    {
         //do something
        //Destroy(gameObject);
        if (gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
            Registry.Players.Remove(transform);
        }
        else
        {
            xp+=10;
            Debug.Log(xp);
            if(xp > 49)
            {
                //FindObjectOfType<Cards>().TestCard();
                FindAnyObjectByType<Cards>().TestCard();
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
