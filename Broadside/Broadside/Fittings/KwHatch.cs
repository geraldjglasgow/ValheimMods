using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Hatches, placed on a deck to go below it, in three sizes: small (a 1 m square opening), medium (1.5 m) and large
    /// (3 m along the deck by 2 m across, closed by two doors). Each is a coaming, a frame of boards round the opening
    /// standing <see cref="Above"/> over the deck and reaching <see cref="Below"/> under its top to cover the deck's cut
    /// edge, and its lid: one leaf hinged on the aft edge, or the large hatch's two doors hinged on the port and
    /// starboard edges, each a "leaf" child on its hinge line that <see cref="KwHatchCover"/> swings open and shut. The
    /// deck under a hatch leaves its opening out (<see cref="KwDeckHoles"/>); a ladder hangs from its edge
    /// (<see cref="KwLadderTool"/>). Kit axes: x forward, y left, z up; the origin on the deck's top at the opening's middle.
    /// </summary>
    public static class KwHatch
    {
        public const string Prefix = "KW_hatch_";
        public const float Rim = 0.1f, Above = 0.15f, Below = 0.12f, Lid = 0.05f;

        // Each size's opening: its length along the deck (x) and its width across it (y).
        private static readonly Dictionary<string, Vector2> Openings = new Dictionary<string, Vector2>
        {
            ["small"] = new Vector2(1f, 1f), ["medium"] = new Vector2(1.5f, 1.5f), ["large"] = new Vector2(3f, 2f),
        };

        public static IEnumerable<KwDef> All()
        {
            yield return Def("small", "Hatch, small (1 × 1 m opening)", Openings["small"], false);
            yield return Def("medium", "Hatch, medium (1.5 × 1.5 m opening)", Openings["medium"], false);
            yield return Def("large", "Hatch, large (3 × 2 m opening, two doors)", Openings["large"], true);
        }

        /// <summary>A hatch prefab's opening (along the deck, across it); null for any other prefab.</summary>
        public static Vector2? Opening(string prefab) =>
            prefab.StartsWith(Prefix, System.StringComparison.Ordinal) && Openings.TryGetValue(prefab.Substring(Prefix.Length), out Vector2 size) ? size : (Vector2?)null;

        // opening: its length along the deck (x) and its width across it (y)
        private static KwDef Def(string size, string label, Vector2 opening, bool doors) =>
            new KwDef(Prefix + size, label, "fitting") { Wood = true, Shape = b => Coaming(b, opening), Setup = p => Setup(p, opening, doors) };

        private static void Setup(GameObject prefab, Vector2 opening, bool doors)
        {
            float hx = opening.x / 2f + Rim, hy = opening.y / 2f + Rim;
            if (doors)
            {
                Leaf(prefab, "leaf0", new Vector3(0f, hy, Above), new Vector3(-hx, -hy, 0f), new Vector3(hx, 0f, Lid));
                Leaf(prefab, "leaf1", new Vector3(0f, -hy, Above), new Vector3(-hx, 0f, 0f), new Vector3(hx, hy, Lid));
            }
            else
                Leaf(prefab, "leaf0", new Vector3(-hx, 0f, Above), new Vector3(0f, -hy, 0f), new Vector3(2f * hx, hy, Lid));
            KwHatchCover cover = prefab.AddComponent<KwHatchCover>();
            cover.Opening = opening;
            cover.Doors = doors;
            prefab.GetComponent<Piece>().m_description =
                "Place on a deck: the deck opens under it. E opens and shuts it; hang a ladder from it with the ladder tool.";
        }

        // Four boards round the opening: the fore and aft ones full width, the side ones between them.
        private static void Coaming(MeshBuilder b, Vector2 opening)
        {
            float hx = opening.x / 2f, hy = opening.y / 2f;
            foreach (float s in new[] { -1f, 1f })
            {
                float x0 = s > 0f ? hx : -hx - Rim, y0 = s > 0f ? hy : -hy - Rim;
                Solid.Box(b, new Vector3(x0, -hy - Rim, -Below), new Vector3(x0 + Rim, hy + Rim, Above));
                Solid.Box(b, new Vector3(-hx, y0, -Below), new Vector3(hx, y0 + Rim, Above));
            }
        }

        // A leaf: a child on its hinge line holding a board panel from min to max (kit, from the hinge) with two battens
        // across it, and a box to walk on while it is shut.
        private static void Leaf(GameObject prefab, string name, Vector3 hinge, Vector3 min, Vector3 max)
        {
            GameObject leaf = new GameObject(name) { layer = prefab.layer };
            leaf.transform.SetParent(prefab.transform, false);
            leaf.transform.localPosition = KwMesh.ToUnity(hinge);
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            Solid.Box(b, min, max);
            foreach (float t in new[] { 0.2f, 0.8f })
            {
                float x = Mathf.Lerp(min.x, max.x, t);
                Solid.Box(b, new Vector3(x - 0.05f, min.y + 0.04f, max.z), new Vector3(x + 0.05f, max.y - 0.04f, max.z + 0.03f));
            }
            leaf.AddComponent<MeshFilter>().sharedMesh = b.ToMesh(prefab.name + "_" + name);
            MeshRenderer renderer = leaf.AddComponent<MeshRenderer>();
            if (KwPrefabs.Planks != null)
                renderer.sharedMaterial = KwPrefabs.Planks;
            Vector3 a = KwMesh.ToUnity(min), c = KwMesh.ToUnity(max);
            BoxCollider box = leaf.AddComponent<BoxCollider>();
            box.center = (a + c) / 2f;
            box.size = new Vector3(Mathf.Abs(c.x - a.x), Mathf.Abs(c.y - a.y), Mathf.Abs(c.z - a.z));
        }
    }
}
