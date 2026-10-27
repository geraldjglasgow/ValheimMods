using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ladder hanging straight down from a hatch's edge, or from a deck's or a cabin roof's open edge, to the floor below
    /// (made by <see cref="KwLadderTool"/>, climbed with <see cref="KwLadderClimb"/>): two rails standing off the edge, from
    /// the floor to just over the deck's top, and round rungs between them every <see cref="Rung"/> from the floor up. Its
    /// length travels in its ZDO as "ladder|length" under the generated parts' key, so blueprints keep it. Kit axes: the
    /// origin at the ladder's top on the edge at the deck's top; x away from the edge (into a hatch's opening, out over an
    /// open edge's drop), y across the ladder, z up.
    /// </summary>
    public static class KwLadder
    {
        public const float Rung = 0.3f, MinLength = 0.6f, MaxLength = 6f;
        private const float Half = 0.22f, Off = 0.03f, Rail = 0.07f, RailTop = 0.1f, TopRung = -0.1f;
        private const string Tag = "ladder|";

        public static string Save(float length) => Tag + length.ToString("0.00", CultureInfo.InvariantCulture);

        public static bool TryLength(string saved, out float length)
        {
            length = 0f;
            return saved.StartsWith(Tag) &&
                float.TryParse(saved.Substring(Tag.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out length) &&
                length >= MinLength && length <= MaxLength + 0.5f;
        }

        public static Mesh? Build(string saved) => TryLength(saved, out float length) ? Mesh(length) : null;

        public static Mesh Mesh(float length)
        {
            MeshBuilder builder = new MeshBuilder(Vector3.zero);
            Shape(builder, length);
            return builder.ToMesh("KW_gen_ladder");
        }

        public static void Shape(MeshBuilder b, float length)
        {
            foreach (float y in new[] { -Half, Half })
                Solid.Box(b, new Vector3(Off, y - 0.03f, -length), new Vector3(Off + Rail, y + 0.03f, RailTop));
            float x = Off + Rail / 2f;
            for (float z = -length + Rung; z <= TopRung + 0.01f; z += Rung)
                Solid.Log(b, new Vector3(x, -Half, z), new Vector3(x, Half, z), 0.022f, 6);
        }
    }
}
