// "Modern Warfare 2 Takeover": SkateBIRD turns into a Call of Duty match. OPFOR toy soldiers shoot at the bird, every
// trick the bird lands fires a volley from its M4A1 BirdSeed rifle at the nearest enemies, ramming them with the
// board hurts them too, and kills build killstreaks: UAV at 3, Predator Missile at 5, TACTICAL NUKE at 10.
// Care packages drop with red smoke and give the bird a Juggernaut suit.
using System.Collections;
using System.Linq;
using Sigf.Kit;
using UnityEngine;

public class SigfMod : MixMod
{
    static int streak, wave, packages;
    static Vector3 packagePos;
    static bool packageDown, demoMode;
    static Vector3 demoHome;
    static bool waveQueued, nukeBusy;
    static int nukes;
    static SigfMod I;
    static readonly Color Gold = new Color(1f, 0.7f, 0.15f);
    static readonly Color Green = new Color(0.4f, 1f, 0.5f);
    static readonly Color Red = new Color(1f, 0.25f, 0.2f);
    bool wasBailed;

    public override void OnLoad() => G.StartLevel = "playground";

    public override void OnReady()
    {
        I = this;
        Mix.Say("MODERN WARFARE 2", 3.5f, Gold, 0.22f, 96);
        Mix.Say("BIRD EDITION", 3.5f, Color.white, 0.32f, 48);
        Hud.StartIntro();
        Fx.Sfx("uav", null, 0.6f);
        G.OnTrick(t => Volley(t != null ? t.Name : "Trick"));
        Mix.After(1.0f, () => SpawnWave());
        Mix.Every(26f, () => { if (Soldier.All.Count > 0) CarePackage(); }, "package");
        Mix.Every(2.5f, () => { if (Soldier.All.Count == 0 && !waveQueued) QueueWave(); }, "waveWatch");
    }

    public override void OnGUI() => Hud.Draw();

    public override void OnUpdate()
    {
        bool b = G.Bailed;
                wasBailed = b;
        foreach (var s in Soldier.All.ToArray())
            if (s != null && s.transform.position.y < G.Pos.y - 15f) s.Hurt(999f, Vector3.up, "Out of bounds");
    }

    // ---------- waves ----------
    static void QueueWave()
    {
        waveQueued = true;
        Mix.After(2.5f, () => { waveQueued = false; SpawnWave(); });
    }

    public static void SpawnWave()
    {
        wave++;
        var center = G.Pos;
        if (demoMode && (G.Pos - demoHome).magnitude > 6f) { center = demoHome; G.Teleport(demoHome, G.Body.rotation); G.SetVelocity(Vector3.zero); }
        int n = Mathf.Min(10, 7 + wave);
        var fwd = G.Cam != null && !demoMode ? G.Cam.transform.forward : G.Forward; fwd.y = 0f; fwd = fwd.normalized;
        for (int i = 0; i < n; i++)
        {
            float ang = Mathf.Lerp(-55f, 55f, n == 1 ? 0.5f : i / (n - 1f)) + Random.Range(-6f, 6f);
            float dist = Random.Range(3.5f, 6.5f);
            var p = center + Quaternion.Euler(0, ang, 0) * fwd * dist;
            var g = Fx.Ground(p);
            Soldier.Spawn(g + Vector3.up * 0.05f);
        }
        Mix.Say("WAVE " + wave + "  -  " + n + " OPFOR INBOUND", 2.5f, Red, 0.42f, 44);
        Mix.Log("wave " + wave + " with " + n + " soldiers, bird at " + G.Pos);
    }

    // ---------- the bird's rifle ----------
    public static void Volley(string trick)
    {
        if (I == null) return;
        Mix.Run(VolleyRoutine(trick), "Volley");
    }

