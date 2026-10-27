using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The name board (<see cref="Prefab"/>): a carved board to hang on the stern, a banner of plank wood 1.64 m across
    /// and 0.36 m tall whose ends run to points, with a raised frame round both faces in a darker stain, and the ship's
    /// name on both faces (<see cref="KwNameText"/>). Its origin is the middle of its back face, its front (+x) is the side
    /// turned out of the ship; the rotate key turns it. E on it opens the game's own text box (<c>TextInput</c>, as a sign
    /// does) for the ship's name, saved in its ZDO (<see cref="Key"/>; on the dock <see cref="KwNameBoardPiece"/>, on a
    /// launched ship in the ship's, <see cref="KwShipName"/>) and in blueprints with it. Costs one wood built with a ship,
    /// as a post cap does. Kit axes: x out of the face, y across, z up, metres.
    /// </summary>
    public static class KwNameBoard
    {
        public const string Prefab = "KW_nameboard", Key = "kw_name";
        public static readonly int Hash = Key.GetStableHashCode();

        /// <summary>The longest name, in letters.</summary>
        public const int Longest = 32;

        /// <summary>The board's front face (x), and the space for the name inside the frame (across, up).</summary>
        public const float Face = 0.05f, TextWide = 1.3f, TextHigh = 0.24f;

        private const float HalfWide = 0.7f, HalfHigh = 0.18f, Point = 0.12f, Rim = 0.045f, Raised = 0.018f;
        private static Material? stain;

        public static IEnumerable<KwDef> All()
        {
            yield return new KwDef(Prefab, "Name board", "fitting") { Shape = Board, Wood = true, Setup = Setup };
        }

        /// <summary>A name as the board keeps it: one line, trimmed, at most <see cref="Longest"/> letters.</summary>
        public static string Clean(string? text)
        {
            string line = (text ?? "").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
            return line.Length > Longest ? line.Substring(0, Longest).Trim() : line;
        }

        // The board's outline across and up (y, z), anticlockwise seen from the front: a banner with pointed ends.
        private static Vector2[] Outline() => new[]
        {
            new Vector2(-HalfWide - Point, 0f), new Vector2(-HalfWide, -HalfHigh), new Vector2(HalfWide, -HalfHigh),
            new Vector2(HalfWide + Point, 0f), new Vector2(HalfWide, HalfHigh), new Vector2(-HalfWide, HalfHigh),
        };

        private static void Board(MeshBuilder b) => Solid.Prism(b, Outline(), 0f, Face);

        // The frame round both faces, raised a little off them, each side of the outline a strip inside it.
        private static void Frame(MeshBuilder b)
        {
            Vector2[] outer = Outline(), inner = Inset(outer, Rim);
            for (int i = 0; i < outer.Length; i++)
            {
                int j = (i + 1) % outer.Length;
                Vector2[] strip = { outer[i], outer[j], inner[j], inner[i] };
                Solid.Prism(b, strip, Face, Face + Raised);
                Solid.Prism(b, strip, -Raised, 0f);
            }
        }

        /// <summary>A convex outline moved in by a distance square to each side (anticlockwise corners).</summary>
        public static Vector2[] Inset(Vector2[] outline, float distance)
        {
            Vector2[] inner = new Vector2[outline.Length];
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 before = outline[(i + outline.Length - 1) % outline.Length], at = outline[i], after = outline[(i + 1) % outline.Length];
                Vector2 n1 = Inward(at - before), n2 = Inward(after - at), miter = (n1 + n2).normalized;
                inner[i] = at + miter * (distance / Mathf.Max(0.2f, Vector2.Dot(miter, n1)));
            }
            return inner;
        }

        // Inward of a side running anticlockwise: its left.
        private static Vector2 Inward(Vector2 side) => new Vector2(-side.y, side.x).normalized;

        // The frame in its darker stain, the board's E (KwNameBoardPiece) and its description.
        private static void Setup(GameObject prefab)
        {
            GameObject frame = new GameObject("frame") { layer = prefab.layer };
            frame.transform.SetParent(prefab.transform, false);
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            Frame(b);
            frame.AddComponent<MeshFilter>().sharedMesh = b.ToMesh(Prefab + "_frame");
            MeshRenderer renderer = frame.AddComponent<MeshRenderer>();
            if (Stain() is Material dark)
                renderer.sharedMaterial = dark;
            prefab.AddComponent<KwNameBoardPiece>();
            prefab.GetComponent<Piece>().m_description = "Hang it on the stern, its front turned out (the rotate key turns it). E on it names the ship.";
        }

        // The plank wood stained dark for the frame (made once).
        private static Material? Stain()
        {
            if (stain != null || KwPrefabs.Planks == null)
                return stain;
            stain = new Material(KwPrefabs.Planks) { name = "kw_nameboard_frame" };
            if (stain.HasProperty("_Color"))
                stain.color = new Color(0.42f, 0.33f, 0.26f);
            return stain;
        }
    }
}
