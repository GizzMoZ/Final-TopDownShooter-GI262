using System.Collections;
using UnityEngine;

// Shows the weapon a character is holding and animates melee swings.
// The weapon hangs on a pivot at the character's center, so rotating the pivot sweeps it around the body.
// A fading crescent (made in code, no asset needed) shows the swing's reach.
public class HeldWeapon : MonoBehaviour
{
    const int Segments = 16;

    SpriteRenderer sr;
    Coroutine routine;
    Coroutine vfxRoutine;
    Vector3 restPos;

    // slash trail
    Mesh slashMesh;
    MeshRenderer slashRenderer;
    Material slashMat;

    public static HeldWeapon Create(Transform parent)
    {
        var pivot = new GameObject("HeldWeapon");
        pivot.transform.SetParent(parent, false);
        var held = pivot.AddComponent<HeldWeapon>();

        var child = new GameObject("Sprite");
        child.transform.SetParent(pivot.transform, false);
        held.sr = child.AddComponent<SpriteRenderer>();
        held.sr.enabled = false;

        // draw above the character
        var parentSprite = parent.GetComponent<SpriteRenderer>();
        if (parentSprite != null)
        {
            held.sr.sortingLayerID = parentSprite.sortingLayerID;
            held.sr.sortingOrder = parentSprite.sortingOrder + 2;
        }

        held.CreateSlash(parent, parentSprite);
        return held;
    }

    void CreateSlash(Transform character, SpriteRenderer characterSprite)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return;

        var go = new GameObject("SlashVfx");
        go.transform.SetParent(character, false);

        slashMesh = new Mesh { name = "SlashMesh" };
        slashMesh.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = slashMesh;

        slashMat = new Material(shader);
        slashRenderer = go.AddComponent<MeshRenderer>();
        slashRenderer.sharedMaterial = slashMat;
        slashRenderer.enabled = false;

        if (characterSprite != null)
        {
            slashRenderer.sortingLayerID = characterSprite.sortingLayerID;
            slashRenderer.sortingOrder = characterSprite.sortingOrder + 3;
        }
    }

    // Shows this weapon (or nothing, if null)
    public void Show(WeaponData w)
    {
        CancelSwing();

        sr.enabled = w != null;
        if (w == null) return;

        Transform t = sr.transform;

        if (w.sprite != null)
        {
            sr.sprite = w.sprite;
            sr.color = Color.white;
            t.localPosition = w.holdOffset;
            t.localRotation = Quaternion.Euler(0f, 0f, w.spriteRotation);
            t.localScale = Vector3.one * WeaponVisual.FitScale(w.sprite, w.spriteLength, transform);
        }
        else
        {
            // no sprite yet: a colored bar sticking out the front
            sr.sprite = WeaponVisual.BarSprite();
            sr.color = w.color;
            t.localPosition = new Vector3(0f, 0.45f, 0f);
            t.localRotation = Quaternion.identity;
            t.localScale = new Vector3(0.18f, 0.5f, 1f);
        }

        restPos = t.localPosition;
    }

    // Pulls the weapon back, ready to swing (the enemy's warning)
    public void Raise(WeaponData w)
    {
        if (w == null || !sr.enabled) return;

        StopRoutines();
        transform.localRotation = Quaternion.Euler(0f, 0f, -w.swingArc / 2f);
    }

    // Plays the swing and the slash trail
    public void Swing(WeaponData w)
    {
        if (w == null || !sr.enabled || !gameObject.activeInHierarchy) return;

        StopRoutines();
        routine = StartCoroutine(SwingRoutine(w));
        vfxRoutine = StartCoroutine(SlashRoutine(w));
    }

    public void CancelSwing()
    {
        StopRoutines();
        ResetPose();
    }

    IEnumerator SwingRoutine(WeaponData w)
    {
        Transform sprite = sr.transform;
        float time = Mathf.Max(0.05f, w.swingTime);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / time;
            float c = Mathf.Clamp01(t);
            float eased = 1f - (1f - c) * (1f - c);   // fast start, soft finish

            // sweep from the right side across the front to the left
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-w.swingArc / 2f, w.swingArc / 2f, eased));

            // stab: push forward and come back
            sprite.localPosition = restPos + Vector3.up * (w.swingThrust * Mathf.Sin(c * Mathf.PI));

            yield return null;
        }

        ResetPose();
        routine = null;
    }

    // The crescent follows the weapon across the arc, then fades out
    IEnumerator SlashRoutine(WeaponData w)
    {
        if (slashRenderer == null || w.slashColor.a <= 0.01f) yield break;

        float k = 1f / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));   // undo the character's scale
        float arc = Mathf.Max(w.swingArc, 40f);                                  // a stab still gets a short streak
        float outerWorld = w.meleeRange + w.meleeRadius;
        float thickness = Mathf.Clamp(w.meleeRadius * 0.6f, 0.2f, 0.5f);
        float outer = outerWorld * k;
        float inner = Mathf.Max(0.2f, outerWorld - thickness) * k;

        var verts = new Vector3[(Segments + 1) * 2];
        var cols = new Color[verts.Length];
        var tris = new int[Segments * 6];

        for (int i = 0; i <= Segments; i++)
        {
            float a = Mathf.Lerp(-arc / 2f, arc / 2f, i / (float)Segments);
            Vector3 dir = Quaternion.Euler(0f, 0f, a) * Vector3.up;
            verts[i * 2] = dir * inner;
            verts[i * 2 + 1] = dir * outer;
        }
        for (int i = 0; i < Segments; i++)
        {
            int v = i * 2, ti = i * 6;
            tris[ti] = v; tris[ti + 1] = v + 1; tris[ti + 2] = v + 2;
            tris[ti + 3] = v + 1; tris[ti + 4] = v + 3; tris[ti + 5] = v + 2;
        }

        slashMesh.Clear();
        slashMesh.vertices = verts;
        slashMesh.triangles = tris;
        slashRenderer.enabled = true;

        float time = Mathf.Max(0.05f, w.swingTime);
        const float fade = 0.12f;
        float t = 0f;

        while (t < time + fade)
        {
            t += Time.deltaTime;
            float c = Mathf.Clamp01(t / time);
            float head = 1f - (1f - c) * (1f - c);                    // the leading edge, same easing as the weapon
            float master = t <= time ? 1f : 1f - (t - time) / fade;   // fade out after the swing ends

            for (int i = 0; i <= Segments; i++)
            {
                float behind = head - i / (float)Segments;           // how far behind the leading edge this point is
                float alpha = behind < 0f ? 0f : Mathf.Clamp01(1f - behind / 0.6f);

                Color col = w.slashColor;
                col.a *= alpha * master;
                cols[i * 2] = col;
                cols[i * 2 + 1] = col;
            }
            slashMesh.colors = cols;
            slashMesh.RecalculateBounds();

            yield return null;
        }

        slashRenderer.enabled = false;
        vfxRoutine = null;
    }

    void StopRoutines()
    {
        if (routine != null) StopCoroutine(routine);
        if (vfxRoutine != null) StopCoroutine(vfxRoutine);
        routine = null;
        vfxRoutine = null;

        if (slashRenderer != null) slashRenderer.enabled = false;
    }

    void ResetPose()
    {
        transform.localRotation = Quaternion.identity;
        if (sr != null) sr.transform.localPosition = restPos;
    }

    public void Hide()
    {
        CancelSwing();
        if (sr != null) sr.enabled = false;
    }

    void OnDestroy()
    {
        if (slashMesh != null) Destroy(slashMesh);
        if (slashMat != null) Destroy(slashMat);
    }
}