    static IEnumerator VolleyRoutine(string trick)
    {
        var targets = Soldier.All.Where(s => s != null && !s.dead).OrderBy(s => (s.transform.position - G.Pos).sqrMagnitude).Take(3).ToList();
        for (int i = 0; i < 9; i++)
        {
            if (!Hud.Shoot()) { yield return Mix.Wait(0.07f); continue; }
            var muzzle = G.Pos + Vector3.up * 0.4f + G.Forward * 0.25f;
            Fx.MuzzleFlash(muzzle);
            Fx.Sfx("shot", muzzle, 0.5f, Random.Range(0.95f, 1.2f));
            Soldier tgt = targets.Count > 0 ? targets[i % targets.Count] : null;
            if (tgt != null && !tgt.dead)
            {
                var end = tgt.Chest;
                Fx.Tracer(muzzle, end, Fx.Yellow, 60f, () => { if (tgt != null) tgt.Hurt(13f, (end - muzzle).normalized, trick); });
            }
            else
            {
                var end = muzzle + G.Forward * 6f + Random.insideUnitSphere * 0.4f;
                Fx.Tracer(muzzle, end, Fx.Yellow, 60f, null);
            }
            yield return Mix.Wait(0.07f);
        }
    }

    // ---------- kills and killstreaks ----------
    public static void OnKill(Soldier s, string source)
    {
        Hud.Kills++;
        streak++;
        Hud.Kill("BIRB  [" + source + "]  " + s.callsign);
        Hud.XpPopup("+100");
        if (Hud.Kills % 3 == 0) Fx.Sfx("v_kill", null, 0.8f);
        switch (streak)
        {
            case 3:
                Mix.Say("3 KILLSTREAK  -  UAV ONLINE", 3f, Green, 0.3f, 56);
                Fx.Sfx("uav", null, 0.9f);
                Hud.Uav(25f);
                break;
            case 5:
                Mix.Say("5 KILLSTREAK  -  PREDATOR MISSILE", 3f, Gold, 0.3f, 56);
                Mix.After(1.2f, () => Mix.Run(Missile(), "Missile"));
                break;
            case 10:
                Mix.Run(Nuke(), "Nuke");
                break;
        }
        if (Soldier.All.Count == 0 && !waveQueued && !nukeBusy) QueueWave();
    }

    public static IEnumerator Missile()
    {
        var alive = Soldier.All.Where(s => s != null && !s.dead).ToList();
        // Aim at the soldier with the most neighbours (or at the ground ahead).
        var target = alive.Count == 0 ? Fx.Ground(G.Ahead(4f)) : alive.OrderByDescending(a => alive.Count(b => (a.transform.position - b.transform.position).sqrMagnitude < 9f)).First().transform.position;
        var start = target + Vector3.up * 9f + new Vector3(3f, 0f, 2f);
        var body = new GameObject("SigfMissile");
        body.transform.position = start;
        var tube = Mix.Shape(PrimitiveType.Cylinder, start, new Vector3(0.12f, 0.35f, 0.12f), new Color(0.75f, 0.75f, 0.78f), false, "MissileBody");
        tube.transform.SetParent(body.transform, true);
        tube.transform.localRotation = Quaternion.Euler(90, 0, 0);
        var nose = Mix.Shape(PrimitiveType.Sphere, start, new Vector3(0.12f, 0.12f, 0.2f), new Color(0.8f, 0.15f, 0.1f), false, "MissileNose");
        nose.transform.SetParent(body.transform, true);
        nose.transform.localPosition = new Vector3(0, 0, 0.33f);
        var fire = Mix.Sphere(start, 0.2f, Fx.Orange, false);
        Mix.Paint(fire, Fx.Orange, 5f);
        fire.transform.SetParent(body.transform, true);
        fire.transform.localPosition = new Vector3(0, 0, -0.4f);
        var light = Mix.Glow(start, Fx.Orange, 4f, 4f, body.transform);
        Fx.Sfx("siren", null, 0.35f, 2f);
        float trail = 0f;
        while (body != null)
        {
            var d = target - body.transform.position;
            float step = 15f * Time.unscaledDeltaTime;
            if (d.magnitude <= step) break;
            body.transform.rotation = Quaternion.LookRotation(d.normalized);
            body.transform.position += d.normalized * step;
            trail -= Time.unscaledDeltaTime;
            if (trail <= 0f)
            {
                trail = 0.025f;
                Mix.Burst(body.transform.position - d.normalized * 0.4f, Fx.Smoke, 2, 0.4f, 0.1f, 1f);
                Mix.Burst(body.transform.position - d.normalized * 0.4f, Fx.Orange, 1, 0.6f, 0.07f, 0.4f);
            }
            yield return null;
        }
        Object.Destroy(body);
        Fx.Explosion(target, 3.2f);
        foreach (var s in Soldier.All.ToArray())
            if (s != null && (s.transform.position - target).magnitude < 3.6f) s.Hurt(200f, (s.transform.position - target).normalized + Vector3.up, "Predator Missile");
    }

