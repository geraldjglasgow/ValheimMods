using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The game's own ghost of a terrain entry (read from the prefabs: a "_GhostOnly" child whose particle system
    /// draws one large flat billboard the size of the vanilla radius, sometimes a ring of small sparkles, and for level
    /// ground a thin marker pole). With the "Ghost Visuals" setting the ring follows the brush radius (Resize: the
    /// billboard size and the emitter radius are scaled, alive particles included), is hidden (Hide), or is left alone.
    /// A circle ring would misstate any other shape, so Resize hides it for squares, rectangles, rings and frames.
    /// Only this player's ghost instance is touched, never the prefab; the game makes a new ghost on every selection.
    /// </summary>
    internal static class GhostVisuals
    {
        private sealed class Part
        {
            public ParticleSystem System;
            public float Size;
            public float Radius;
            public float Rate;
            public bool Billboard;
        }

        private static GameObject ghost;
        private static TerrainOp ghostOp;
        private static readonly List<Part> parts = new List<Part>();
        private static readonly List<Renderer> renderers = new List<Renderer>();
        private static ParticleSystem.Particle[] particles = new ParticleSystem.Particle[64];
        private static float applied = 1f;
        private static bool hidden;

        public static void Update()
        {
            GameObject current = Player.m_localPlayer != null ? Player.m_localPlayer.m_placementGhost : null;
            if (current != ghost)
                Capture(current);
            if (ghost == null)
                return;
            ToolAction action = PreviewFrame.Action;
            bool brush = action != null && !action.IsPathTool;
            if (brush && !PreviewFrame.BrushVisible)
                return;
            GhostMode mode = brush ? PreviewSettings.GhostVisuals.Value : GhostMode.Leave;
            if (mode == GhostMode.Resize && PreviewFrame.HeightSpec.Print.Shape != BrushShape.Circle)
                mode = GhostMode.Hide;
            SetHidden(mode == GhostMode.Hide);
            Scale(mode == GhostMode.Resize ? Factor() : 1f);
        }

        /// <summary>
        /// The stroke's radius over the radius the ghost was made for: the height radius of the entry's own TerrainOp
        /// (the ghost keeps it), or its paint radius for a paint-only entry. EarthWright's cloned entries carry their own.
        /// </summary>
        private static float Factor()
        {
            StrokeParams p = PreviewFrame.Params;
            TerrainOp op = ghostOp;
            if (op == null || p == null)
                return 1f;
            TerrainOp.Settings s = op.m_settings;
            float own = Mathf.Max(s.m_level ? s.m_levelRadius : 0f, Mathf.Max(s.m_raise ? s.m_raiseRadius : 0f, s.m_smooth ? s.m_smoothRadius : 0f));
            bool paintOnly = own <= 0f;
            if (paintOnly)
                own = s.m_paintCleared ? s.m_paintRadius : 0f;
            float radius = paintOnly ? p.PaintPrint.Outer : p.HeightPrint.Outer;
            return own > 0.01f ? Mathf.Clamp(radius / own, 0.05f, 100f) : 1f;
        }

        private static void Capture(GameObject current)
        {
            ghost = current;
            ghostOp = current != null ? current.GetComponent<TerrainOp>() : null;
            parts.Clear();
            renderers.Clear();
            applied = 1f;
            hidden = false;
            if (ghost == null)
                return;
            foreach (ParticleSystem system in ghost.GetComponentsInChildren<ParticleSystem>(true))
                parts.Add(Describe(system));
            foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.enabled)
                    renderers.Add(renderer);
            }
        }

        private static Part Describe(ParticleSystem system)
        {
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            return new Part
            {
                System = system,
                Size = system.main.startSizeMultiplier,
                Radius = system.shape.radius,
                Rate = system.emission.rateOverTimeMultiplier,
                Billboard = renderer != null && renderer.renderMode == ParticleSystemRenderMode.HorizontalBillboard,
            };
        }

        private static void SetHidden(bool hide)
        {
            if (hide == hidden)
                return;
            hidden = hide;
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = !hide;
            }
        }

        private static void Scale(float factor)
        {
            if (Mathf.Abs(factor - applied) < 0.005f)
                return;
            applied = factor;
            foreach (Part part in parts)
            {
                if (part.System != null)
                    Scale(part, factor);
            }
        }

        /// <summary>The flat billboard grows in size (alive particles too); emitters grow in radius and rate.</summary>
        private static void Scale(Part part, float factor)
        {
            ParticleSystem.ShapeModule shape = part.System.shape;
            shape.radius = part.Radius * factor;
            if (!part.Billboard)
            {
                ParticleSystem.EmissionModule emission = part.System.emission;
                emission.rateOverTimeMultiplier = part.Rate * Mathf.Clamp(factor, 1f, 10f);
                return;
            }
            ParticleSystem.MainModule main = part.System.main;
            main.startSizeMultiplier = part.Size * factor;
            ResizeAlive(part.System, part.Size * factor);
        }

        private static void ResizeAlive(ParticleSystem system, float size)
        {
            int count = system.particleCount;
            if (particles.Length < count)
                particles = new ParticleSystem.Particle[count];
            count = system.GetParticles(particles, count);
            for (int i = 0; i < count; i++)
                particles[i].startSize = size;
            system.SetParticles(particles, count);
        }
    }
}
