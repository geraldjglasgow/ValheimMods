using System;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// One blueprint piece as smart select sees it: its drawn footprint in the blueprint frame (a rectangle turned
    /// with the piece around <see cref="X"/>, <see cref="Z"/>), its bottom and top, and what kind of part it is: a roof
    /// (shelters what stands under it) or a floor (a storey stands on it). Plain arithmetic on the frame, no game
    /// objects, so the same code runs in the offline tests.
    /// </summary>
    internal sealed class SmartBox
    {
        /// <summary>The footprint centre in the frame.</summary>
        public float X;
        public float Z;

        /// <summary>The piece's own x axis (across) and z axis (along) in the frame.</summary>
        public float AcrossX;
        public float AcrossZ;
        public float AlongX;
        public float AlongZ;

        /// <summary>Half the footprint along the piece's own x and z.</summary>
        public float HalfX;
        public float HalfZ;

        public float Bottom;
        public float Top;
        public bool Roof;
        public bool Floor;

        /// <summary>The distance from the centre to a footprint corner.</summary>
        public float Reach => Mathf.Sqrt(HalfX * HalfX + HalfZ * HalfZ);

        /// <summary>The piece's box from its drawn bounds at yaw 0 (pivot at the origin); a one-metre cube on the pivot when unknown.</summary>
        public static SmartBox Of(BlueprintPiece piece, Bounds? drawn)
        {
            Bounds b = drawn ?? new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);
            float yaw = piece.Yaw * Mathf.Deg2Rad, cos = Mathf.Cos(yaw), sin = Mathf.Sin(yaw);
            SmartBox box = new SmartBox
            {
                X = piece.X + b.center.x * cos + b.center.z * sin,
                Z = piece.Z - b.center.x * sin + b.center.z * cos,
                AcrossX = cos, AcrossZ = -sin, AlongX = sin, AlongZ = cos,
                HalfX = b.extents.x, HalfZ = b.extents.z,
                Bottom = piece.Y + b.min.y, Top = piece.Y + b.max.y,
            };
            box.Roof = IsRoof(piece.Prefab);
            box.Floor = box.HalfX >= 0.45f && box.HalfZ >= 0.45f && (b.size.y <= 0.6f || Has(piece.Prefab, "floor"));
            return box;
        }

        /// <summary>The frame point lies in the footprint grown by <paramref name="grow"/> on every side.</summary>
        public bool Covers(float x, float z, float grow)
        {
            float dx = x - X, dz = z - Z;
            return Mathf.Abs(dx * AcrossX + dz * AcrossZ) <= HalfX + grow && Mathf.Abs(dx * AlongX + dz * AlongZ) <= HalfZ + grow;
        }

        /// <summary>Half the footprint's extent along the frame's x (<paramref name="onX"/>) or z axis.</summary>
        public float Spread(bool onX) => onX
            ? Mathf.Abs(AcrossX) * HalfX + Mathf.Abs(AlongX) * HalfZ
            : Mathf.Abs(AcrossZ) * HalfX + Mathf.Abs(AlongZ) * HalfZ;

        /// <summary>How much of the height band from <paramref name="low"/> to <paramref name="high"/> the piece fills.</summary>
        public float Overlap(float low, float high) => Mathf.Min(Top, high) - Mathf.Max(Bottom, low);

        /// <summary>Two boxes touch: their heights and their footprints come within <paramref name="gap"/> (separating axes).</summary>
        public bool Touches(SmartBox o, float gap)
        {
            if (Bottom > o.Top + gap || o.Bottom > Top + gap)
                return false;
            float dx = o.X - X, dz = o.Z - Z, reach = Reach + o.Reach + gap;
            if (dx * dx + dz * dz > reach * reach)
                return false;
            return Apart(o, AcrossX, AcrossZ) <= gap && Apart(o, AlongX, AlongZ) <= gap
                && Apart(o, o.AcrossX, o.AcrossZ) <= gap && Apart(o, o.AlongX, o.AlongZ) <= gap;
        }

        /// <summary>The gap between the two footprints measured along one axis (0 or less when they overlap on it).</summary>
        private float Apart(SmartBox o, float ax, float az)
        {
            float d = Mathf.Abs((o.X - X) * ax + (o.Z - Z) * az);
            return d - Extent(ax, az) - o.Extent(ax, az);
        }

        private float Extent(float ax, float az) => Mathf.Abs(AcrossX * ax + AcrossZ * az) * HalfX + Mathf.Abs(AlongX * ax + AlongZ * az) * HalfZ;

        /// <summary>Roofs by name (the game's thatch, darkwood and other roofs); a gable wall ("wood_wall_roof") is a wall.</summary>
        private static bool IsRoof(string prefab) => Has(prefab, "roof") && !Has(prefab, "wall");

        private static bool Has(string prefab, string word) => prefab != null && prefab.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
