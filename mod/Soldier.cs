// An OPFOR toy soldier built from primitives with the generated camo texture: strafes, shoots tracers at the bird,
// takes damage from the bird's rifle volley and from being rammed by the board, falls over when it dies.
using System.Collections.Generic;
using Sigf.Kit;
using UnityEngine;

public class Soldier : MonoBehaviour
{
    public static readonly List<Soldier> All = new List<Soldier>();
    static readonly string[] Names = { "Pvt. Waffles", "Sgt. Crumbs", "Cpl. Fluffy", "Lt. Bagel", "Maj. Nugget", "Pvt. Nacho", "Gen. Pickle", "Sgt. Biscuit", "Cpl. Muffin", "Pvt. Noodle" };

    public const float Height = 1.0f;
    public string callsign;
    public float hp = 100f, maxHp = 100f;
    public bool dead;
    Rigidbody rb;
    Transform legL, legR, armR;
    readonly List<Renderer> rends = new List<Renderer>();
    readonly List<Color> cols = new List<Color>();
    float flash, nextShot, strafeT, nextRam, swing;
    int dirSign = 1;
    Vector3 home;
    Transform muzzle;

    public Vector3 Chest => transform.position + Vector3.up * Height * 0.5f;
    public Vector3 Head => transform.position + Vector3.up * Height * 1.12f;

    public static Soldier Spawn(Vector3 groundPos)
    {
        var root = new GameObject("SigfSoldier");
        root.transform.position = groundPos;
        var s = root.AddComponent<Soldier>();
        s.Build();
        return s;
    }

    GameObject Part(PrimitiveType t, Transform parent, Vector3 lp, Vector3 scale, Color c, Texture2D tex = null, float tile = 1f)
    {
        var go = Mix.Shape(t, Vector3.zero, scale, c, false, "SoldierPart");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lp;
        if (tex != null)
        {
            Mix.Paint(go, c, 0f, tex);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.material.mainTextureScale = new Vector2(tile, tile);
        }
        foreach (var r in go.GetComponentsInChildren<Renderer>()) { rends.Add(r); cols.Add(r.material.color); }
        return go;
    }

    void Build()
    {
        callsign = Names[Random.Range(0, Names.Length)];
        home = transform.position;
        var camo = Mix.Texture("camo.png");
        var skin = new Color(0.85f, 0.65f, 0.5f);
        var dark = new Color(0.12f, 0.13f, 0.1f);
        var white = Color.white;
        var legPivotL = new GameObject("legL").transform; legPivotL.SetParent(transform, false); legPivotL.localPosition = new Vector3(-0.08f, 0.3f, 0f);
        var legPivotR = new GameObject("legR").transform; legPivotR.SetParent(transform, false); legPivotR.localPosition = new Vector3(0.08f, 0.3f, 0f);
        legL = legPivotL; legR = legPivotR;
        Part(PrimitiveType.Cube, legL, new Vector3(0, -0.15f, 0), new Vector3(0.13f, 0.32f, 0.14f), white, camo, 0.5f);
        Part(PrimitiveType.Cube, legR, new Vector3(0, -0.15f, 0), new Vector3(0.13f, 0.32f, 0.14f), white, camo, 0.5f);
        Part(PrimitiveType.Cube, legL, new Vector3(0, -0.3f, 0.03f), new Vector3(0.14f, 0.06f, 0.2f), dark);
        Part(PrimitiveType.Cube, legR, new Vector3(0, -0.3f, 0.03f), new Vector3(0.14f, 0.06f, 0.2f), dark);
        Part(PrimitiveType.Cube, transform, new Vector3(0, 0.46f, 0), new Vector3(0.34f, 0.34f, 0.2f), white, camo, 0.8f);
        Part(PrimitiveType.Cube, transform, new Vector3(0, 0.5f, 0.08f), new Vector3(0.26f, 0.16f, 0.06f), dark); // vest
        Part(PrimitiveType.Sphere, transform, new Vector3(0, 0.74f, 0), new Vector3(0.2f, 0.22f, 0.2f), skin);
        Part(PrimitiveType.Sphere, transform, new Vector3(0, 0.8f, -0.01f), new Vector3(0.26f, 0.2f, 0.27f), new Color(0.3f, 0.36f, 0.2f)); // helmet
        Part(PrimitiveType.Cube, transform, new Vector3(0, 0.75f, 0.1f), new Vector3(0.2f, 0.06f, 0.04f), dark); // goggles
        Part(PrimitiveType.Cube, transform, new Vector3(-0.21f, 0.46f, 0.02f), new Vector3(0.08f, 0.3f, 0.09f), white, camo, 0.3f);
        var arm = Part(PrimitiveType.Cube, transform, new Vector3(0.2f, 0.52f, 0.12f), new Vector3(0.08f, 0.1f, 0.3f), white, camo, 0.3f);
        armR = arm.transform;
        var rifle = Part(PrimitiveType.Cube, transform, new Vector3(0.12f, 0.5f, 0.28f), new Vector3(0.06f, 0.09f, 0.55f), dark);
        muzzle = new GameObject("muzzle").transform;
        muzzle.SetParent(transform, false);
        muzzle.localPosition = new Vector3(0.12f, 0.5f, 0.58f);
        // Red enemy plate on the back so they read as the other team from every side.
        Part(PrimitiveType.Cube, transform, new Vector3(0, 0.55f, -0.11f), new Vector3(0.18f, 0.18f, 0.02f), new Color(0.8f, 0.1f, 0.1f));

        var cap = gameObject.AddComponent<CapsuleCollider>();
        cap.height = Height; cap.radius = 0.2f; cap.center = new Vector3(0, Height * 0.5f, 0);
        rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 0.6f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        nextShot = Time.time + Random.Range(1.5f, 3.5f);
        strafeT = Random.Range(0.5f, 2f);
        All.Add(this);
    }

