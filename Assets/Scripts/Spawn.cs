using UnityEngine;

public class Spawn : MonoBehaviour
{
    [SerializeField] GameObject creep;
    [SerializeField] float cooldown =3f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating(nameof(EnemyCreate), 5f, cooldown);
    }

    // Update is called once per frame
    void Update()
    {
    }
    void EnemyCreate()
    {
        var creature = Instantiate(creep);
        float x =Random.Range(-15f,15f);
        float y =Random.Range(-15f,15f);
        creature.transform.position = new Vector3(x,y,0);
    }
}
