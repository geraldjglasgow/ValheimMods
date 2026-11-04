using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One end of a hammock as built (<see cref="KwHammockSpec"/>), in the hammock's own frame (kit axes: x along the span,
    /// y across, z up; metres from its origin): a free-standing post on a floor, a deck or the ground, or an iron hook fixed
    /// into whatever else it was aimed at. <see cref="Rope"/> is where its ropes hang from (a post's peg tip, a hook's eye),
    /// <see cref="Base"/> where it stands or is fixed (a post's foot on its floor, a hook's point on the surface), and
    /// <see cref="Face"/> the way that surface faces (out of it; straight up for a post). Plain data.
    /// </summary>
    public readonly struct KwHammockMount
    {
        public KwHammockMount(bool post, Vector3 rope, Vector3 at, Vector3 face) => (Post, Rope, Base, Face) = (post, rope, at, face);

        /// <summary>A free-standing post; else an iron hook.</summary>
        public bool Post { get; }

        public Vector3 Rope { get; }
        public Vector3 Base { get; }
        public Vector3 Face { get; }

        /// <summary>A post's peg over its foot.</summary>
        public float Peg => Rope.z - Base.z;

        public string Says => Post ? "a post" : "an iron hook";
    }

    /// <summary>
    /// A hammock's shape, saved in its ZDO (<see cref="Key"/>) by the game that hangs it and read on every peer, so every
    /// screen and every reload builds the same (<see cref="KwHammock"/>): its two ends (<see cref="A"/> at -x, <see cref="B"/>
    /// at +x, their rope points on the line y = 0) and the spreader bars' height (<see cref="Bars"/>). The frame's origin is
    /// on the floor (the ground, a deck) under the middle of the line between the two rope points, its x the span's level
    /// way, its z up (a ship's up aboard). The text: <c>hm1|bars|P;rope;base;face|H;rope;base;face</c> (P a post, H a hook,
    /// each point x,y,z), metres to the millimetre. A hammock with no text is <see cref="Standard"/>. Plain data.
    /// </summary>
    public sealed class KwHammockSpec
    {
        public const string Key = "bs_hammock";
        private const string Head = "hm1";
        private static readonly Vector3 Up = new Vector3(0f, 0f, 1f);

        public KwHammockSpec(KwHammockMount a, KwHammockMount b, float bars) => (A, B, Bars) = (a, b, bars);

        public KwHammockMount A { get; }
        public KwHammockMount B { get; }

        /// <summary>The spreader bars' height over the origin.</summary>
        public float Bars { get; }

        /// <summary>The prefab's hammock (its look and icon) and one with no saved shape: two posts 2.6 m apart, pegs at 1.25 m.</summary>
        public static KwHammockSpec Standard { get; } = new KwHammockSpec(Post(-1f), Post(1f), KwHammockRules.Standard);

        /// <summary>The middle of the line between the rope points: where the sling hangs from and swings about.</summary>
        public Vector3 Middle => (A.Rope + B.Rope) * 0.5f;

        /// <summary>The rope points' distance along the span.</summary>
        public float Span => B.Rope.x - A.Rope.x;

        /// <summary>The cloth's length between these ends.</summary>
        public float Cloth => KwHammockShape.ClothFor(Span);

        /// <summary>Whether either end hangs from a hook.</summary>
        public bool Hooks => !A.Post || !B.Post;

        public string Says => A.Post && B.Post ? "two posts" : A.Post || B.Post ? "a post and an iron hook" : "two iron hooks";

        private static KwHammockMount Post(float end)
        {
            float half = KwHammockRules.StandardApart * 0.5f;
            return new KwHammockMount(true, new Vector3(end * (half - KwHammockParts.PegIn), 0f, KwHammockRules.Hook), new Vector3(end * half, 0f, 0f), Up);
        }

        public override string ToString() => string.Join("|", Head, F(Bars), Text(A), Text(B));

        private static string Text(KwHammockMount m) => string.Join(";", m.Post ? "P" : "H", V(m.Rope), V(m.Base), V(m.Face));

        private static string V(Vector3 v) => F(v.x) + "," + F(v.y) + "," + F(v.z);

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>A saved shape read back; false (and the standard) for no text or one that is not a hammock's.</summary>
        public static bool TryParse(string? text, out KwHammockSpec spec)
        {
            spec = Standard;
            string[] parts = (text ?? "").Split('|');
            if (parts.Length != 4 || parts[0] != Head || !Number(parts[1], out float bars))
                return false;
            if (!Mount(parts[2], out KwHammockMount a) || !Mount(parts[3], out KwHammockMount b) || b.Rope.x - a.Rope.x < 0.5f)
                return false;
            spec = new KwHammockSpec(a, b, bars);
            return true;
        }

        private static bool Mount(string text, out KwHammockMount mount)
        {
            mount = default;
            string[] parts = text.Split(';');
            if (parts.Length != 4 || parts[0] != "P" && parts[0] != "H")
                return false;
            if (!Point(parts[1], out Vector3 rope) || !Point(parts[2], out Vector3 at) || !Point(parts[3], out Vector3 face))
                return false;
            mount = new KwHammockMount(parts[0] == "P", rope, at, face.sqrMagnitude > 1e-6f ? face.normalized : Up);
            return true;
        }

        private static bool Point(string text, out Vector3 point)
        {
            point = Vector3.zero;
            string[] n = text.Split(',');
            if (n.Length != 3 || !Number(n[0], out float x) || !Number(n[1], out float y) || !Number(n[2], out float z))
                return false;
            point = new Vector3(x, y, z);
            return true;
        }

        private static bool Number(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
