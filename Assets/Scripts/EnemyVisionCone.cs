using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
public class EnemyVisionCone : MonoBehaviour
{
    [SerializeField] Color calmColor = new Color(1f, 0.92f, 0.2f, 0.18f);
    [SerializeField] Color investigateColor = new Color(1f, 0.6f, 0.1f, 0.22f);
    [SerializeField] Color alertColor = new Color(1f, 0.15f, 0.15f, 0.22f);
    [SerializeField] int sortingOrder = -5;      // above the floor, below walls and characters
    [SerializeField] float degreesPerRay = 5f;   // lower = smoother edges, more raycasts

    EnemyAI ai;
    EnemyHealth health;
    Mesh mesh;
    MeshRenderer mr;
    Material mat;

    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<int> tris = new List<int>();

    void Awake()
    {
        ai = GetComponent<EnemyAI>();
        health = GetComponent<EnemyHealth>();

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("EnemyVisionCone: Sprites/Default shader not found.");
            enabled = false;
            return;
        }

        var go = new GameObject("VisionCone");
        go.transform.SetParent(transform, false);

        mesh = new Mesh { name = "VisionConeMesh" };
        mesh.MarkDynamic();

        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        mat = new Material(shader);
        mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.sortingOrder = sortingOrder;
    }

    void LateUpdate()
    {
        bool hidden = health != null && (health.IsDead || health.IsStunned);
        mr.enabled = !hidden;
        if (hidden) return;

        bool alerted = ai.IsChasing;
        float angle = alerted ? 360f : ai.ViewAngle;
        float start = alerted ? 0f : -angle / 2f;
        int rays = Mathf.Max(2, Mathf.CeilToInt(angle / degreesPerRay));

        Vector2 origin = transform.position;
        Vector2 forward = transform.up;
        float range = ai.SightRange;
        LayerMask mask = ai.ObstacleMask;

        verts.Clear();
        tris.Clear();
        verts.Add(Vector3.zero); 

        for (int i = 0; i <= rays; i++)
        {
            float a = start + angle * i / rays;
            Vector2 dir = Quaternion.Euler(0, 0, a) * forward;

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, range, mask);
            Vector2 end = hit.collider != null ? hit.point : origin + dir * range;

            verts.Add(transform.InverseTransformPoint(end));
        }

        for (int i = 0; i < rays; i++)
        {
            tris.Add(0);
            tris.Add(i + 1);
            tris.Add(i + 2);
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        mat.color = alerted ? alertColor
                  : ai.IsInvestigating ? investigateColor
                  : calmColor;
    }

    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (mat != null) Destroy(mat);
    }
}