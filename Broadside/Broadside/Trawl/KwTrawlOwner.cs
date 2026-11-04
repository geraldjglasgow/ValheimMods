using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The net winch's owner's side (the game that owns the winch's ZDO: whoever the game hands the piece to, a player near
    /// it, a dedicated server running the area): the only game that writes the winch's state. It begins a cast or a haul
    /// when asked (<see cref="Use"/>), moves the net on from step to step by the network clock, fishes once a second while
    /// the net is out and the winch's ship is in its scene (<see cref="KwTrawlCatch"/>), and lands the catch on the deck
    /// (<see cref="KwTrawlDrop"/>; the net emptied in the same frame, so a new owner never lands it twice). A winch no
    /// longer riding any ship has its net wound in at once; its catch stays in the net until it is next landed.
    /// </summary>
    public sealed class KwTrawlOwner
    {
        private const float CatchEvery = 1f;
        private readonly KwTrawlWinch winch;
        private float nextCatch;

        public KwTrawlOwner(KwTrawlWinch winch) => this.winch = winch;

        /// <summary>
        /// On the owner, E asked of it by a player's game (<see cref="KwTrawlWinch.UseCall"/>): a stowed net cast (its ship
        /// in this game's scene), a net out hauled in; anything else (a second ask, a step already moved on) is let be.
        /// </summary>
        public void Use(int want)
        {
            ZDO? zdo = winch.Zdo;
            if (zdo == null || !zdo.IsOwner())
                return;
            KwTrawlStep step = KwTrawlState.Step(zdo);
            if (want == KwTrawlWinch.Cast && step == KwTrawlStep.Stowed && winch.Ship != null)
            {
                KwTrawlState.Begin(zdo, KwTrawlStep.Casting);
                KwTrawlState.Fares(zdo, KwTrawlFare.None);
            }
            else if (want == KwTrawlWinch.Haul && step == KwTrawlStep.Out)
                KwTrawlState.Begin(zdo, KwTrawlStep.Hauling);
        }

        /// <summary>On the owner, each frame: the step moved on when its time is up.</summary>
        public void Tick(ZDO zdo, KwTrawlStep step, float t)
        {
            if (step != KwTrawlStep.Stowed && !winch.Riding)
            {
                KwTrawlState.Begin(zdo, KwTrawlStep.Stowed);
                return;
            }
            if (step == KwTrawlStep.Casting && t >= KwTrawlTune.CastTime)
                KwTrawlState.Begin(zdo, KwTrawlStep.Out);
            else if (step == KwTrawlStep.Out)
                Fish(zdo);
            else if (step == KwTrawlStep.Hauling && t >= KwTrawlTune.HaulTime)
                KwTrawlState.Begin(zdo, KwTrawlStep.Landing);
            else if (step == KwTrawlStep.Landing)
                Land(zdo, t);
        }

        // Once a second while out, its ship here and the net followed: how it fares and what it catches.
        private void Fish(ZDO zdo)
        {
            if (Time.time < nextCatch)
                return;
            nextCatch = Time.time + CatchEvery;
            if (winch.Ship != null && winch.Tow.Started)
                KwTrawlCatch.Step(zdo, winch, CatchEvery);
        }

        // The bag opened over the deck: the fish landed once (they wait in the net while its ship is not here); then stowed.
        private void Land(ZDO zdo, float t)
        {
            bool full = KwTrawlState.Count(zdo) > 0;
            if (full && t >= KwTrawlTune.DropAt && winch.Ship != null)
            {
                KwTrawlDrop.Land(winch, winch.Ship, zdo);
                full = false;
            }
            if (!full && t >= KwTrawlTune.LandTime)
                KwTrawlState.Begin(zdo, KwTrawlStep.Stowed);
        }
    }
}
