using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The stay a bowsprit's jibs set on (<see cref="KwBowsprit"/>): from the foremast's upper part, just under its masthead on
    /// its face toward the bow, down to the bowsprit. The foremast is the mast nearest the bow of those standing aft of the
    /// bowsprit's post about on its line (within a metre of it, and a sixth of the way back more), at most
    /// <see cref="Reach"/> off. A mast's text cannot know where the bowsprit's tip is, nor the bowsprit's where the mast
    /// is, so the stay is found from the ship's own pieces whenever the jibs are built: on the dock among the masts loaded
    /// there (<see cref="OnDock"/>), on a launched ship and for its cost and drive among its blueprint's pieces
    /// (<see cref="InBlueprint"/>), from the masts' saved texts, so every peer finds the same stay. Kit axes of the bowsprit.
    /// </summary>
    public sealed class KwJibStay
    {
        public const float Reach = 30f, UnderHead = 0.35f;

        private KwJibStay(Vector3 head, Vector3 foot, KwMastSpec mast)
        {
            (Head, Foot, Mast) = (head, foot, mast);
        }

        /// <summary>The stay's upper end on the foremast.</summary>
        public Vector3 Head { get; }

        /// <summary>The foremast's foot.</summary>
        public Vector3 Foot { get; }

        public KwMastSpec Mast { get; }

        /// <summary>What tells two stays apart, for building the jibs again: the head's place to the centimetre, and the mast.</summary>
        public string Key => $"{Head.x:F2},{Head.y:F2},{Head.z:F2}|{Mast}";

        /// <summary>
        /// The stay from a bowsprit at a place to the foremost of these masts (each its foot and spec, in the bowsprit's
        /// frame of reference: the world on the dock, the ship's frame in a blueprint), and which one it is; null and -1 when
        /// none stands aft of it on its line.
        /// </summary>
        public static KwJibStay? Of(Vector3 at, Quaternion turn, List<(Vector3 Foot, KwMastSpec Mast)> masts, out int index)
        {
            index = -1;
            Quaternion back = new Quaternion(-turn.x, -turn.y, -turn.z, turn.w);
            float best = float.MinValue;
            for (int i = 0; i < masts.Count; i++)
            {
                Vector3 kit = KwMesh.ToKit(back * (masts[i].Foot - at));
                if (kit.x > -0.5f || kit.x < -Reach || Mathf.Abs(kit.y) > 1f + Mathf.Abs(kit.x) / 6f || kit.x <= best)
                    continue;
                (best, index) = (kit.x, i);
            }
            return index < 0 ? null : Made(KwMesh.ToKit(back * (masts[index].Foot - at)), masts[index].Mast);
        }

        // The stay's head: under the masthead, on the mast's face toward the bowsprit.
        private static KwJibStay Made(Vector3 foot, KwMastSpec mast)
        {
            float up = mast.Mast - UnderHead;
            Vector3 toward = new Vector3(-foot.x, -foot.y, 0f).normalized * KwMastTops.RadiusAt(mast.Mast, up);
            return new KwJibStay(foot + new Vector3(0f, 0f, up) + toward, foot, mast);
        }

        /// <summary>A blueprint's bowsprit's stay, to its foremast among the blueprint's masts; null with none.</summary>
        public static KwJibStay? InBlueprint(KwBlueprint blueprint, KwBpPiece bowsprit)
        {
            List<(Vector3 Foot, KwMastSpec Mast)> masts = new List<(Vector3 Foot, KwMastSpec Mast)>();
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                if (piece.Prefab == KwSailTool.Made && KwMastSpec.TryParse(KwShipParts.Saved(piece), out KwMastSpec mast))
                    masts.Add((piece.Position, mast));
            }
            return Of(bowsprit.Position, bowsprit.Rotation, masts, out _);
        }

        /// <summary>The stay from a bowsprit at a place on the dock to its foremast among the masts loaded, and that mast; null with none.</summary>
        public static KwJibStay? OnDock(Vector3 at, Quaternion turn, out KwSailRig? rig)
        {
            List<KwSailRig> near = new List<KwSailRig>();
            List<(Vector3 Foot, KwMastSpec Mast)> masts = new List<(Vector3 Foot, KwMastSpec Mast)>();
            foreach (KwSailRig one in KwSailRig.Loaded)
            {
                if (one == null || one.Spec == null || (one.transform.position - at).sqrMagnitude > Reach * Reach)
                    continue;
                near.Add(one);
                masts.Add((one.transform.position, one.Spec));
            }
            KwJibStay? stay = Of(at, turn, masts, out int index);
            rig = index >= 0 ? near[index] : null;
            return stay;
        }
    }
}
