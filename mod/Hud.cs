// The Call of Duty style HUD, drawn with IMGUI over the game: crosshair, hitmarkers, killfeed, XP popups, radar,
// enemy name plates, ammo counter, damage flash, vignette, the nuke flash and the typed mission intro.
using System.Collections.Generic;
using Sigf.Kit;
using UnityEngine;

public static class Hud
{
    class Feed { public string text; public float until; }
    class Xp { public string text; public float at; }
    static readonly List<Feed> feed = new List<Feed>();
    static readonly List<Xp> xps = new List<Xp>();
    static float hitAt = -9f, hitKillAt = -9f, dmgAt = -9f, flashAt = -9f, flashLen = 1f, uavUntil, introAt = -1f;
    public static int Ammo = 30, Reserve = 90;
    public static int Kills;
    public static bool Visible = true;
    static Texture2D vignette;
    static GUIStyle label;
    static float reloadUntil;

    static readonly string[] Intro =
    {
        "MODERN WARFARE 2: BIRD EDITION",
        "\"Soap\" MacBirb  -  Task Force 141",
        "0640 HOURS  -  Somewhere in a giant bedroom",
        "Objective: kickflip every OPFOR in the room",
    };

    public static void StartIntro() { introAt = Time.unscaledTime; }
    public static void Hitmarker(bool kill) { hitAt = Time.unscaledTime; if (kill) hitKillAt = Time.unscaledTime; }
    public static void DamageFlash() { if (Time.unscaledTime - dmgAt > 0.9f) dmgAt = Time.unscaledTime; }
    public static void NukeFlash(float len) { flashAt = Time.unscaledTime; flashLen = len; }
    public static void Uav(float seconds) { uavUntil = Time.unscaledTime + seconds; }
    public static void Kill(string text) { feed.Add(new Feed { text = text, until = Time.unscaledTime + 5f }); if (feed.Count > 5) feed.RemoveAt(0); }
    public static void XpPopup(string text) { xps.Add(new Xp { text = text, at = Time.unscaledTime }); if (xps.Count > 5) xps.RemoveAt(0); }

    public static bool Shoot()
    {
        if (Time.unscaledTime < reloadUntil) return false;
        Ammo--;
        if (Ammo <= 0) { reloadUntil = Time.unscaledTime + 1.2f; Mix.After(1.2f, () => { Ammo = 30; Reserve = Mathf.Max(0, Reserve - 30); if (Reserve < 30) Reserve = 90; }); }
        return true;
    }

    static void Box(float x, float y, float w, float h, Color c)
    {
        var p = GUI.color; GUI.color = c;
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = p;
    }

