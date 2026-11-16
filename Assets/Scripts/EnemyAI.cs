using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum AttackType { Chaser, Gunner }
    public enum IdleMode { Stand, Patrol }
    enum State { Idle, Patrol, Chase, Investigate, Return }
    enum LungePhase { None, Windup, Leap, Recover }
    enum MeleePhase { None, Windup, Swing }

    [Header("Behavior")]
    [SerializeField] AttackType attackType = AttackType.Chaser;
    [SerializeField] IdleMode idleMode = IdleMode.Stand;

    [Header("Patrol (drag empty objects in order)")]
    [SerializeField] Transform[] waypoints;
    [SerializeField] float patrolSpeed = 1.5f;
    [SerializeField] float waitAtWaypoint = 1f;

    [Header("Senses")]
    [SerializeField] float sightRange = 8f;
    [SerializeField] float viewAngle = 140f;     // vision cone when calm
    [SerializeField] float loseSightTime = 3f;   // chase stops after this long without seeing the player
    [SerializeField] LayerMask obstacleMask;     // for wall or other objects

    [Header("Chase / Investigate")]
    [SerializeField] float chaseSpeed = 3f;
    [SerializeField] float investigateSpeed = 2.5f;
    [SerializeField] float investigateWait = 2f; // investigate

    [Header("Pathfinding")]
    [SerializeField] bool usePathfinding = true; // pathfinding

    [Header("Gunner only")]
    [SerializeField] EnemyBullet bulletPrefab;
    [SerializeField] float stopDistance = 5f;
    [SerializeField] float reactionTime = 0.5f;
    [SerializeField] float fireDelay = 1.2f; 
    [SerializeField] float bulletSpeedScale = 0.5f; 
    [SerializeField] float spreadAngle = 6f;         
    [SerializeField] float weaponSearchRadius = 10f; // enemy search radius for weapons on the floor
    [SerializeField] bool useCarriedGun;  // enemy will use new gun if needed

    [Header("Melee weapon (enemies carrying a melee weapon)")]
    [SerializeField] float meleeWindup = 0.3f;     
    [SerializeField] float meleeCooldown = 0.8f;     // pause between swings

    [Header("Lunge (dog)")]
    [SerializeField] bool canLunge;
    [SerializeField] float lungeRange = 3.5f;        // dog lunge
    [SerializeField] float lungeWindup = 0.25f;      // pre lunge pause
    [SerializeField] float lungeSpeed = 12f;
    [SerializeField] float lungeDuration = 0.35f;    // lunge distance = speed x duration
    [SerializeField] float lungeRecover = 0.7f;      // stands still after a lunge, so it can be punished

    [Header("Debug")]
    [SerializeField] bool showVision = true;     // debug

    // Read by EnemyVisionCone
    public float SightRange => sightRange;
    public float ViewAngle => viewAngle;
    public LayerMask ObstacleMask => obstacleMask;
    public bool IsChasing => state == State.Chase;
    public bool IsInvestigating => state == State.Investigate;

    const float TurnSpeed = 540f;
    const float LookAroundSpeed = 120f;
    const float PickupReach = 0.7f;    // pickup range
    const float PlayerRadius = 0.5f;   // melee range

    Rigidbody2D rb;
    EnemyHealth health;
    EnemyWeapon gun;
    PlayerHealth playerHealth;
    Transform player;

    State state;
    Vector2 homePos;
    float homeRot;

    int waypointIndex;
    bool waiting;
    float waitTimer;

    Vector2 targetPos;
    bool arrived;

    Vector2 lastSeenPos;
    float loseTimer;
    float seenTimer;
    float nextFireTime;

    // pathfinding
    readonly List<Vector2> path = new List<Vector2>();
    int pathIndex;
    bool hasPath;
    Vector2 pathTarget;
    float nextRepathTime;

    // melee swing
    MeleePhase meleePhase;
    float meleeTimer;
    float nextMeleeTime;
    bool meleeHitDone;

    // lunge
    LungePhase lunge;
    float lungeTimer;
    Vector2 lungeDir;

    // gun fetching
    WeaponPickup fetchTarget;
    WeaponPickup ignoredPickup;
    float ignoreUntil;
    float nextSearchTime;

    float stuckTimer;
    Vector2 stuckCheckPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();

        // every enemy carries an EnemyWeapon
        gun = GetComponent<EnemyWeapon>();
        if (gun == null)
        {
            Debug.LogWarning(name + " has no Enemy Weapon component, so it spawns unarmed (fine for dogs). " +
                             "Add Enemy Weapon and fill its Weapon Pool to give it a weapon.", this);
            gun = gameObject.AddComponent<EnemyWeapon>();
        }

        // if the mask was left empty, use the layer named Walls
        if (obstacleMask.value == 0) obstacleMask = LayerMask.GetMask("Walls");

        homePos = rb.position;
        homeRot = rb.rotation;

        bool canPatrol = idleMode == IdleMode.Patrol && waypoints != null && waypoints.Length > 0;
        SetState(canPatrol ? State.Patrol : State.Idle);
    }

    // Start runs after every Awake, so the player has registered itself 
    void Start()
    {
        playerHealth = PlayerHealth.Instance;
        if (playerHealth != null) player = playerHealth.transform;
    }

    void OnEnable() { Noise.OnNoise += OnNoise; }
    void OnDisable() { Noise.OnNoise -= OnNoise; }

    void FixedUpdate()
    {
        if (health != null && (health.IsDead || health.IsStunned))
        {
            rb.linearVelocity = Vector2.zero;
            seenTimer = 0f;
            lunge = LungePhase.None;   // a stun cancels a lunge
            CancelMelee();             // and a swing
            return;
        }
        if (player == null || playerHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Spot the player from any calm state
        if (state != State.Chase && CanSeePlayer(true))
            EnterChase();

        switch (state)
        {
            case State.Idle: DoIdle(); break;
            case State.Patrol: DoPatrol(); break;
            case State.Chase: DoChase(); break;
            case State.Investigate: DoInvestigate(); break;
            case State.Return: DoReturn(); break;
        }
    }

    // States

    void DoIdle()
    {
        rb.linearVelocity = Vector2.zero;
        rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, homeRot, TurnSpeed * Time.fixedDeltaTime);
    }

    void DoPatrol()
    {
        if (waypoints == null || waypoints.Length == 0) { SetState(State.Idle); return; }

        if (waiting)
        {
            rb.linearVelocity = Vector2.zero;
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0f) { waiting = false; ResetStuck(); }
            return;
        }

        if (NavigateTo(waypoints[waypointIndex].position, patrolSpeed))
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            waiting = true;
            waitTimer = waitAtWaypoint;
        }
    }

    void DoChase()
    {
        bool seeing = CanSeePlayer(false);   // no cone after alerted
        Vector2 toPlayer = (Vector2)player.position - rb.position;

        if (seeing)
        {
            lastSeenPos = player.position;
            loseTimer = 0f;
            seenTimer += Time.fixedDeltaTime;
        }
        else
        {
            loseTimer += Time.fixedDeltaTime;
            seenTimer = 0f;
        }

        // investigate if lost sight
        if (loseTimer >= loseSightTime)
        {
            EnterInvestigate(lastSeenPos);
            return;
        }

        bool isGunner = attackType == AttackType.Gunner;

        // Out of ammo: go and grab another gun from the floor, like the player does.
        // A Gunner always does. A chaser only does if it carried a gun and uses it (Use Carried Gun).
        bool wantsGun = isGunner || (useCarriedGun && gun.IsOutOfAmmo);
        if (wantsGun && !gun.HasAmmo && TryFetchWeapon()) return;

        // Shoots if it is a gunner, or a chaser carrying a gun (when Use Carried Gun is on)
        bool armedShooter = (isGunner || useCarriedGun) && gun.HasAmmo;

        // Carrying a melee weapon: swing it when the player is in reach
        if (!armedShooter && DoMeleeAttack(seeing, toPlayer)) return;

        // Dog-style leap at the player
        if (canLunge && attackType == AttackType.Chaser && !armedShooter && DoLunge(seeing, toPlayer)) return;

        // Everyone else runs at the last place they saw the player
        if (!armedShooter || !seeing)
        {
            NavigateTo(lastSeenPos, chaseSpeed);
            return;
        }

        // Armed shooter with line of sight
        Vector2 dir = toPlayer.normalized;
        FaceDirection(dir);
        rb.linearVelocity = toPlayer.magnitude > stopDistance ? dir * chaseSpeed : Vector2.zero;

        if (seenTimer >= reactionTime && Time.time >= nextFireTime)
        {
            Shoot(dir);
            nextFireTime = Time.time + Mathf.Max(fireDelay, gun.Weapon.fireDelay);
        }
    }

    // Windup (weapon pulled back) -> Swing (hit lands halfway through). Returns true while attacking.
    bool DoMeleeAttack(bool seeing, Vector2 toPlayer)
    {
        WeaponData w = gun.Weapon;
        if (w == null || !w.isMelee) return false;

        switch (meleePhase)
        {
            case MeleePhase.None:
                if (seeing && Time.time >= nextMeleeTime && toPlayer.magnitude <= w.meleeRange + w.meleeRadius)
                {
                    meleePhase = MeleePhase.Windup;
                    meleeTimer = meleeWindup;
                    gun.Raise();   // pull the weapon back as a warning
                }
                return false;      // keeps chasing until the attack starts

            case MeleePhase.Windup:
                rb.linearVelocity = Vector2.zero;
                FaceDirection(toPlayer.normalized);

                meleeTimer -= Time.fixedDeltaTime;
                if (meleeTimer <= 0f)
                {
                    meleePhase = MeleePhase.Swing;
                    meleeTimer = w.swingTime;
                    meleeHitDone = false;
                    gun.Swing();
                }
                return true;

            case MeleePhase.Swing:
                rb.linearVelocity = Vector2.zero;

                meleeTimer -= Time.fixedDeltaTime;
                if (!meleeHitDone && meleeTimer <= w.swingTime * 0.5f)
                {
                    meleeHitDone = true;
                    MeleeHit(w);
                }
                if (meleeTimer <= 0f)
                {
                    meleePhase = MeleePhase.None;
                    nextMeleeTime = Time.time + meleeCooldown;
                }
                return true;
        }
        return false;
    }

    // Same hit area as the player's swing: a circle in front of the enemy
    void MeleeHit(WeaponData w)
    {
        Vector2 forward = Quaternion.Euler(0, 0, rb.rotation) * Vector2.up;
        Vector2 center = rb.position + forward * w.meleeRange;

        if (Vector2.Distance(center, player.position) > w.meleeRadius + PlayerRadius) return;
        if (Physics2D.Linecast(rb.position, player.position, obstacleMask)) return;   // wall in between

        playerHealth.TakeDamage(1);
    }

    void CancelMelee()
    {
        if (meleePhase == MeleePhase.None) return;

        meleePhase = MeleePhase.None;
        gun.CancelSwing();
    }

    // Windup -> Leap -> Recover. Returns true while the lunge is controlling the enemy's movement.
    bool DoLunge(bool seeing, Vector2 toPlayer)
    {
        switch (lunge)
        {
            case LungePhase.None:
                if (seeing && toPlayer.magnitude <= lungeRange)
                {
                    lunge = LungePhase.Windup;
                    lungeTimer = lungeWindup;
                }
                return false;   // keeps chasing normally until the lunge starts

            case LungePhase.Windup:
                rb.linearVelocity = Vector2.zero;
                lungeDir = toPlayer.normalized;   // keeps aiming until the leap
                FaceDirection(lungeDir);

                lungeTimer -= Time.fixedDeltaTime;
                if (lungeTimer <= 0f)
                {
                    lunge = LungePhase.Leap;
                    lungeTimer = lungeDuration;
                }
                return true;

            case LungePhase.Leap:
                rb.linearVelocity = lungeDir * lungeSpeed;
                FaceDirection(lungeDir);

                lungeTimer -= Time.fixedDeltaTime;
                if (lungeTimer <= 0f)
                {
                    rb.linearVelocity = Vector2.zero;
                    lunge = LungePhase.Recover;
                    lungeTimer = lungeRecover;
                }
                return true;

            case LungePhase.Recover:
                rb.linearVelocity = Vector2.zero;

                lungeTimer -= Time.fixedDeltaTime;
                if (lungeTimer <= 0f) lunge = LungePhase.None;
                return true;
        }
        return false;
    }

    void DoInvestigate()
    {
        if (!arrived)
        {
            if (NavigateTo(targetPos, investigateSpeed))
            {
                arrived = true;
                waitTimer = 0f;
            }
            return;
        }

        // Look around at the spot
        rb.linearVelocity = Vector2.zero;
        rb.rotation += LookAroundSpeed * Time.fixedDeltaTime;

        waitTimer += Time.fixedDeltaTime;
        if (waitTimer >= investigateWait) ReturnToPost();
    }

    void DoReturn()
    {
        if (NavigateTo(homePos, patrolSpeed)) SetState(State.Idle);
    }

    // Transitions

    void SetState(State s)
    {
        state = s;
        arrived = false;
        waiting = false;
        fetchTarget = null;
        hasPath = false;
        lunge = LungePhase.None;
        CancelMelee();
        ResetStuck();
    }

    void EnterChase()
    {
        SetState(State.Chase);
        lastSeenPos = player.position;
        loseTimer = 0f;
        seenTimer = 0f;
    }

    void EnterInvestigate(Vector2 pos)
    {
        SetState(State.Investigate);
        targetPos = pos;
        waitTimer = 0f;
    }

    void ReturnToPost()
    {
        bool canPatrol = idleMode == IdleMode.Patrol && waypoints != null && waypoints.Length > 0;
        SetState(canPatrol ? State.Patrol : State.Return);
    }

    void OnNoise(Vector2 pos, float radius)
    {
        if (health != null && health.IsDead) return;
        if (state == State.Chase) return;   // already after the player

        if (Vector2.Distance(rb.position, pos) <= radius)
            EnterInvestigate(pos);
    }

    // Senses

    bool CanSeePlayer(bool useCone)
    {
        Vector2 to = (Vector2)player.position - rb.position;
        if (to.magnitude > sightRange) return false;

        if (useCone)
        {
            Vector2 forward = Quaternion.Euler(0, 0, rb.rotation) * Vector2.up;
            if (Vector2.Angle(forward, to) > viewAngle / 2f) return false;
        }

        return !Physics2D.Linecast(rb.position, player.position, obstacleMask);
    }

    // Movement

    // Walks to a spot, going around walls if needed. Returns true when it has arrived (or is stuck).
    bool NavigateTo(Vector2 target, float speed)
    {
        PathGrid grid = PathGrid.Instance;
        if (!usePathfinding || grid == null) return MoveTo(target, speed);

        Vector2 to = target - rb.position;
        float dist = to.magnitude;

        // nothing in the way: walk straight there
        if (dist < 0.3f || Physics2D.CircleCast(rb.position, grid.AgentRadius, to / dist, dist, obstacleMask).collider == null)
        {
            hasPath = false;
            return MoveTo(target, speed);
        }

        // a wall is in the way: build a path (and rebuild it if the target moved a lot)
        bool needPath = !hasPath || (target - pathTarget).sqrMagnitude > 1f;
        if (needPath && Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + 0.4f;
            pathTarget = target;
            pathIndex = 0;
            hasPath = grid.FindPath(rb.position, target, path);
        }

        // no way through: try straight anyway, the stuck check will give up
        if (!hasPath) return MoveTo(target, speed);

        if (pathIndex >= path.Count)
        {
            hasPath = false;
            return MoveTo(target, speed);
        }

        // follow the path point by point
        if (MoveTo(path[pathIndex], speed))
        {
            pathIndex++;
            if (pathIndex >= path.Count) return true;
        }
        return false;
    }

    // check if we are close enough to the target, and move towards it if not. Returns true if arrived.
    bool MoveTo(Vector2 target, float speed)
    {
        Vector2 to = target - rb.position;
        if (to.magnitude < 0.2f)
        {
            rb.linearVelocity = Vector2.zero;
            return true;
        }

        Vector2 dir = to.normalized;
        rb.linearVelocity = dir * speed;
        FaceDirection(dir);

        // check if stuck (not moving for a while)
        stuckTimer += Time.fixedDeltaTime;
        if (stuckTimer >= 1f)
        {
            if (Vector2.Distance(rb.position, stuckCheckPos) < 0.1f)
            {
                rb.linearVelocity = Vector2.zero;
                return true;
            }
            stuckCheckPos = rb.position;
            stuckTimer = 0f;
        }
        return false;
    }

    void ResetStuck()
    {
        stuckTimer = 0f;
        stuckCheckPos = rb != null ? rb.position : Vector2.zero;
    }

    void FaceDirection(Vector2 dir)
    {
        float target = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, target, TurnSpeed * Time.fixedDeltaTime);
    }

    // Picking up guns

    // pick the nearest usable gun and grabs it. Returns false if there is none
    bool TryFetchWeapon()
    {
        // look for a gun a new gun every 0.4 seconds, so it doesn't waste time on a faraway gun that is already gone
        if (fetchTarget == null && Time.time >= nextSearchTime)
        {
            nextSearchTime = Time.time + 0.4f;
            fetchTarget = FindNearestGun();
        }
        if (fetchTarget == null) return false;

        Vector2 to = (Vector2)fetchTarget.transform.position - rb.position;

        // close enough: take it
        if (to.magnitude <= PickupReach)
        {
            gun.Equip(fetchTarget.weapon, fetchTarget.ammo);
            Destroy(fetchTarget.gameObject);
            fetchTarget = null;
            seenTimer = 0f;   // short pause to aim with the new gun
            return true;
        }

        // NavigateTo returns true when stuck, so skip this gun for a while
        if (NavigateTo(fetchTarget.transform.position, chaseSpeed))
        {
            ignoredPickup = fetchTarget;
            ignoreUntil = Time.time + 5f;
            fetchTarget = null;
            return false;
        }
        return true;
    }

    WeaponPickup FindNearestGun()
    {
        WeaponPickup best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in Physics2D.OverlapCircleAll(rb.position, weaponSearchRadius))
        {
            var p = hit.GetComponent<WeaponPickup>();
            if (p == null || p.weapon == null) continue;
            if (p.weapon.isMelee || p.ammo <= 0) continue;               
            if (p == ignoredPickup && Time.time < ignoreUntil) continue;   

            Vector2 pos = p.transform.position;
            if (Physics2D.Linecast(rb.position, pos, obstacleMask)) continue;   // wall 

            float d = (pos - rb.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = p; }
        }
        return best;
    }

    // Attacks

    void Shoot(Vector2 aim)
    {
        if (bulletPrefab == null) return;

        WeaponData w = gun.Weapon;

        Vector2 spawn = (Vector2)transform.position + aim * 0.6f;
        if (Physics2D.Linecast(transform.position, spawn, obstacleMask)) return;

        gun.UseAmmo();

        float totalSpread = w.spreadAngle + spreadAngle;
        for (int i = 0; i < w.pellets; i++)
        {
            float angle = Random.Range(-totalSpread / 2f, totalSpread / 2f);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * aim;

            EnemyBullet b = Instantiate(bulletPrefab, spawn, Quaternion.identity);
            b.Launch(dir, w.bulletSpeed * bulletSpeedScale, 3f);
        }
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (health != null && (health.IsDead || health.IsStunned)) return;
        if (gun != null && gun.Weapon != null && gun.Weapon.isMelee) return;

        var ph = c.collider.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(1);
    }

    void OnDrawGizmos() //visual cone
    {
        if (!showVision) return;

        Vector2 origin = transform.position;
        bool playing = Application.isPlaying;
        bool chasing = playing && state == State.Chase;
        bool investigating = playing && state == State.Investigate;

        if (chasing)
        {
            // Alert
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, sightRange);
            Gizmos.DrawWireSphere(lastSeenPos, 0.2f);
        }
        else
        {
            // Calm
            Gizmos.color = investigating ? new Color(1f, 0.6f, 0.1f) : Color.yellow;

            const int rays = 24;
            Vector2 prevEnd = origin;
            for (int i = 0; i <= rays; i++)
            {
                float a = -viewAngle / 2f + viewAngle * i / rays;
                Vector2 dir = Quaternion.Euler(0, 0, a) * transform.up;

                RaycastHit2D hit = Physics2D.Raycast(origin, dir, sightRange, obstacleMask);
                Vector2 end = hit.collider != null ? hit.point : origin + dir * sightRange;

                if (i == 0 || i == rays) Gizmos.DrawLine(origin, end);
                if (i > 0) Gizmos.DrawLine(prevEnd, end);
                prevEnd = end;
            }

            if (investigating) Gizmos.DrawSphere(targetPos, 0.15f);
        }

        // Where a dog starts its lunge
        if (canLunge)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(origin, lungeRange);
        }

        // The path it is following (green)
        if (playing && hasPath && pathIndex < path.Count)
        {
            Gizmos.color = Color.green;
            Vector2 from = origin;
            for (int i = pathIndex; i < path.Count; i++)
            {
                Gizmos.DrawLine(from, path[i]);
                from = path[i];
            }
        }

        // Line to the player while within range: green = clear, red = wall in the way
        if (playing && player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - origin;
            if (toPlayer.magnitude <= sightRange)
            {
                bool blocked = Physics2D.Linecast(origin, player.position, obstacleMask);
                Gizmos.color = blocked ? Color.red : Color.green;
                Gizmos.DrawLine(origin, player.position);
            }
        }

        // Patrol path
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            var a = waypoints[i];
            var b = waypoints[(i + 1) % waypoints.Length];
            if (a != null && b != null) Gizmos.DrawLine(a.position, b.position);
        }
    }
}