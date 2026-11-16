using UnityEngine;

// Shared helpers for showing weapon sprites
public static class WeaponVisual
{
    static Sprite barSprite;

    // The scale that makes the sprite's longest side equal 'length' in the world,
    // whatever its pixel size, Pixels Per Unit, or the parent's own scale
    public static float FitScale(Sprite sprite, float length, Transform parent)
    {
        Vector2 size = sprite.bounds.size;
        float longest = Mathf.Max(size.x, size.y);
        float parentScale = parent != null ? Mathf.Max(0.0001f, Mathf.Abs(parent.lossyScale.x)) : 1f;

        return longest > 0.0001f ? length / longest / parentScale : 1f;
    }

    // A plain white square, used as the colored bar when a weapon has no sprite yet
    public static Sprite BarSprite()
    {
        if (barSprite != null) return barSprite;

        var tex = new Texture2D(4, 4);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();

        barSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return barSprite;
    }
}