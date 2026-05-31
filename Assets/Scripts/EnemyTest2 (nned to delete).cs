using UnityEngine;
public class EnemyTest2 : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        var players = Registry.Players;

        if (players.Count == 0)
            return; // players not spawned yet

        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (var p in players)
        {
            if (p == null) continue;

            float dist = (p.position - transform.position).sqrMagnitude;

            if (dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }

        target = closest;

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