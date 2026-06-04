using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float starthp;
    private float hp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hp = starthp;
    }
    public void TakeDamage(float damage)
    {
        hp =Mathf.Clamp(hp -damage ,0,starthp);
        if(hp >0)
        {
            // do something
            Debug.Log("took" + damage);
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
        Destroy(gameObject);
        if (gameObject.CompareTag("Player"))
        {
            Registry.Players.Remove(transform);
        }
        else
        {
            Debug.Log("enemy down");
        }
        

    }
    // Update is called once per frame
    void Update()
    {
    }
}
