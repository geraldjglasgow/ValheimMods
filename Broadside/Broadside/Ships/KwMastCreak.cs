using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The wood of a falling rig piece straining as it goes (the user, 2026-10-27: "wood kind of creak snapping slowly as
    /// the mast falls over, and kinda when it's about halfway over ... it gets a little more intense", in place of the
    /// game's tree fall): the game's own recordings, its heavy darkwood gate's creaks and its branches breaking, played at
    /// the break (<see cref="KwRigSound"/>). At first they come seldom and quiet, a little high; as the piece leans they
    /// come closer together, louder and deeper, with more splintering, most when it is about halfway over (three quarters
    /// of the way through its fall, as it gathers speed: <see cref="Peak"/>), where the wood also cracks once more. The
    /// moments and pitches come from the fall mark (<see cref="KwMastFall.Chance"/>), so every screen hears the same fall.
    /// Local sounds only.
    /// </summary>
    public sealed class KwMastCreak
    {
        /// <summary>The share of the fall where it strains most: about halfway over, the fall's angle growing as its time to the power 2.4.</summary>
        public const float Peak = 0.75f;

        private const float Last = 0.95f, Seldom = 1.3f, Often = 0.32f;
        private readonly KwRigMark fall;
        private readonly float length;
        private float next;
        private int count;
        private bool cracked;

        public KwMastCreak(KwRigMark fall, float length)
        {
            (this.fall, this.length) = (fall, length);
            next = 0.25f + 0.35f * KwMastFall.Chance(fall, 100);
        }

        /// <summary>The sounds due by this moment of the fall, played where the piece broke.</summary>
        public void Tick(float time, Vector3 at)
        {
            float share = Mathf.Clamp01(time / length), strain = Strain(share);
            if (!cracked && share >= Peak)
            {
                cracked = true;
                KwRigSound.Play(KwRigSound.Crack, at, 1f, 0.85f);
            }
            if (time < next || share >= Last)
                return;
            float r = KwMastFall.Chance(fall, 101 + count);
            KwRigSound.Play(KwRigSound.Creak, at, Mathf.Lerp(0.45f, 1f, strain), Mathf.Lerp(0.95f, 0.72f, strain) + 0.1f * (r - 0.5f));
            if (r < 0.2f + 0.55f * strain)
                KwRigSound.Play(KwRigSound.Splinter, at, Mathf.Lerp(0.3f, 0.85f, strain), 0.9f + 0.2f * r);
            count++;
            next = time + Mathf.Lerp(Seldom, Often, strain) * (0.75f + 0.5f * KwMastFall.Chance(fall, 300 + count));
        }

        // How hard the wood strains through the fall: a little at first, rising smoothly to the most at Peak, kept there.
        private static float Strain(float share) => share >= Peak ? 1f : Mathf.Lerp(0.15f, 1f, Mathf.SmoothStep(0f, 1f, share / Peak));
    }
}
