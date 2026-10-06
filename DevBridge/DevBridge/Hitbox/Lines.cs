using System.Collections.Generic;
using System.Linq;
using DevBridge.Overlay;
using UnityEngine;
using UnityEngine.Rendering;

namespace DevBridge.Hitbox
{
    /// <summary>
    /// Short-lived world-space lines, drawn over everything (no depth test) so a hit shape shows through the creature and
    /// the ground. Each lasts its seconds of game time, so a frozen swing keeps its lines. They are drawn from one
    /// LinePool, as /overlay draws, so the line renderers are reused rather than made for each line and destroyed after
    /// it: a frame that added a line or saw one run out sets the pool again. They live under one root in the world
    /// scene, so logging out removes them.
    /// </summary>
    internal static class Lines
    {
        // Lines alive at once; past it the oldest goes first (an axe sweep leaves three a frame).
        private const int Most = 600;

        private struct Timed
        {
            internal Vector3[] Points;
            internal Color Color;
            internal float Width;
            internal float Until;
        }

        private static readonly List<Timed> Alive = new List<Timed>();
        private static readonly LinePool Pool = new LinePool("lines");
        private static GameObject root;
        private static Material material;
        private static bool changed;
        private static float nextEnd = float.MaxValue;

        internal static void Draw(IList<Vector3> points, Color color, float seconds, float width = 0.04f)
        {
            if (points.Count < 2) return;
            if (!root) Restart();
            if (Alive.Count >= Most) Alive.RemoveAt(0);
            float until = Time.time + seconds;
            Alive.Add(new Timed { Points = points as Vector3[] ?? points.ToArray(), Color = color, Width = width, Until = until });
            nextEnd = Mathf.Min(nextEnd, until);
            changed = true;
        }

        /// <summary>A flat circle round a point, for a body's outline on the ground.</summary>
        internal static Vector3[] Ring(Vector3 centre, float radius, int steps = 24)
        {
            var points = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float angle = i * Mathf.PI * 2f / steps;
                points[i] = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            }
            return points;
        }

        /// <summary>The same over-everything line material, for lines kept and updated elsewhere (/overlay's pool).</summary>
        internal static Material Shared => Material();

        /// <summary>Once a frame, after the game's updates: lines past their time go, and the pool is set again if anything changed.</summary>
        internal static void Tick()
        {
            if (Time.time >= nextEnd) Expire();
            if (!changed || !root) return;
            changed = false;
            Pool.Begin(root.transform, Most);
            foreach (Timed line in Alive) Pool.Add(line.Points, line.Color, line.Width);
            Pool.End();
        }

        private static void Expire()
        {
            float now = Time.time;
            int before = Alive.Count;
            Alive.RemoveAll(line => line.Until <= now);
            changed |= Alive.Count != before;
            nextEnd = float.MaxValue;
            foreach (Timed line in Alive) nextEnd = Mathf.Min(nextEnd, line.Until);
        }

        // The first line, or the first in a new world: the old root went with the last world's scene, its lines with it.
        private static void Restart()
        {
            Alive.Clear();
            Pool.Clear();
            nextEnd = float.MaxValue;
            root = new GameObject("DevBridge_Hitbox");
            root.AddComponent<LineTicker>();
        }

        // The engine's own line shader, which the game build keeps: vertex colours, alpha blended, depth test off.
        private static Material Material()
        {
            if (material) return material;
            material = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave, renderQueue = 5000 };
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_Cull", (int)CullMode.Off);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_ZTest", (int)CompareFunction.Always);
            return material;
        }
    }

    /// <summary>Runs Lines.Tick once a frame from the lines' own root, so it stops with them when the world unloads.</summary>
    internal sealed class LineTicker : MonoBehaviour
    {
        private void LateUpdate() => Lines.Tick();
    }
}
