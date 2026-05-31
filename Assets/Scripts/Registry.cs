using System.Collections.Generic;
using UnityEngine;

public class Registry : MonoBehaviour
{
    public static List<Transform> Players = new List<Transform>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Players.Clear();
    }
    public static void Register(Transform t)
    {
        if (!Players.Contains(t))
            Players.Add(t);
    }

    public static void Unregister(Transform t)
    {
        Players.Remove(t);
    }
}