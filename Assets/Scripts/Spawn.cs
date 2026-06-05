using UnityEngine;

public class Spawn : MonoBehaviour
{
    [SerializeField] GameObject creep;
    [SerializeField] float cooldown =3f;
    private float ourcooldown;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ourcooldown =cooldown;
        InvokeRepeating(nameof(EnemyCreate), 5f, cooldown);
    }

    // Update is called once per frame
    void Update()
    {
        //if(ourcooldown > 0)
        //{
          //  ourcooldown =Time.deltaTime;
        //}
        //if(ourcooldown <= 0)
        //{
         //   EnemyCreate();
          //  ourcooldown =cooldown;
        //}
    }
    void EnemyCreate()
    {
        var creature = Instantiate(creep);
        float x =Random.Range(-15f,15f);
        float y =Random.Range(-15f,15f);
        creature.transform.position = new Vector3(x,y,0);
    }
}
