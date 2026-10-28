using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The meshes generated for ships' parts, kept for the session (the user's choice, 2026-10-27, after a Little Gull's
    /// parts took 20 to 50 ms each to generate, for its preview and again for the ship built from it): a saved ship's
    /// preview, the ship launched from it, its look each time it loads and every ship of the same design generate a part
    /// once. A part is found by everything its meshes are made from: its tool, its saved text, its tier, whether its covers
    /// are drawn shut, and its seat on its ship's keels in its own frame (<see cref="KwSeat"/>: a level and a keel line;
    /// a rib's climb), which is the same in a preview's frame and a launched ship's. Each use gets its own copy, which the
    /// caller combines, cuts or lets go as before; a deck with hatch holes cut (a launched ship's look) is made each time,
    /// and so is anything not kept readable. At most <see cref="Most"/> vertices are kept; past that the oldest go.
    /// Clients only (the looks are drawn only there).
    /// </summary>
    public static class KwPartMeshes
    {
        private const int Most = 1500000;

        private sealed class Kept
        {
            public Kept(List<(Material? Material, Mesh Mesh)> meshes, KwDef? def) => (Meshes, Def) = (meshes, def);

            public List<(Material? Material, Mesh Mesh)> Meshes { get; }
            public KwDef? Def { get; }
            public int Vertices => Meshes.Sum(m => m.Mesh.vertexCount);
        }

        private static readonly Dictionary<string, Kept> kept = new Dictionary<string, Kept>();
        private static readonly Queue<string> order = new Queue<string>();
        private static int vertices;

        /// <summary><see cref="KwGenerated.MeshFor"/> for a part not placed in the world, from the kept meshes when it was made before.</summary>
        public static Mesh? MeshFor(string tool, string saved, out KwDef? def, Func<KwDef, List<Rect>>? holes, KwBiomeTier tier, KwSeatAt? seat)
        {
            if (holes != null)
                return KwGenerated.MeshFor(tool, saved, null, out def, holes, tier, seat);
            string key = Key("mesh", tool, saved, tier, seat, false);
            if (!kept.TryGetValue(key, out Kept found))
            {
                Mesh? made = KwGenerated.MeshFor(tool, saved, null, out def, null, tier, seat);
                if (!Keep(key, made != null ? new List<(Material?, Mesh)> { (null, made) } : new List<(Material?, Mesh)>(), def))
                    return made;
                found = kept[key];
            }
            def = found.Def;
            return found.Meshes.Count > 0 ? Copy(found.Meshes[0].Mesh) : null;
        }

        /// <summary><see cref="KwOpeningLooks.Dressed"/>, from the kept meshes when it was made before.</summary>
        public static List<(Material? Material, Mesh Mesh)> Dressed(string tool, string saved, bool shut, KwBiomeTier tier, KwSeatAt? seat)
        {
            string key = Key("openings", tool, saved, tier, seat, shut);
            if (!kept.TryGetValue(key, out Kept found))
            {
                List<(Material? Material, Mesh Mesh)> made = KwOpeningLooks.Dressed(tool, saved, shut, tier, seat);
                if (!Keep(key, made, null))
                    return made;
                found = kept[key];
            }
            return found.Meshes.ConvertAll(m => (m.Material, Copy(m.Mesh)));
        }

        /// <summary><see cref="KwHouses.Looks"/> (a cabin's), from the kept meshes when it was made before.</summary>
        public static List<(Material Material, Mesh Mesh)> Looks(string tool, string saved, KwBiomeTier tier)
        {
            string key = Key("house", tool, saved, tier, null, false);
            if (!kept.TryGetValue(key, out Kept found))
            {
                List<(Material Material, Mesh Mesh)> made = KwHouses.Looks(tool, saved, tier);
                if (!Keep(key, made.ConvertAll(m => ((Material?)m.Material, m.Mesh)), null))
                    return made;
                found = kept[key];
            }
            return found.Meshes.ConvertAll(m => (m.Material!, Copy(m.Mesh)));
        }

        // The made meshes kept as they are (each use copies them); false, and nothing kept, when one is not readable (the
        // caller then uses them as made).
        private static bool Keep(string key, List<(Material? Material, Mesh Mesh)> meshes, KwDef? def)
        {
            if (!meshes.TrueForAll(m => m.Mesh != null && m.Mesh.isReadable))
                return false;
            Kept made = new Kept(meshes, def);
            kept[key] = made;
            order.Enqueue(key);
            vertices += made.Vertices;
            while (vertices > Most && order.Count > 1)
                Forget(order.Dequeue());
            return true;
        }

        private static void Forget(string key)
        {
            if (!kept.TryGetValue(key, out Kept old))
                return;
            kept.Remove(key);
            vertices -= old.Vertices;
            foreach ((Material? _, Mesh mesh) in old.Meshes)
                Object.Destroy(mesh);
        }

        private static Mesh Copy(Mesh mesh)
        {
            if (!mesh.isReadable)
                return mesh;
            Mesh copy = Object.Instantiate(mesh);
            copy.name = mesh.name;
            return copy;
        }

        private static string Key(string kind, string tool, string saved, KwBiomeTier tier, KwSeatAt? seat, bool shut) =>
            new StringBuilder(kind).Append('|').Append(tool).Append('|').Append(tier.Id).Append('|').Append(shut ? 's' : 'o')
                .Append('|').Append(SeatKey(saved, seat)).Append('|').Append(saved).ToString();

        // The seat as the part's own frame sees it, to a tenth of a millimetre: a rib's climb, a span's level and keel line.
        private static string SeatKey(string saved, KwSeatAt? seat)
        {
            if (seat is not KwSeatAt at)
                return "-";
            StringBuilder key = new StringBuilder(R(at.Climb));
            if (!KwSpan.TryParse(saved, out KwSpan span) || at.Keels.Seat(at.Origin, at.Forward, span) is not KwSeat placed)
                return key.ToString();
            key.Append('|').Append(R(placed.Level));
            foreach (Vector2 p in placed.Keel ?? new List<Vector2>())
                key.Append(';').Append(R(p.x)).Append(',').Append(R(p.y));
            return key.ToString();
        }

        private static string R(float value) => value.ToString("0.0000", CultureInfo.InvariantCulture);
    }
}
