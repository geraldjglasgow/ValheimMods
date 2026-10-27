using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rib's size: its beam (the hull's full width at the top) and its height above the keel line. The joint table's
    /// seven sizes are presets (<see cref="Preset"/>) and keep their number when written ("7"); any other frame is written
    /// as "beam x height" ("4.5x6"), so spans saved before custom ribs still read; standing on a keel thinner or thicker
    /// than the standard keel, its keel's width follows ("4.5x6x0.16"), since the hull's sides start at the keel's sides. Two
    /// frames are the same when all their numbers agree to a centimetre.
    /// </summary>
    public readonly struct KwFrame
    {
        public const float MinBeam = 1.5f, MaxBeam = 14f, MinHeight = 0.8f, MaxHeight = 10f;

        public KwFrame(float beam, float height, int size = 0, float keelHalf = KwKeel.HalfWidth)
        {
            Beam = beam;
            Height = height;
            Size = Mathf.Abs(keelHalf - KwKeel.HalfWidth) < 0.005f ? size : 0;
            KeelHalf = keelHalf;
        }

        /// <summary>Half the width of the keel the rib stands on: where the hull's sides start.</summary>
        public float KeelHalf { get; }

        /// <summary>The same frame on a keel of another width.</summary>
        public KwFrame OnKeel(float keelHalf) => new KwFrame(Beam, Height, Size, keelHalf);

        public float Beam { get; }
        public float Height { get; }

        /// <summary>The joint table's size this frame is (1 to 7), or 0 for a frame of its own.</summary>
        public int Size { get; }

        public static KwFrame Preset(int size) => KwJoints.Preset(size);

        public bool Same(KwFrame other) =>
            Mathf.Abs(Beam - other.Beam) < 0.01f && Mathf.Abs(Height - other.Height) < 0.01f && Mathf.Abs(KeelHalf - other.KeelHalf) < 0.005f;

        public override string ToString()
        {
            if (Size > 0)
                return Size.ToString(CultureInfo.InvariantCulture);
            string text = F(Beam) + "x" + F(Height);
            return Mathf.Abs(KeelHalf - KwKeel.HalfWidth) < 0.005f ? text : text + "x" + F(KeelHalf * 2f);
        }

        public string Label => $"{Beam:0.##} m wide, {Height:0.##} m tall";

        private static string F(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        public static bool TryParse(string text, out KwFrame frame)
        {
            frame = default;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int size))
            {
                if (size < 1 || size > 7)
                    return false;
                frame = Preset(size);
                return true;
            }
            string[] parts = text.Split('x');
            float keel = KwKeel.HalfWidth * 2f;
            if ((parts.Length != 2 && parts.Length != 3) || !Num(parts[0], out float beam) || !Num(parts[1], out float height)
                || (parts.Length == 3 && !Num(parts[2], out keel)))
                return false;
            frame = new KwFrame(beam, height, 0, keel / 2f);
            return beam >= MinBeam - 0.01f && beam <= MaxBeam + 0.01f && height >= MinHeight - 0.01f && height <= MaxHeight + 0.01f;
        }

        private static bool Num(string text, out float value) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
