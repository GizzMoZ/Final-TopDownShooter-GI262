using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerKeys  // The keys the player is carrying. Cleared whenever a scene loads, so pressing R starts without keys.
{
    static readonly HashSet<string> keys = new HashSet<string>();

    public static IEnumerable<string> All => keys;

    public static bool Has(string id) { return keys.Contains(id); }
    public static void Add(string id) { keys.Add(id); }
    public static bool Use(string id) { return keys.Remove(id); }
    public static void Clear() { keys.Clear(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        keys.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        keys.Clear();
    }
}