    static void Text(string s, float x, float y, float w, float h, int size, Color c, TextAnchor a)
    {
        if (label == null) label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = false };
        float k = Screen.height / 1080f;
        label.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * k));
        label.alignment = a;
        var r = new Rect(x, y, w, h);
        var p = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.85f * c.a);
        GUI.Label(new Rect(r.x + 2 * k, r.y + 2 * k, r.width, r.height), s, label);
        GUI.color = c;
        GUI.Label(r, s, label);
        GUI.color = p;
    }

    static Texture2D MakeVignette()
    {
        const int n = 128;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x / (n - 1f) - 0.5f) * 2f, dy = (y / (n - 1f) - 0.5f) * 2f;
                float d = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.55f) / 0.9f);
                t.SetPixel(x, y, new Color(0, 0, 0, d * d * 0.75f));
            }
        t.Apply();
        t.wrapMode = TextureWrapMode.Clamp;
        return t;
    }

    public static void Draw()
    {
        if (Event.current.type != EventType.Repaint) return;
        float k = Screen.height / 1080f, W = Screen.width, H = Screen.height, now = Time.unscaledTime;
        if (!Visible) return;

        if (vignette == null) vignette = MakeVignette();
        GUI.DrawTexture(new Rect(0, 0, W, H), vignette);
        Box(0, 0, W, H, new Color(1f, 0.6f, 0.2f, 0.05f)); // warm desert grade

        // Damage flash: red edges when the OPFOR tracers land.
        float dm = 1f - (now - dmgAt) / 0.3f;
        if (dm > 0f)
        {
            var rc = new Color(0.9f, 0f, 0f, 0.35f * dm);
            float e = 50f * k;
            Box(0, 0, W, e, rc); Box(0, H - e, W, e, rc); Box(0, 0, e, H, rc); Box(W - e, 0, e, H, rc);
        }

        // Crosshair + hitmarker.
        float cx = W / 2f, cy = H / 2f;
        var cc = new Color(1, 1, 1, 0.85f);
        float gap = 8f * k, len = 12f * k, th = 2.5f * k;
        Box(cx - gap - len, cy - th / 2, len, th, cc); Box(cx + gap, cy - th / 2, len, th, cc);
        Box(cx - th / 2, cy - gap - len, th, len, cc); Box(cx - th / 2, cy + gap, th, len, cc);
        float hm = 1f - (now - hitAt) / 0.25f;
        if (hm > 0f)
        {
            bool kill = now - hitKillAt < 0.25f;
            var hc = kill ? new Color(1f, 0.1f, 0.1f, hm) : new Color(1f, 1f, 1f, hm);
            var m = GUI.matrix;
            for (int a = 45; a < 360; a += 90)
            {
                GUIUtility.RotateAroundPivot(a, new Vector2(cx, cy));
                Box(cx + 14f * k, cy - 1.5f * k, 14f * k, 3f * k, hc);
                GUI.matrix = m;
            }
        }

        // Enemy name plates.
        var cam = G.Cam;
        if (cam != null)
        {
            foreach (var s in Soldier.All)
            {
                if (s == null || s.dead) continue;
                var sp = cam.WorldToScreenPoint(s.Head + Vector3.up * 0.15f);
                if (sp.z <= 0f || sp.z > 25f) continue;
                float y = H - sp.y;
                float sc = Mathf.Clamp(5f / sp.z, 0.5f, 1.4f);
                Text("▼", sp.x - 50, y - 44 * k * sc, 100, 50, Mathf.RoundToInt(40 * sc), new Color(1f, 0.15f, 0.1f), TextAnchor.MiddleCenter);
                Text(s.callsign, sp.x - 150, y - 62 * k * sc, 300, 50, Mathf.RoundToInt(30 * sc), new Color(1f, 0.4f, 0.3f), TextAnchor.MiddleCenter);
                float bw = 60f * k * sc;
                Box(sp.x - bw / 2, y - 6 * k * sc, bw, 5 * k, new Color(0, 0, 0, 0.7f));
                Box(sp.x - bw / 2, y - 6 * k * sc, bw * Mathf.Clamp01(s.hp / s.maxHp), 5 * k, new Color(1f, 0.2f, 0.1f));
            }
        }

        // Radar bottom left.
        float rs = 220f * k, rx = 40f * k, ry = H * 0.30f;
        Box(rx, ry, rs, rs, new Color(0f, 0.08f, 0f, 0.55f));
        var gc = new Color(0.3f, 1f, 0.4f, 0.8f);
        Box(rx, ry, rs, 2, gc); Box(rx, ry + rs - 2, rs, 2, gc); Box(rx, ry, 2, rs, gc); Box(rx + rs - 2, ry, 2, rs, gc);
        Box(rx + rs / 2 - 1, ry, 2, rs, new Color(0.3f, 1f, 0.4f, 0.25f)); Box(rx, ry + rs / 2 - 1, rs, 2, new Color(0.3f, 1f, 0.4f, 0.25f));
        bool uav = now < uavUntil;
        float range = uav ? 22f : 12f;
        if (cam != null)
        {
            var f = cam.transform.forward; f.y = 0f; f = f.normalized;
            var r = new Vector3(f.z, 0, -f.x);
            foreach (var s in Soldier.All)
            {
                if (s == null || s.dead) continue;
                var d = s.transform.position - G.Pos;
                float px = Vector3.Dot(d, r) / range, py = Vector3.Dot(d, f) / range;
                if (px * px + py * py > 1f) continue;
                Box(rx + rs / 2 + px * rs / 2 - 5 * k, ry + rs / 2 - py * rs / 2 - 5 * k, 10 * k, 10 * k, new Color(1f, 0.15f, 0.1f));
            }
            if (uav)
            {
                float ang = (now * 200f) % 360f;
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(ang, new Vector2(rx + rs / 2, ry + rs / 2));
                Box(rx + rs / 2, ry + rs / 2 - 1, rs / 2, 2, new Color(0.3f, 1f, 0.4f, 0.7f));
                GUI.matrix = m;
            }
        }
        Box(rx + rs / 2 - 5 * k, ry + rs / 2 - 5 * k, 10 * k, 10 * k, new Color(0.4f, 0.8f, 1f));
        Text(uav ? "UAV ONLINE" : "RADAR", rx, ry - 30 * k, rs, 28, 22, uav ? new Color(0.4f, 1f, 0.5f) : new Color(0.7f, 0.9f, 0.7f), TextAnchor.MiddleLeft);

        // Ammo bottom right.
        bool reloading = now < reloadUntil;
        Text(reloading ? "RELOADING" : Ammo + " / " + Reserve, W - 480 * k, H - 150 * k, 440 * k, 90, 72, reloading ? new Color(1f, 0.5f, 0.2f) : Color.white, TextAnchor.MiddleRight);
        Text("M4A1 BIRDSEED  [FULL AUTO]", W - 600 * k, H - 70 * k, 560 * k, 36, 26, new Color(1f, 0.85f, 0.5f), TextAnchor.MiddleRight);
        Text("KILLS " + Kills, W - 480 * k, H - 210 * k, 440 * k, 50, 38, new Color(1f, 0.85f, 0.5f), TextAnchor.MiddleRight);

        // Killfeed top right.
        for (int i = feed.Count - 1; i >= 0; i--) if (now > feed[i].until) feed.RemoveAt(i);
        for (int i = 0; i < feed.Count; i++)
        {
            float a = Mathf.Clamp01((feed[i].until - now) * 2f);
            Text(feed[i].text, W - 740 * k, 40 * k + i * 38 * k, 700 * k, 36, 26, new Color(1f, 0.95f, 0.8f, a), TextAnchor.MiddleRight);
        }

        // XP popups under the crosshair.
        for (int i = xps.Count - 1; i >= 0; i--) if (now - xps[i].at > 1.3f) xps.RemoveAt(i);
        foreach (var x in xps)
        {
            float t = (now - x.at) / 1.3f;
            Text(x.text, cx + 40 * k, cy + 30 * k - t * 60 * k, 300 * k, 40, 34, new Color(1f, 0.9f, 0.3f, 1f - t), TextAnchor.MiddleLeft);
        }

        // Typed mission intro, top left.
        if (introAt >= 0f && now - introAt < 9f)
        {
            float t = now - introAt - 0.4f;
            float fade = Mathf.Clamp01((9f - (now - introAt)) * 1.5f);
            int chars = Mathf.Max(0, Mathf.RoundToInt(t * 38f));
            for (int i = 0; i < Intro.Length && chars > 0; i++)
            {
                string line = Intro[i].Substring(0, Mathf.Min(chars, Intro[i].Length));
                chars -= Intro[i].Length + 6;
                Text(line, 50 * k, 40 * k + i * 42 * k, 1100 * k, 40, i == 0 ? 38 : 28, new Color(1f, i == 0 ? 0.65f : 0.95f, i == 0 ? 0.2f : 0.85f, fade), TextAnchor.MiddleLeft);
            }
        }

        // Nuke flash.
        float fl = (now - flashAt) / flashLen;
        if (fl >= 0f && fl < 1f) Box(0, 0, W, H, new Color(1f, 1f, 0.95f, fl < 0.15f ? fl / 0.15f : 1f - (fl - 0.15f) / 0.85f));
    }
}
