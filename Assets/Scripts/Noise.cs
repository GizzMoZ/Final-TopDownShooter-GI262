using System;
using UnityEngine;

public static class Noise // sound call enemies
{
    public static event Action<Vector2, float> OnNoise;

    public static void Make(Vector2 position, float radius)
    {
        OnNoise?.Invoke(position, radius);
    }
}