    public static IEnumerator Nuke()
    {
        if (nukeBusy) yield break;
        nukeBusy = true;
        nukes++;
        Mix.Say("TACTICAL NUKE INCOMING", 4f, Red, 0.3f, 80);
        Fx.Sfx("v_nuke", null, 1f);
        Fx.Sfx("siren", null, 0.8f);
        for (int i = 3; i >= 1; i--)
        {
            Mix.Say(i.ToString(), 1f, Color.white, 0.5f, 140);
            yield return Mix.Wait(1f);
        }
        Hud.NukeFlash(3.2f);
        var p = G.Pos;
        Fx.Sfx("boom", null, 1f, 0.7f);
        var ball = Mix.Sphere(p, 0.5f, new Color(1f, 0.7f, 0.3f), false);
        Mix.Paint(ball, new Color(1f, 0.7f, 0.3f), 6f);
        Mix.Run(Fx.GrowKeep(ball, 40f, 1.4f), "NukeBall");
        var l = Mix.Glow(p + Vector3.up, Color.white, 40f, 8f);
        Object.Destroy(l.gameObject, 2f);
        Mix.Burst(p + Vector3.up * 0.5f, Fx.Orange, 60, 14f, 0.15f, 2.5f);
        G.Screm();
        yield return Mix.Wait(0.35f);
        foreach (var s in Soldier.All.ToArray()) if (s != null) s.Hurt(999f, (s.transform.position - p).normalized + Vector3.up, "TACTICAL NUKE");
        Mix.Say("MISSION ACCOMPLISHED", 4f, Gold, 0.3f, 80);
        yield return Mix.Wait(1.2f);
        Object.Destroy(ball);
        streak = 0;
        nukeBusy = false;
        yield return Mix.Wait(1f);
        if (Soldier.All.Count == 0 && !waveQueued) QueueWave();
    }

    // ---------- care packages ----------
    public static void CarePackage()
    {
        Mix.Run(CareRoutine(), "CarePackage");
    }

    static IEnumerator CareRoutine()
    {
        packages++;
        var side = G.Cam != null ? G.Cam.transform.right : Vector3.right; side.y = 0f;
        var land = Fx.Ground(G.Ahead(3.5f) + side.normalized * 1.8f) + Vector3.up * 0.25f;
        packagePos = land; packageDown = false;
        Mix.Say("CARE PACKAGE INBOUND", 2.5f, Gold, 0.3f, 56);
        Fx.Sfx("uav", null, 0.8f);
        var crate = Mix.Cube(land + Vector3.up * 8f, 0.5f, Color.white);
        Mix.Paint(crate, Color.white, 0f, Mix.Texture("crate.png"));
        var chute = Mix.Sphere(crate.transform.position + Vector3.up * 0.8f, 1f, new Color(0.9f, 0.45f, 0.1f), false);
        chute.transform.localScale = new Vector3(1.1f, 0.5f, 1.1f);
        chute.transform.SetParent(crate.transform, true);
        var smoke = Mix.Glow(crate.transform.position, Red, 3f, 2f, crate.transform);
        var rb = crate.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        while (crate != null && crate.transform.position.y > land.y)
        {
            crate.transform.position += Vector3.down * 2.8f * Time.unscaledDeltaTime;
            yield return null;
        }
        if (crate == null) yield break;
        crate.transform.position = land;
        rb.isKinematic = true;
        Object.Destroy(chute);
        packageDown = true;
        Mix.Burst(land, Fx.Smoke, 14, 1.2f, 0.1f, 1.5f);
        float until = Time.unscaledTime + 25f;
        float puff = 0f;
        while (crate != null && Time.unscaledTime < until)
        {
            puff -= Time.unscaledDeltaTime;
            if (puff <= 0f)
            {
                puff = 0.25f;
                Mix.Burst(land + Vector3.up * 0.3f, Red, 2, 0.8f, 0.09f, 1.6f);
            }
            var d = G.Pos - land; d.y = 0f;
            if (d.magnitude < 1.0f && Mathf.Abs(G.Pos.y - land.y) < 1.5f)
            {
                packageDown = false;
                Reward(packages);
                Fx.Sparks(land, 20);
                Object.Destroy(crate);
                yield break;
            }
            yield return null;
        }
        if (crate != null) Object.Destroy(crate);
    }

