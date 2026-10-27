using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Broadside
{
    /// <summary>
    /// One mark of damage on a launched ship's rig (<see cref="KwRigDamage"/>), kept in the ship's ZDO as text
    /// (<see cref="KwRigMarks"/>): which blueprint piece and which part of it, and four numbers whose meaning goes by its type.
    /// <list type="bullet">
    /// <item>A notch in a spar (<see cref="Notch"/>): <see cref="A"/> how far along the spar (m), <see cref="B"/> the side it
    /// was cut from (degrees round the spar's axis, <see cref="KwRigSpar.Across"/>), <see cref="C"/> the share of the
    /// spar's section it cut (0 to 1), <see cref="D"/> half its length along the spar (m). Part: the spar
    /// (<see cref="KwRigSpar.Column"/>, a yard, a boom, a gaff).</item>
    /// <item>A hole in a sail (<see cref="Hole"/>): A and B where in the cloth's texture (0 to 1 each way), C its radius (m).
    /// Part: the sail's number on its mast.</item>
    /// <item>A chip off a post cap (<see cref="Chip"/>): A, B, C its middle in the cap's kit axes, D its radius (m). A chip
    /// low on the cap also cuts a notch in its base (the cap's own upright, <see cref="KwRigSpar.Column"/>).</item>
    /// <item>A chunk off a rudder's blade (<see cref="Chunk"/>): A its height on the blade (kit z), B where the boards are
    /// broken off from (kit x, aft is negative), C half its height, D 1 when the whole blade is gone.</item>
    /// <item>A mast, bowsprit or cap fallen (<see cref="Fall"/>): A where along its spar it broke (m), B the heading it fell
    /// to (degrees round the ship's up, from its bow), C the share of the section lost where it broke.</item>
    /// </list>
    /// </summary>
    public readonly struct KwRigMark
    {
        public const char Notch = 'n', Hole = 'h', Chip = 'c', Chunk = 'r', Fall = 'x';

        public KwRigMark(char type, int piece, int part, float a, float b, float c, float d, int seed)
        {
            (Type, Piece, Part, Seed) = (type, piece, part, seed);
            (A, B, C, D) = (a, b, c, d);
        }

        public char Type { get; }
        public int Piece { get; }
        public int Part { get; }
        public float A { get; }
        public float B { get; }
        public float C { get; }
        public float D { get; }
        public int Seed { get; }

        /// <summary>Whether it is a mark of this type on that piece and part.</summary>
        public bool On(char type, int piece, int part) => Type == type && Piece == piece && Part == part;

        /// <summary>The same mark with other numbers.</summary>
        public KwRigMark With(float a, float b, float c, float d) => new KwRigMark(Type, Piece, Part, a, b, c, d, Seed);

        public override string ToString() => string.Join(",", Type.ToString(), I(Piece), I(Part), F(A), F(B), F(C), F(D), I(Seed));

        private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private static string I(int v) => v.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A launched ship's rig damage as the text its ZDO keeps (<see cref="Key"/>): its marks (<see cref="KwRigMark"/>)
    /// joined by ";", each "type,piece,part,a,b,c,d,seed". At most <see cref="Most"/> marks, the oldest notches and holes
    /// let go first; a fall is never let go. A mark that does not read is left out.
    /// </summary>
    public static class KwRigMarks
    {
        public const string Key = "kw_rig_damage";
        public const int Most = 160;
        public static readonly int Hash = Key.GetStableHashCode();

        public static List<KwRigMark> Read(string? text)
        {
            List<KwRigMark> marks = new List<KwRigMark>();
            foreach (string part in (text ?? "").Split(';'))
            {
                if (TryRead(part, out KwRigMark mark))
                    marks.Add(mark);
            }
            return marks;
        }

        public static string Write(List<KwRigMark> marks)
        {
            Trim(marks);
            StringBuilder text = new StringBuilder();
            foreach (KwRigMark mark in marks)
                text.Append(text.Length > 0 ? ";" : "").Append(mark);
            return text.ToString();
        }

        // Past the most kept, the oldest marks that are not falls go first.
        private static void Trim(List<KwRigMark> marks)
        {
            for (int i = 0; marks.Count > Most && i < marks.Count;)
            {
                if (marks[i].Type == KwRigMark.Fall)
                    i++;
                else
                    marks.RemoveAt(i);
            }
        }

        private static bool TryRead(string text, out KwRigMark mark)
        {
            mark = default;
            string[] p = text.Split(',');
            if (p.Length != 8 || p[0].Length != 1 || "nhcrx".IndexOf(p[0][0]) < 0)
                return false;
            if (!Int(p[1], out int piece) || !Int(p[2], out int part) || !Int(p[7], out int seed))
                return false;
            if (!Num(p[3], out float a) || !Num(p[4], out float b) || !Num(p[5], out float c) || !Num(p[6], out float d))
                return false;
            mark = new KwRigMark(p[0][0], piece, part, a, b, c, d, seed);
            return true;
        }

        private static bool Int(string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        private static bool Num(string text, out float value) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
