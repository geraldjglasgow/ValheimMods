using System;
using System.Collections.Generic;
using EliteEquipment.Boots;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// Fitted leggings for one look, body mesh and lower end, made the first time they are needed and kept: the
    /// leggings' mesh and the body's mesh without the triangles they cover. The male and female bodies each get their
    /// own, and each length (bare legs, or boots of one height) its own; a look worn under the boots keeps its full
    /// length with boots on, sinks inside them and loses its ankle cuff. Null for a body they cannot be made from (logged
    /// once).
    /// </summary>
    internal static class FittedBuild
    {
        private static readonly Dictionary<Mesh, BodySurface> surfaces = new Dictionary<Mesh, BodySurface>();
        private static readonly Dictionary<(LegStyle, Mesh, int, int), FittedParts> made = new Dictionary<(LegStyle, Mesh, int, int), FittedParts>();

        public static FittedParts For(LegStyle style, Mesh bodyMesh, string[] bones, BootSet boots)
        {
            BodySurface body = Surface(bodyMesh, bones);
            if (body == null)
                return null;
            float top = BootTop.Of(body, boots);
            float bottom = LegRegion.Bottom(body, style.UnderBoots ? 0f : top);
            var key = (style, bodyMesh, Mathf.RoundToInt(bottom * 1000f), Mathf.RoundToInt(top * 1000f));
            if (!made.TryGetValue(key, out FittedParts parts))
                made[key] = parts = Guarded(style, body, bottom, top);
            return parts;
        }

        internal static BodySurface Surface(Mesh bodyMesh, string[] bones)
        {
            if (bodyMesh == null)
                return null;
            if (!surfaces.TryGetValue(bodyMesh, out BodySurface body))
            {
                surfaces[bodyMesh] = body = BodySurface.Of(bodyMesh, bones);
                if (body == null)
                    Plugin.Log.LogWarning($"EliteEquipment: the body {bodyMesh.name} cannot be read; it wears the game's leggings");
            }
            return body;
        }

        private static FittedParts Guarded(LegStyle style, BodySurface body, float bottom, float bootTop)
        {
            try
            {
                return Make(style, body, bottom, bootTop);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the {style.Key} leggings could not be fitted to {body.Mesh.name}: {e.Message}");
                return null;
            }
        }

        private static FittedParts Make(LegStyle style, BodySurface body, float bottom, float bootTop)
        {
            bool[] covered = LegRegion.Covered(body, bottom);
            var unwrap = new Unwrap(body, LegRegion.Covered(body, LegRegion.Ankle));
            var mesh = new FittedMesh();
            FittedShell.Build(style, body, covered, unwrap, mesh, bootTop);
            FittedShell.Hems(style, body, covered, mesh, bootTop);
            FittedBands.Add(style, body, covered, unwrap, mesh, bottom, bootTop > LegRegion.Ankle);
            return new FittedParts(mesh.ToMesh("EE_Fitted" + style.Key + "_" + body.Mesh.name, body.Mesh.bindposes), Trimmed(body, covered));
        }

        private static Mesh Trimmed(BodySurface body, bool[] covered)
        {
            Mesh trimmed = Object.Instantiate(body.Mesh);
            trimmed.name = body.Mesh.name + "_ee_fitted";
            var kept = new List<int>(body.Triangles.Length);
            for (int t = 0; t < covered.Length; t++)
            {
                if (!covered[t])
                    kept.AddRange(new[] { body.Triangles[3 * t], body.Triangles[3 * t + 1], body.Triangles[3 * t + 2] });
            }
            trimmed.SetTriangles(kept, 0);
            return trimmed;
        }
    }

    /// <summary>The leggings' mesh and the body without what they cover.</summary>
    internal sealed class FittedParts
    {
        public FittedParts(Mesh leggings, Mesh body)
        {
            Leggings = leggings;
            Body = body;
        }

        public Mesh Leggings { get; }
        public Mesh Body { get; }
    }
}