    static void Reward(int n)
    {
        Fx.Sfx("uav", null, 1f);
        if (n % 2 == 1)
        {
            Mix.Say("JUGGERNAUT SUIT  -  SUPER PUSH", 3.5f, Gold, 0.3f, 56);
            G.SuperPush(true);
            G.Screm();
            Mix.After(14f, () => G.SuperPush(false));
        }
        else
        {
            Mix.Say("HARRIER STRIKE", 3f, Gold, 0.3f, 56);
            Mix.Run(Missile(), "Harrier1");
            Mix.After(1.1f, () => Mix.Run(Missile(), "Harrier2"));
        }
    }

    // ---------- the clip ----------
    static void Face(Vector3 target)
    {
        var d = target - G.Pos; d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;
        G.Teleport(G.Pos, Quaternion.LookRotation(d.normalized));
    }

    static Vector3 NearestSoldier()
    {
        var s = Soldier.All.Where(x => x != null && !x.dead).OrderBy(x => (x.transform.position - G.Pos).sqrMagnitude).FirstOrDefault();
        return s != null ? s.transform.position : G.Ahead(4f);
    }

    static IEnumerator Medic()
    {
        while (true) { if (G.Bailed) G.GetUp(); yield return Mix.Wait(1.2f); }
    }

    static IEnumerator Strike(string flip)
    {
        if (G.Bailed) { G.GetUp(); yield return Mix.Wait(0.8f); }
        Face(NearestSoldier());
        G.Boost(2.5f);
        yield return Mix.Wait(0.5f);
        G.Launch(5f);
        yield return Mix.Wait(0.25f);
        G.Flip(flip);
        yield return Mix.Wait(1.6f);
    }

    public override IEnumerator Demo()
    {
        demoMode = true;
        Mix.Say("MODERN WARFARE 2", 4f, Gold, 0.22f, 96);
        Mix.Say("BIRD EDITION", 4f, Color.white, 0.32f, 48);
        Hud.StartIntro();
        demoHome = G.Pos;
        Mix.Run(Medic(), "Medic");
        // 1: the enemy opens fire, tracers fly at the bird, the HUD is up.
        yield return Mix.Wait(1.5f);
        // 2: tricks fire the bird's rifle volleys (the 5th kill earns the Predator Missile).
        Mix.Log("demo: act 2");
        yield return Strike("Kickflip");
        yield return Strike("Heelflip");
        CarePackage();
        yield return Strike("Kickflip");
        // 3: ramming a soldier with the board at speed.
        Mix.Log("demo: act 3");
        Face(NearestSoldier());
        G.Boost(6f);
        yield return Mix.Wait(1.6f);
        // 4: the care package.
        Mix.Log("demo: package");
        if (packageDown) { Face(packagePos); G.Boost(4f); yield return Mix.Wait(2f); }
        // 5: keep trick-killing; the 10th kill drops the nuke, then the next wave comes in.
        Mix.Log("demo: nuke chase");
        for (int i = 0; i < 20; i++)
        {
            if (nukes > 0 && !nukeBusy && packageDown) { Face(packagePos); G.Boost(4f); yield return Mix.Wait(2f); }
            if (nukeBusy) { yield return Mix.Wait(1f); continue; }
            yield return Strike(i % 2 == 0 ? "Kickflip" : "Heelflip");
        }
    }
}
