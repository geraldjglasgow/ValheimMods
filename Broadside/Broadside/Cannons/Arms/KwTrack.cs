using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One pose on a moving part's track (<see cref="KwTrack"/>): when (0 to 1 along the track), how far the part is moved
    /// from where it was drawn (kit axes: x forward, y left, z up, metres), turned about its pivot (degrees about the kit's
    /// x, then y, then z, right-handed: +z swings forward toward the left, +y swings forward down, +x rolls the left side
    /// up; more than 360 spins it round), scaled along its kit axes, whether it is shown from this key to the next, and
    /// whether the move into it from the key before is eased (slow at both ends) or steady.
    /// </summary>
    public readonly struct KwKey
    {
        public static readonly KwKey Rest = new KwKey(0f, Vector3.zero, Vector3.zero, Vector3.one, true, true);

        public KwKey(float at, Vector3 move, Vector3 turn, Vector3 scale, bool shown, bool eased)
        {
            (At, Move, Turn, Scale, Shown, Eased) = (at, move, turn, scale, shown, eased);
        }

        public float At { get; }
        public Vector3 Move { get; }
        public Vector3 Turn { get; }
        public Vector3 Scale { get; }
        public bool Shown { get; }
        public bool Eased { get; }

        /// <summary>The pose a fraction of the way from this key to the next, eased as the next says; shown as this one says.</summary>
        public KwKey Toward(KwKey next, float t)
        {
            float f = next.Eased ? Mathf.SmoothStep(0f, 1f, t) : t;
            return new KwKey(Mathf.Lerp(At, next.At, t), Vector3.Lerp(Move, next.Move, f), Vector3.Lerp(Turn, next.Turn, f), Vector3.Lerp(Scale, next.Scale, f), Shown, true);
        }
    }

    /// <summary>
    /// A moving part's motion over a shot or a reload (<see cref="KwArmMover.Fire"/>, <see cref="KwArmMover.Reload"/>): a
    /// few keys from 0 (its start) to 1 (its end), sampled anywhere along it. Before the first key the part holds the first
    /// key's pose, after the last the last's, between two it goes from one to the next; two keys at one moment make a jump.
    /// A track with no keys leaves the part where it was drawn, shown.
    /// </summary>
    public sealed class KwTrack
    {
        private readonly List<KwKey> keys = new List<KwKey>();

        public IReadOnlyList<KwKey> Keys => keys;

        /// <summary>Adds a key (kept in order of its moment; a key at the same moment as another comes after it).</summary>
        public KwTrack Key(float at, Vector3 move = default, Vector3 turn = default, Vector3? scale = null, bool shown = true, bool eased = true)
        {
            KwKey key = new KwKey(Mathf.Clamp01(at), move, turn, scale ?? Vector3.one, shown, eased);
            int i = keys.Count;
            while (i > 0 && keys[i - 1].At > key.At)
                i--;
            keys.Insert(i, key);
            return this;
        }

        /// <summary>The pose at a moment from 0 to 1 along the track.</summary>
        public KwKey Sample(float t)
        {
            if (keys.Count == 0)
                return KwKey.Rest;
            if (t < keys[0].At)
                return keys[0];
            for (int i = 1; i < keys.Count; i++)
            {
                if (t >= keys[i].At)
                    continue;
                KwKey from = keys[i - 1], to = keys[i];
                return from.Toward(to, (t - from.At) / Mathf.Max(1e-5f, to.At - from.At));
            }
            return keys[keys.Count - 1];
        }
    }
}
