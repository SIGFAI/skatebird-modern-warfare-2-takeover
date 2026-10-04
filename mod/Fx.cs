// Effects shared by the soldiers, the killstreaks and the HUD: tracers, explosions, sounds, ground lookup.
using System;
using System.Collections;
using Sigf.Kit;
using UnityEngine;

public static class Fx
{
    public static readonly Color Orange = new Color(1f, 0.55f, 0.1f);
    public static readonly Color Yellow = new Color(1f, 0.9f, 0.3f);
    public static readonly Color Smoke = new Color(0.35f, 0.35f, 0.35f);

    public static Vector3 Ground(Vector3 p)
    {
        RaycastHit h;
        if (Physics.Raycast(p + Vector3.up * 4f, Vector3.down, out h, 40f, ~0, QueryTriggerInteraction.Ignore)) return h.point;
        return p;
    }

    public static void Sfx(string name, Vector3? pos = null, float vol = 1f, float pitch = 1f)
    {
        try { Mix.Play(Mix.Sound(name + ".wav"), pos, vol, pitch); }
        catch (Exception e) { Mix.Error("Sfx " + name, e); }
    }

    /// <summary>A glowing streak flying from a to b; onHit runs on arrival.</summary>
    public static void Tracer(Vector3 a, Vector3 b, Color c, float speed, Action onHit)
    {
        Mix.Run(TracerRoutine(a, b, c, speed, onHit), "Tracer");
    }

    static IEnumerator TracerRoutine(Vector3 a, Vector3 b, Color c, float speed, Action onHit)
    {
        var go = Mix.Shape(PrimitiveType.Cube, a, new Vector3(0.025f, 0.025f, 0.45f), c, false, "SigfTracer");
        Mix.Paint(go, c, 5f);
        var dir = b - a;
        float d = Mathf.Max(0.01f, dir.magnitude);
        dir /= d;
        go.transform.rotation = Quaternion.LookRotation(dir);
        float t = 0f;
        while (t < d)
        {
            t += speed * Time.unscaledDeltaTime;
            if (go == null) yield break;
            go.transform.position = a + dir * Mathf.Min(t, d);
            yield return null;
        }
        if (go != null) UnityEngine.Object.Destroy(go);
        if (onHit != null) onHit();
    }

    public static void MuzzleFlash(Vector3 p, float size = 1f)
    {
        var l = Mix.Glow(p, Yellow, 2f * size, 4f);
        UnityEngine.Object.Destroy(l.gameObject, 0.07f);
        var s = Mix.Sphere(p, 0.09f * size, Yellow, false);
        Mix.Paint(s, Yellow, 6f);
        UnityEngine.Object.Destroy(s, 0.06f);
    }

    public static void Sparks(Vector3 p, int n = 8)
    {
        Mix.Burst(p, Yellow, n, 3.5f, 0.025f, 0.6f);
        Mix.Burst(p, Orange, n / 2, 2.5f, 0.03f, 0.5f);
    }

    public static void Explosion(Vector3 p, float size)
    {
        Sfx("boom", p, 1f, UnityEngine.Random.Range(0.9f, 1.1f));
        var ball = Mix.Sphere(p, size * 0.2f, Orange, false);
        Mix.Paint(ball, Orange, 4f);
        Mix.Run(Grow(ball, size, 0.45f), "ExplosionBall");
        var core = Mix.Sphere(p, size * 0.12f, Color.white, false);
        Mix.Paint(core, Color.white, 6f);
        Mix.Run(Grow(core, size * 0.7f, 0.25f), "ExplosionCore");
        var l = Mix.Glow(p + Vector3.up * 0.3f, Orange, size * 4f, 8f);
        UnityEngine.Object.Destroy(l.gameObject, 0.5f);
        Mix.Burst(p, Orange, 36, size * 3f, 0.07f * size, 1.2f);
        Mix.Burst(p, Yellow, 24, size * 4f, 0.05f * size, 0.9f);
        Mix.Burst(p, Smoke, 26, size * 2f, 0.12f * size, 2f);
    }

    static IEnumerator Grow(GameObject go, float to, float time)
    {
        float t = 0f;
        float from = go.transform.localScale.x;
        while (t < time && go != null)
        {
            t += Time.unscaledDeltaTime;
            go.transform.localScale = Vector3.one * Mathf.Lerp(from, to, Mathf.Sqrt(t / time));
            yield return null;
        }
        if (go != null) UnityEngine.Object.Destroy(go);
    }

    public static IEnumerator GrowKeep(GameObject go, float to, float time)
    {
        float t = 0f;
        float from = go.transform.localScale.x;
        while (t < time && go != null)
        {
            t += Time.unscaledDeltaTime;
            go.transform.localScale = Vector3.one * Mathf.Lerp(from, to, t / time);
            yield return null;
        }
    }
}
