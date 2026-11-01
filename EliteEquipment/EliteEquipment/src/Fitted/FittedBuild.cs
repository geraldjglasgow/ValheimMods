using System;
using System.Collections.Generic;
using EliteEquipment.Boots;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// Fitted leggings made once per kind and shared by every player who wears that kind: the leggings' mesh and the
    /// body's mesh without the triangles they cover. A kind is what the meshes are made from and nothing else: the look,
    /// the body mesh (the male or the female body, read once each, <see cref="BodySurface"/>), the leggings' lower end,
    /// and of the boots only what the look uses (their height, in millimetres, for a look worn under them, which sinks
    /// inside them; for the others only whether boots are on, which drops the ankle cuff). The trimmed body depends only
    /// on the body and the lower end, so every look of one length shares one (<see cref="BodyCut"/>), as every look shares
    /// a body's texture wrap. Both meshes are only drawn (each player's own renderer, on that player's own bones), as the
    /// game shares its own body and leggings meshes; nothing writes to them once made. Null for a body they cannot be made
    /// from (logged once). Kept while the scene runs, released when the menu or a world starts (<see cref="FittedCache"/>).
    /// </summary>
    internal static class FittedBuild
    {
        private static readonly Dictionary<Mesh, BodySurface> surfaces = new Dictionary<Mesh, BodySurface>();
        private static readonly Dictionary<Mesh, Unwrap> unwraps = new Dictionary<Mesh, Unwrap>();
        private static readonly Dictionary<(Mesh, int), BodyCut> cuts = new Dictionary<(Mesh, int), BodyCut>();
        private static readonly Dictionary<(LegStyle, Mesh, int, int), FittedParts> made = new Dictionary<(LegStyle, Mesh, int, int), FittedParts>();

        public static FittedParts For(LegStyle style, Mesh bodyMesh, SkinnedMeshRenderer skin, BootSet boots)
        {
            BodySurface body = Surface(bodyMesh, skin);
            if (body == null)
                return null;
            float top = BootTop.Of(body, boots);
            float bottom = LegRegion.Bottom(body, style.UnderBoots ? 0f : top);
            var key = (style, bodyMesh, Mm(bottom), Length(style, top));
            if (!made.TryGetValue(key, out FittedParts parts))
                made[key] = parts = Guarded(style, body, bottom, top);
            return parts;
        }

        /// <summary>The body as fitted leggings are made from it, read once per body mesh (its bone names only then).</summary>
        internal static BodySurface Surface(Mesh bodyMesh, SkinnedMeshRenderer skin)
        {
            if (bodyMesh == null || skin == null)
                return null;
            if (!surfaces.TryGetValue(bodyMesh, out BodySurface body))
            {
                surfaces[bodyMesh] = body = BodySurface.Of(bodyMesh, Array.ConvertAll(skin.bones, bone => bone != null ? bone.name : null));
                if (body == null)
                    Plugin.Log.LogWarning($"EliteEquipment: the body {bodyMesh.name} cannot be read; it wears the game's leggings");
            }
            return body;
        }

        /// <summary>A body read before anyone needs it, with its texture wrap and its trim for bare legs (<see cref="FittedCache"/>).</summary>
        internal static void Prepare(Mesh bodyMesh, SkinnedMeshRenderer skin)
        {
            BodySurface body = Surface(bodyMesh, skin);
            if (body == null)
                return;
            UnwrapOf(body);
            BodyCut cut = Cut(body, LegRegion.Ankle);
            if (cut.Trimmed == null)
                cut.Trimmed = Trimmed(body, cut.Covered);
        }

        /// <summary>
        /// What of the boots the leggings are made from: 0 for none (or boots too low to reach the ankle); for a look worn
        /// under them their height in millimetres; for the others only that they are on (-1).
        /// </summary>
        private static int Length(LegStyle style, float top) => top <= LegRegion.Ankle ? 0 : style.UnderBoots ? Mm(top) : -1;

        private static int Mm(float metres) => Mathf.RoundToInt(metres * 1000f);

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
            BodyCut cut = Cut(body, bottom);
            Unwrap unwrap = UnwrapOf(body);
            var mesh = new FittedMesh();
            FittedShell.Build(style, body, cut.Covered, unwrap, mesh, bootTop);
            FittedShell.Hems(style, body, cut.Covered, mesh, bootTop);
            FittedBands.Add(style, body, cut.Covered, unwrap, mesh, bottom, bootTop > LegRegion.Ankle);
            Mesh leggings = mesh.ToMesh("EE_Fitted" + style.Key + "_" + body.Mesh.name, body.Mesh.bindposes);
            if (cut.Trimmed == null)
                cut.Trimmed = Trimmed(body, cut.Covered);
            return new FittedParts(leggings, cut.Trimmed);
        }

        /// <summary>The body triangles leggings ending at <paramref name="bottom"/> cover, one per body and lower end.</summary>
        private static BodyCut Cut(BodySurface body, float bottom)
        {
            if (!cuts.TryGetValue((body.Mesh, Mm(bottom)), out BodyCut cut))
                cuts[(body.Mesh, Mm(bottom))] = cut = new BodyCut(LegRegion.Covered(body, bottom));
            return cut;
        }

        /// <summary>How the texture wraps round a body: from its full length (waist to ankle) whatever the lower end, one per body.</summary>
        private static Unwrap UnwrapOf(BodySurface body)
        {
            if (!unwraps.TryGetValue(body.Mesh, out Unwrap unwrap))
                unwraps[body.Mesh] = unwrap = new Unwrap(body, Cut(body, LegRegion.Ankle).Covered);
            return unwrap;
        }

        private static Mesh Trimmed(BodySurface body, bool[] covered)
        {
            Mesh trimmed = Object.Instantiate(body.Mesh);
            trimmed.name = body.Mesh.name + "_ee_fitted";
            var kept = new List<int>(body.Triangles.Length);
            for (int t = 0; t < covered.Length; t++)
            {
                if (covered[t])
                    continue;
                kept.Add(body.Triangles[3 * t]);
                kept.Add(body.Triangles[3 * t + 1]);
                kept.Add(body.Triangles[3 * t + 2]);
            }
            trimmed.SetTriangles(kept, 0);
            return trimmed;
        }

        /// <summary>
        /// Drops every build no character in the scene wears, its meshes destroyed, and the bodies read for them; a build
        /// still worn stays, with the trimmed body it shares. Returns how many meshes were destroyed.
        /// </summary>
        internal static int Release(HashSet<Object> used)
        {
            var kept = new HashSet<Mesh>();
            int freed = ReleaseParts(used, kept) + ReleaseCuts(used, kept);
            surfaces.Clear();
            unwraps.Clear();
            return freed;
        }

        private static int ReleaseParts(HashSet<Object> used, HashSet<Mesh> kept)
        {
            int freed = 0;
            foreach ((LegStyle, Mesh, int, int) key in new List<(LegStyle, Mesh, int, int)>(made.Keys))
            {
                FittedParts parts = made[key];
                if (parts != null && used.Contains(parts.Leggings))
                {
                    kept.Add(parts.Body);
                    continue;
                }
                made.Remove(key);
                if (parts != null)
                {
                    Object.Destroy(parts.Leggings);
                    freed++;
                }
            }
            return freed;
        }

        private static int ReleaseCuts(HashSet<Object> used, HashSet<Mesh> kept)
        {
            int freed = 0;
            foreach ((Mesh, int) key in new List<(Mesh, int)>(cuts.Keys))
            {
                Mesh trimmed = cuts[key].Trimmed;
                if (trimmed != null && (used.Contains(trimmed) || kept.Contains(trimmed)))
                    continue;
                cuts.Remove(key);
                if (trimmed != null)
                {
                    Object.Destroy(trimmed);
                    freed++;
                }
            }
            return freed;
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

    /// <summary>
    /// One body cut at one lower end: the triangles the leggings cover, and the body drawn without them, made the first
    /// time a look of that length is worn and shared by every look of it.
    /// </summary>
    internal sealed class BodyCut
    {
        public BodyCut(bool[] covered)
        {
            Covered = covered;
        }

        public bool[] Covered { get; }
        public Mesh Trimmed { get; set; }
    }
}