    void Update()
    {
        if (dead) return;
        float dt = Time.deltaTime;
        var toBird = G.Pos - transform.position; toBird.y = 0f;
        float dist = toBird.magnitude;
        if (toBird.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toBird.normalized), 6f * dt);

        strafeT -= dt;
        if (strafeT <= 0f) { dirSign = Random.value < 0.5f ? -1 : 1; strafeT = Random.Range(1f, 2.5f); }
        var fromHome = transform.position - home; fromHome.y = 0f;
        if (fromHome.magnitude > 2.2f) dirSign = Vector3.Dot(fromHome, transform.right) > 0f ? -1 : 1;
        var v = rb.velocity;
        var sv = transform.right * dirSign * 0.7f;
        rb.velocity = new Vector3(sv.x, v.y, sv.z);
        swing += dt * 9f;
        legL.localRotation = Quaternion.Euler(Mathf.Sin(swing) * 28f, 0, 0);
        legR.localRotation = Quaternion.Euler(-Mathf.Sin(swing) * 28f, 0, 0);

        if (dist < 14f && Time.time >= nextShot) Shoot();

        if (flash > 0f)
        {
            flash -= dt;
            for (int i = 0; i < rends.Count; i++) if (rends[i] != null) rends[i].material.color = flash > 0f ? new Color(3f, 1f, 1f) : cols[i];
        }

        // Ramming: the board at speed hits this soldier (the board's physics decide the speed).
        var c = Chest;
        if (Time.time > nextRam && (G.Pos - c).sqrMagnitude < 0.6f * 0.6f && G.Speed > 1.8f)
        {
            nextRam = Time.time + 0.5f;
            Hurt(Mathf.Clamp(G.Speed * 14f, 25f, 70f), (c - G.Pos).normalized, "Ramming Speed");
        }
    }

    void Shoot()
    {
        nextShot = Time.time + Random.Range(1.2f, 2.6f);
        var m = muzzle.position;
        Fx.MuzzleFlash(m, 0.8f);
        Fx.Sfx("shot", m, 0.35f, Random.Range(0.85f, 1.15f));
        var target = G.Pos + Vector3.up * 0.25f + Random.insideUnitSphere * 0.5f;
        Fx.Tracer(m, target, new Color(1f, 0.25f, 0.15f), 22f, () =>
        {
            Fx.Sparks(target, 5);
            Hud.DamageFlash();
        });
    }

    void OnCollisionEnter(Collision col)
    {
        if (dead || Time.time < nextRam) return;
        if (col.collider.GetComponentInParent<Skatebirb.SkateboardGateway>() == null) return;
        float sp = col.relativeVelocity.magnitude;
        if (sp < 1.2f) return;
        nextRam = Time.time + 0.5f;
        Hurt(Mathf.Clamp(sp * 14f, 25f, 70f), (Chest - G.Pos).normalized, "Ramming Speed");
    }

    public void Hurt(float dmg, Vector3 dir, string source)
    {
        if (dead) return;
        hp -= dmg;
        flash = 0.09f;
        Fx.Sparks(Chest, 8);
        Fx.Sfx("hit", Chest, 0.9f, Random.Range(0.9f, 1.2f));
        Hud.Hitmarker(hp <= 0f);
        rb.AddForce((dir.normalized + Vector3.up * 0.4f) * 1.4f, ForceMode.Impulse);
        if (hp <= 0f) Die(dir, source);
    }

    void Die(Vector3 dir, string source)
    {
        dead = true;
        All.Remove(this);
        rb.constraints = RigidbodyConstraints.None;
        rb.AddForce((dir.normalized + Vector3.up) * 2.5f, ForceMode.Impulse);
        rb.AddTorque(Random.onUnitSphere * 1.5f, ForceMode.Impulse);
        Mix.Burst(Chest, Fx.Smoke, 10, 1.5f, 0.1f, 1.2f);
        foreach (var r in rends) if (r != null) r.material.color = new Color(0.55f, 0.55f, 0.55f);
        for (int i = 0; i < rends.Count; i++) if (rends[i] != null) cols[i] = rends[i].material.color;
        SigfMod.OnKill(this, source);
        Destroy(gameObject, 4f);
    }

    void OnDestroy() { All.Remove(this); }
}
