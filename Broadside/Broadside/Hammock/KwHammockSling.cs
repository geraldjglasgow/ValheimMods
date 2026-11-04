using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The sling of a hammock (<see cref="KwHammockSpec"/>), plain maths in the hammock's frame (kit axes): a spreader bar
    /// across at each end of the cloth (<see cref="Length"/> long, centred under the middle of the rope line, level at the
    /// spec's bar height), two ropes from each end's rope point down to its bar's ends, and the cloth between the bars
    /// sagging along its length on a catenary (solved once for its sag) and cradling across where it sags. The sling's own
    /// frame (<see cref="In"/>) sits on the middle of the rope line, its z along the line (<see cref="Rest"/>, tilted with
    /// it when the ends hang at different heights), so turning it about its z swings it about the line between the hooks
    /// (<see cref="KwHammock"/>) while its rope ends stay put. The same numbers make the meshes, the colliders, the checks
    /// and where a sleeper lies, so all agree.
    /// </summary>
    public sealed class KwHammockSling
    {
        private static readonly float[] Ends = { -1f, 1f };
        private readonly float a;

        public KwHammockSling(KwHammockSpec spec)
        {
            Spec = spec;
            Length = spec.Cloth;
            a = Catenary(Length * 0.5f, KwHammockShape.Sag);
            Middle = spec.Middle;
            Vector3 line = KwMesh.ToUnity(spec.B.Rope - spec.A.Rope);
            Rest = line.sqrMagnitude > 1e-6f ? Quaternion.FromToRotation(Vector3.forward, line.normalized) : Quaternion.identity;
        }

        public KwHammockSpec Spec { get; }

        /// <summary>The cloth's length.</summary>
        public float Length { get; }

        /// <summary>The middle of the rope line (kit, the hammock's frame): the sling's own origin.</summary>
        public Vector3 Middle { get; }

        /// <summary>The sling's turn at rest under the model (Unity): its z along the rope line.</summary>
        public Quaternion Rest { get; }

        /// <summary>A point of the hammock's frame in the sling's own (both kit axes).</summary>
        public Vector3 In(Vector3 kit) => KwHammockShape.Kit(Quaternion.Inverse(Rest) * KwMesh.ToUnity(kit - Middle));

        /// <summary>A point on the cloth, the hammock's frame: s along the span from its middle (within half its length), w across.</summary>
        public Vector3 ClothPoint(float s, float w)
        {
            float dip = Dip(s), across = 2f * w / KwHammockShape.Width;
            return new Vector3(s, w, Spec.Bars - KwHammockShape.Sag * dip + KwHammockShape.Curl * dip * across * across);
        }

        /// <summary>Each spreader bar's two ends, the hammock's frame.</summary>
        public IEnumerable<(Vector3 From, Vector3 To)> Bars()
        {
            foreach (float end in Ends)
                yield return (new Vector3(end * Length * 0.5f, -KwHammockShape.Bar, Spec.Bars), new Vector3(end * Length * 0.5f, KwHammockShape.Bar, Spec.Bars));
        }

        /// <summary>The four ropes, each from its end's rope point to an end of its bar, the hammock's frame.</summary>
        public IEnumerable<(Vector3 From, Vector3 To)> Ropes()
        {
            foreach (float end in Ends)
            {
                Vector3 hook = end < 0f ? Spec.A.Rope : Spec.B.Rope;
                foreach (float side in Ends)
                    yield return (hook, new Vector3(end * Length * 0.5f, side * (KwHammockShape.Bar - 0.025f), Spec.Bars + 0.02f));
            }
        }

        /// <summary>Where a sleeper lies (the bed's spawn point), the hammock's frame: over the cloth's middle.</summary>
        public Vector3 Lying => new Vector3(0f, 0f, Spec.Bars - KwHammockShape.Sag + 0.08f);

        // How far down the sag a point s along the cloth is: 1 in the middle, 0 at the bars, on the catenary.
        private float Dip(float s)
        {
            double h = Length * 0.5 / a, end = Math.Cosh(h), at = Math.Cosh(s / a);
            return Mathf.Clamp01((float)((end - at) / (end - 1.0)));
        }

        // The catenary's parameter a for the cloth's half length and sag: a (cosh(h / a) - 1) = sag.
        private static float Catenary(float half, float sag)
        {
            float low = 0.05f, high = 100f;
            for (int i = 0; i < 40; i++)
            {
                float mid = 0.5f * (low + high);
                if (mid * (Math.Cosh(half / mid) - 1.0) > sag)
                    low = mid;
                else
                    high = mid;
            }
            return 0.5f * (low + high);
        }
    }
}
