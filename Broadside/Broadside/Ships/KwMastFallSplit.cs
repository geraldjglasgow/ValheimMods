using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What of a rig piece's fitting goes with its fall (the user, 2026-10-27: "if the very top part of a 3 tiered mast is
    /// knocked off ... the masts and the sails below that can be fine. Right now ... all the sails just disappear"). Of a
    /// mast: the pennant at its head and each sail hung from above the break (<see cref="KwRigCondition.Above"/>) with
    /// its gaff, boom, halyard, peak and sheets; of a bowsprit or post cap, all of it. Those parts are gathered under one
    /// holder in the fitting's own frame (so every part keeps its local pose, and the boom swing and sail motion keep
    /// working on them), which <see cref="KwMastFall"/> topples and hides as it did the whole fitting; the ropes tied on
    /// above the break part (<see cref="Ropes"/>); everything else stays on the mast. Put back, they go back into the
    /// fitting (<see cref="Scatter"/>). Only where the ship is drawn.
    /// </summary>
    public static class KwMastFallSplit
    {
        private const string HolderName = "fallen", Pennant = "pennant", Sheet = "sheet";
        private static readonly string[] SailParts = { KwSailDress.ClothName, KwSailDress.GaffName, "boom", "halyard", "peak" };

        /// <summary>The holder of what goes with the piece's fall, its parts gathered under it the first time; null with no fitting.</summary>
        public static Transform? Gather(KwRigLook look, KwRigMark fall)
        {
            Transform? fitting = look.Fitting;
            if (fitting == null)
                return null;
            if (look.Fallen != null)
                return look.Fallen;
            GameObject holder = new GameObject(HolderName) { layer = fitting.gameObject.layer };
            holder.transform.SetParent(fitting.parent, false);
            holder.transform.SetLocalPositionAndRotation(fitting.localPosition, fitting.localRotation);
            List<Transform> going = new List<Transform>();
            foreach (Transform part in fitting)
            {
                if (Goes(look.Piece, part.name, fall))
                    going.Add(part);
            }
            foreach (Transform part in going)
                part.SetParent(holder.transform, false);
            fitting.gameObject.AddComponent<KwFallenLink>().Holder = holder;
            return look.Fallen = holder.transform;
        }

        /// <summary>Everything gathered for a fall back in the fitting as it was, and the holder gone (the piece put back).</summary>
        public static void Scatter(KwRigLook look)
        {
            Transform? holder = look.Fallen, fitting = look.Fitting;
            look.Fallen = null;
            if (holder == null || fitting == null)
                return;
            List<Transform> parts = new List<Transform>();
            foreach (Transform part in holder)
                parts.Add(part);
            foreach (Transform part in parts)
                part.SetParent(fitting, false);
            UnityEngine.Object.Destroy(fitting.GetComponent<KwFallenLink>());
            UnityEngine.Object.Destroy(holder.gameObject);
        }

        /// <summary>
        /// The ropes that part with a fall (<see cref="Gather"/> first): every rope that goes with it, but a sail's bolt rope,
        /// which goes with its cloth, and every rope left on the mast whose highest point is above the break.
        /// </summary>
        public static List<LineRenderer> Ropes(KwRigLook look, KwRigMark fall)
        {
            List<LineRenderer> ropes = new List<LineRenderer>();
            if (look.Fallen != null)
                ropes.AddRange(Array.FindAll(look.Fallen.GetComponentsInChildren<LineRenderer>(true), Rope));
            if (look.Fitting == null)
                return ropes;
            KwRigSpar main = look.Piece.Main;
            float breaks = look.Piece.ToShip(main.At(Mathf.Clamp(fall.A, 0f, main.Length))).y;
            Transform ship = look.transform.parent;
            foreach (LineRenderer line in look.Fitting.GetComponentsInChildren<LineRenderer>(true))
            {
                if (Rope(line) && Top(line, ship) > breaks + 0.05f)
                    ropes.Add(line);
            }
            return ropes;
        }

        private static bool Rope(LineRenderer line) => line.name != KwSailRigging.BoltName && line.positionCount >= 2;

        // A rope's highest point in the frame the rig piece's look stands in.
        private static float Top(LineRenderer line, Transform ship)
        {
            float top = float.MinValue;
            for (int i = 0; i < line.positionCount; i++)
            {
                Vector3 p = line.GetPosition(i);
                top = Mathf.Max(top, ship.InverseTransformPoint(line.useWorldSpace ? p : line.transform.TransformPoint(p)).y);
            }
            return top;
        }

        // A part of the fitting that goes: all of a bowsprit's or cap's; of a mast's, the pennant at its head and the parts
        // of each sail hung from above the break.
        private static bool Goes(KwRigPiece piece, string name, KwRigMark fall)
        {
            if (!piece.IsMast || name == Pennant)
                return true;
            for (int i = 0; i < piece.Sails.Count; i++)
            {
                if (KwRigCondition.Above(piece, i, fall) && Of(name, i))
                    return true;
            }
            return false;
        }

        private static bool Of(string name, int sail) =>
            name.StartsWith(Sheet + sail + "_", StringComparison.Ordinal) || Array.Exists(SailParts, part => name == part + sail);
    }

    /// <summary>
    /// On a fitting whose fallen parts are held apart (<see cref="KwMastFallSplit"/>): the holder goes when the fitting does
    /// (a fallen mast removed or made again at sea), so nothing is left behind hidden.
    /// </summary>
    public sealed class KwFallenLink : MonoBehaviour
    {
        public GameObject? Holder { get; set; }

        private void OnDestroy()
        {
            if (Holder != null)
                Destroy(Holder);
        }
    }
}
