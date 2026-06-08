using System.Collections.Generic;
using UnityEngine;

public class Registry : MonoBehaviour
{
    public static List<Transform> Players = new List<Transform>();
    public static List<PlayerController> PlayerControllers = new List<PlayerController>();
    public static List<ZombieAI> Zombies = new List<ZombieAI>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Players.Clear();
        PlayerControllers.Clear();
        Zombies.Clear();
    }

    public static void Register(Transform t)
    {
        if (t == null)
            return;

        if (!Players.Contains(t))
            Players.Add(t);

        PlayerController controller = t.GetComponent<PlayerController>();
        if (controller == null)
            controller = t.GetComponentInChildren<PlayerController>();

        RegisterPlayerController(controller);
    }

    public static void RegisterPlayerController(PlayerController controller)
    {
        if (controller != null && !PlayerControllers.Contains(controller))
            PlayerControllers.Add(controller);
    }

    public static void Unregister(Transform t)
    {
        if (t == null)
            return;

        Players.Remove(t);

        PlayerController controller = t.GetComponent<PlayerController>();
        if (controller == null)
            controller = t.GetComponentInChildren<PlayerController>();

        if (controller != null)
            PlayerControllers.Remove(controller);
    }

    public static PlayerController GetPlayerController(Transform t)
    {
        if (t == null)
            return null;

        for (int i = PlayerControllers.Count - 1; i >= 0; i--)
        {
            PlayerController controller = PlayerControllers[i];
            if (controller == null)
            {
                PlayerControllers.RemoveAt(i);
                continue;
            }

            if (controller.transform == t || t.IsChildOf(controller.transform) || controller.transform.IsChildOf(t))
                return controller;
        }

        PlayerController found = t.GetComponent<PlayerController>();
        if (found == null)
            found = t.GetComponentInChildren<PlayerController>();

        RegisterPlayerController(found);
        return found;
    }

    public static void CleanupPlayers()
    {
        for (int i = Players.Count - 1; i >= 0; i--)
        {
            if (Players[i] == null)
                Players.RemoveAt(i);
        }

        for (int i = PlayerControllers.Count - 1; i >= 0; i--)
        {
            PlayerController controller = PlayerControllers[i];
            if (controller == null || controller.transform == null)
                PlayerControllers.RemoveAt(i);
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
            if (Zombies[i] == null)
                Zombies.RemoveAt(i);
        }
    }
}
