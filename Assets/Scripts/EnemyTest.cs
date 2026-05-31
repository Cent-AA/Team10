using UnityEngine;

public class EnemyTest : MonoBehaviour

{
    public Transform player1, player2;
    public Transform target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                if (players.Length >= 2)
        {
            player1 = players[0].transform;
            player2 = players[1].transform;
            }
        if (players.Length == 0) return;

        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                closest = player.transform;
            }
        }

        target = closest;

        // пример движения к цели
        if (target != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                3f * Time.deltaTime
            );
    }
    }
}
