using System.Collections.Generic;
using UnityEngine;

public class Registry : MonoBehaviour
{
    public static List<Transform> Players = new List<Transform>();
    public static List<ZombieAI> Zombies = new List<ZombieAI>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Players.Clear();
        Zombies.Clear();
    }

    public static void Register(Transform t)
    {
        if (t != null && !Players.Contains(t))
            Players.Add(t);
    }

    public static void Unregister(Transform t)
    {
        Players.Remove(t);
    }

    public static void CleanupPlayers()
    {
        for (int i = Players.Count - 1; i >= 0; i--)
        {
            if (Players[i] == null)
            {
                Players.RemoveAt(i);
            }
        }
    }

    public static void RegisterZombie(ZombieAI zombie)
    {
        if (zombie != null && !Zombies.Contains(zombie))
            Zombies.Add(zombie);
    }

    public static void UnregisterZombie(ZombieAI zombie)
    {
        Zombies.Remove(zombie);
    }

    public static void CleanupZombies()
    {
        for (int i = Zombies.Count - 1; i >= 0; i--)
        {
            if (Zombies[i] == null || !Zombies[i].IsAlive)
            {
                Zombies.RemoveAt(i);
            }
        }
    }
}
