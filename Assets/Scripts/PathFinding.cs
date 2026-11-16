using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Grid pathfinding for enemies.
// Put this on the Grid object (the parent of your Ground and Walls tilemaps).
public class PathGrid : MonoBehaviour
{
    public static PathGrid Instance { get; private set; }

    [SerializeField] LayerMask wallMask;        // falls back to the layer named Walls
    [SerializeField] float cellSize = 1f;       // keep equal to your tile size so 1-tile doors work
    [SerializeField] float agentRadius = 0.4f;  // how wide enemies are; cells closer to a wall than this are blocked
    [SerializeField] bool showGizmos = true;    // Scene view while playing: red squares = blocked cells

    public float AgentRadius => agentRadius;

    Vector2 origin;
    int width, height;
    bool[] blocked;
    bool baked;

    // A* working memory
    float[] gCost;
    int[] parent;
    byte[] state;   // 0 = unseen, 1 = open, 2 = closed

    void Awake()
    {
        Instance = this;
        if (wallMask.value == 0) wallMask = LayerMask.GetMask("Walls");
    }

    // wait one physics step so the wall colliders exist, then bake
    IEnumerator Start()
    {
        yield return new WaitForFixedUpdate();
        EnsureBaked();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------- Building the grid ----------

    void EnsureBaked()
    {
        if (baked) return;
        baked = true;

        // the area covered by every tilemap under this object
        bool any = false;
        Vector2 min = Vector2.zero, max = Vector2.zero;

        foreach (var tm in GetComponentsInChildren<Tilemap>())
        {
            BoundsInt cb = tm.cellBounds;
            if (cb.size.x <= 0 || cb.size.y <= 0) continue;

            Vector3 a = tm.CellToWorld(cb.min);
            Vector3 b = tm.CellToWorld(cb.max);
            Vector2 lo = Vector2.Min(a, b);
            Vector2 hi = Vector2.Max(a, b);

            if (!any) { min = lo; max = hi; any = true; }
            else { min = Vector2.Min(min, lo); max = Vector2.Max(max, hi); }
        }

        if (!any)
        {
            Debug.LogWarning("PathGrid: no tilemaps found under this object. Put the PathGrid script on the Grid object.", this);
            return;
        }

        origin = min - Vector2.one * cellSize;   // one cell of padding
        width = Mathf.CeilToInt((max.x - min.x) / cellSize) + 2;
        height = Mathf.CeilToInt((max.y - min.y) / cellSize) + 2;

        Physics2D.SyncTransforms();

        blocked = new bool[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                blocked[y * width + x] = Physics2D.OverlapCircle(CellCenter(x, y), agentRadius, wallMask) != null;
    }

    Vector2 CellCenter(int x, int y)
    {
        return origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
    }

    int WorldToCell(Vector2 w)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((w.x - origin.x) / cellSize), 0, width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((w.y - origin.y) / cellSize), 0, height - 1);
        return y * width + x;
    }

    // the closest open cell to this one (the cell itself if it is open)
    int NearestFree(int cell)
    {
        if (!blocked[cell]) return cell;

        int cx = cell % width, cy = cell / width;
        int best = -1;
        float bestDist = float.MaxValue;

        for (int r = 1; r <= 4; r++)
        {
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;   // ring only
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    int ni = ny * width + nx;
                    if (blocked[ni]) continue;

                    float d = dx * dx + dy * dy;
                    if (d < bestDist) { bestDist = d; best = ni; }
                }
            if (best >= 0) return best;
        }
        return -1;
    }

    // ---------- Finding a path ----------

    // Fills 'path' with points to walk through (not including 'from'). Returns false if there is no way.
    public bool FindPath(Vector2 from, Vector2 to, List<Vector2> path)
    {
        path.Clear();
        EnsureBaked();
        if (blocked == null) return false;

        int start = NearestFree(WorldToCell(from));
        int goal = NearestFree(WorldToCell(to));
        if (start < 0 || goal < 0) return false;

        int n = width * height;
        if (gCost == null || gCost.Length != n)
        {
            gCost = new float[n];
            parent = new int[n];
            state = new byte[n];
        }
        System.Array.Clear(state, 0, n);

        var open = new List<int>();
        gCost[start] = 0f;
        parent[start] = -1;
        state[start] = 1;
        open.Add(start);

        int goalX = goal % width, goalY = goal / width;
        int iterations = 0;

        while (open.Count > 0 && iterations++ < 8000)
        {
            // take the open cell with the lowest total cost estimate
            int bi = 0;
            float bf = float.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                int c = open[i];
                float f = gCost[c] + Heuristic(c % width, c / width, goalX, goalY);
                if (f < bf) { bf = f; bi = i; }
            }
            int cur = open[bi];
            open[bi] = open[open.Count - 1];
            open.RemoveAt(open.Count - 1);
            state[cur] = 2;

            if (cur == goal) break;

            int cx = cur % width, cy = cur / width;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                    int ni = ny * width + nx;
                    if (blocked[ni] || state[ni] == 2) continue;

                    // no cutting across wall corners
                    if (dx != 0 && dy != 0 && (blocked[cy * width + nx] || blocked[ny * width + cx])) continue;

                    float ng = gCost[cur] + ((dx != 0 && dy != 0) ? 1.4142f : 1f);
                    if (state[ni] == 0 || ng < gCost[ni])
                    {
                        gCost[ni] = ng;
                        parent[ni] = cur;
                        if (state[ni] == 0) { state[ni] = 1; open.Add(ni); }
                    }
                }
        }

        if (state[goal] != 2) return false;   // never reached it

        // walk back from the goal to the start
        var cells = new List<Vector2>();
        for (int c = goal; c != -1; c = parent[c])
            cells.Add(CellCenter(c % width, c / width));
        cells.Reverse();

        // straighten the path: skip ahead to the farthest point we can walk to in a straight line
        Vector2 pos = from;
        int idx = 0;
        while (idx < cells.Count)
        {
            int far = idx;
            for (int j = cells.Count - 1; j > idx; j--)
            {
                if (ClearLine(pos, cells[j])) { far = j; break; }
            }
            path.Add(cells[far]);
            pos = cells[far];
            idx = far + 1;
        }

        // finish on the exact target when we can
        if (!blocked[WorldToCell(to)] && ClearLine(pos, to))
            path.Add(to);

        return true;
    }

    bool ClearLine(Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        float dist = d.magnitude;
        if (dist < 0.01f) return true;
        return Physics2D.CircleCast(a, agentRadius, d / dist, dist, wallMask).collider == null;
    }

    static float Heuristic(int x, int y, int gx, int gy)
    {
        float dx = Mathf.Abs(x - gx), dy = Mathf.Abs(y - gy);
        return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy);
    }

    // ---------- Debug ----------

    void OnDrawGizmos()
    {
        if (!showGizmos || blocked == null) return;

        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.35f);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (blocked[y * width + x])
                    Gizmos.DrawCube(CellCenter(x, y), Vector3.one * cellSize * 0.9f);
    }
}