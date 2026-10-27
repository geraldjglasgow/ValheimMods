using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One click of the hammer's repair on a launched ship (<see cref="KwShipRepair"/>), on the repairing player's game: the
    /// ship's damage as this game reads it from the ship's ZDO, where the hammer aims, and the one thing mended. What the
    /// hammer is aimed at comes first: a rig part (<see cref="KwRigMendAsk.Part"/>: its marks, or a fallen piece's stump), a
    /// hole the ray meets or whose edge is within <see cref="HoleNear"/> of where it struck (<see cref="KwHoleAim"/>), a
    /// broken railing within <see cref="RailNear"/>, the deck where a fallen piece stood (<see cref="KwRigMendAsk.Deck"/>);
    /// else the first of <see cref="KwShipRepairPlan"/>'s steps not asked just now (<see cref="KwShipRepairAsked"/>). Each is
    /// asked of the ship's owner by its own call (<see cref="KwHullHoles.MendRpc"/>, <see cref="KwShipRails.MendRpc"/>,
    /// <see cref="KwRigDamage.MendRpc"/>, <see cref="KwRigDamage.RaiseRpc"/>), whose change every peer then shows, and told in
    /// the corner with how many are left.
    /// </summary>
    internal sealed class KwShipRepairWork
    {
        public const float HoleMargin = 0.4f, HoleNear = 1f, RailNear = 1.2f, Behind = 1f;
        private static readonly List<KwRailStretch> NoStretches = new List<KwRailStretch>();
        private static readonly HashSet<(int, int)> NoBroken = new HashSet<(int, int)>();
        private static readonly List<KwRigMark> NoMarks = new List<KwRigMark>();
        private static readonly HashSet<int> NoPatches = new HashSet<int>();
        private readonly Player me;
        private readonly KwHullHoles holes;
        private readonly KwShipRails? rails;
        private readonly KwRigDamage? rig;
        private readonly Transform ship;
        private readonly Collider? aimed;
        private readonly Vector3 point;
        private readonly bool rigAimed, onShip;

        public KwShipRepairWork(KwHullHoles holes, Player me)
        {
            (this.holes, this.me, ship) = (holes, me, holes.transform);
            (rails, rig) = (holes.GetComponent<KwShipRails>(), holes.GetComponent<KwRigDamage>());
            aimed = KwRigDamageMend.Aimed(me, out point, out rigAimed);
            onShip = aimed != null && aimed.GetComponentInParent<KwHullHoles>() == holes;
        }

        /// <summary>One thing mended: what the hammer aims at, else the next in the plan; false when nothing could be.</summary>
        public bool Next() => Aimed() || Planned();

        private bool Aimed()
        {
            if (!onShip)
                return false;
            if (rigAimed && rig != null && KwRigMendAsk.Part(rig, aimed!, me))
                return Tell("The rig is mended where the hammer struck");
            KwRepairStep? step = AimedHole() ?? AimedRail();
            if (step.HasValue)
                return Ask(step.Value);
            return rig != null && KwRigMendAsk.Deck(rig, point, me) && Tell("The rig is being put back");
        }

        // The hole the hammer's ray meets before what it struck (and a little past), else the one whose edge is nearest that.
        private KwRepairStep? AimedHole()
        {
            Transform eye = GameCamera.instance.transform;
            Vector3 from = ship.InverseTransformPoint(eye.position), way = ship.InverseTransformDirection(eye.forward).normalized;
            int i = KwHoleAim.Find(holes.Holes, from, way, HoleMargin, Vector3.Distance(eye.position, point) + Behind, out _);
            if (i < 0)
                i = KwHoleAim.Nearest(holes.Holes, ship.InverseTransformPoint(point), HoleNear);
            return i < 0 ? (KwRepairStep?)null : HoleStep(holes.Holes[i]);
        }

        private KwRepairStep HoleStep(KwHullHole hole) =>
            new KwRepairStep(holes.Patches != null && holes.Patches.Covers(hole.Id) ? KwRepairStep.Patched : KwRepairStep.Hole, hole.Id, 0, 0, hole.Place);

        // The broken railing stretch nearest where the hammer struck, within reach.
        private KwRepairStep? AimedRail()
        {
            if (rails == null || !rails.AnyBroken)
                return null;
            Vector3 local = ship.InverseTransformPoint(point);
            (int found, float best) = (-1, RailNear);
            for (int i = 0; i < rails.Stretches.Count; i++)
            {
                KwRailStretch s = rails.Stretches[i];
                float d = s.Distance(local);
                if (d < best && rails.Broken.Contains((s.Piece, s.Part)))
                    (found, best) = (i, d);
            }
            return found < 0 ? (KwRepairStep?)null : new KwRepairStep(KwRepairStep.Rail, rails.Stretches[found].Piece, rails.Stretches[found].Part, 0, local);
        }

        // The first step of the plan not asked just now that can be asked (a fallen piece only with its cost in hand).
        private bool Planned()
        {
            foreach (KwRepairStep step in Plan())
            {
                if (!KwShipRepairAsked.Waiting(holes, step) && Ask(step))
                    return true;
            }
            return false;
        }

        private List<KwRepairStep> Plan()
        {
            Vector3 aim = ship.InverseTransformPoint(onShip ? point : me.transform.position);
            return KwShipRepairPlan.Of(holes.Holes, holes.Patches != null ? holes.Patches.Shown : NoPatches, rails?.Stretches ?? NoStretches,
                rails?.Broken ?? NoBroken, rig?.Parts, rig?.Marks ?? NoMarks, aim);
        }

        private bool Ask(KwRepairStep step)
        {
            if (!Send(step))
                return false;
            KwShipRepairAsked.Mark(holes, step);
            return Tell(KwShipRepairPlan.Words(step, rig?.Parts));
        }

        // The step's call to the ship's owner; false when it cannot be asked (a fallen piece the player cannot pay for).
        private bool Send(KwRepairStep step)
        {
            switch (step.Kind)
            {
                case KwRepairStep.Patched:
                case KwRepairStep.Hole:
                    holes.AskMend(step.A);
                    return true;
                case KwRepairStep.Rail:
                    holes.GetComponent<ZNetView>()?.InvokeRPC(KwShipRails.MendRpc, step.A, step.B);
                    return true;
                case KwRepairStep.Rig:
                    return rig != null && KwRigMendAsk.Mend(rig, step.A, step.B, (char)step.C);
                default:
                    KwRigPiece? piece = rig?.Parts.Get(step.A);
                    return piece != null && KwRigMendAsk.Raise(rig!, piece, me);
            }
        }

        // What was mended told in the corner, with how much is left (the steps asked just now counted as done).
        private bool Tell(string words)
        {
            int left = Plan().FindAll(s => !KwShipRepairAsked.Waiting(holes, s)).Count;
            me.Message(MessageHud.MessageType.TopLeft, left > 0 ? $"{words} ({left} more to mend)" : $"{words}: nothing more to mend");
            return true;
        }
    }
}
