using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>Where a ship cannon's hauler is with it: not at all, stepping in to its rear, holding it.</summary>
    public enum KwTowPhase
    {
        None,
        StepIn,
        Holding,
    }

    /// <summary>
    /// A ship cannon hitched to its hauler, on the hauler's own game (which owns the cannon's ZDO while he hauls it). Taken
    /// up from anywhere within <see cref="Take"/> of the spot behind its rear, the same reach its hover offers the key in
    /// (<see cref="InReach"/>, <see cref="Reachable"/>), he is first brought there at once, round the cannon
    /// (<see cref="KwTowPhase.StepIn"/>, the MannedWeapons library's <see cref="QuickStep"/>), then hauls it about its
    /// ship's deck (<see cref="KwDeckHaul"/>), which holds his walk to it. While hitched his body and the cannon's colliders
    /// pass through each other, he walks rather than runs and cannot jump, at the carriage's pace
    /// (<see cref="KwArmControls"/>; his walk setting comes back after), and the ZDO names him for every peer (his hands,
    /// <see cref="KwTowHands"/>). Let go at once: Shift+E, dodging, dying, teleporting, swimming, other controls taken,
    /// walking off out of reach, stepping off its ship (<see cref="KwArmTow.Footed"/>), the cannon owned by another game.
    /// Let go, its owner settles it (<see cref="KwArmTow.Settle"/>). One cannon at a time: hitching one lets go of any other.
    /// </summary>
    public sealed class KwTowHitch
    {
        /// <summary>
        /// How near the spot behind its rear his feet must be to take it (the manned weapons' use range: brought there at
        /// once from anywhere in it), and how near what he holds his hips must stay to keep it.
        /// </summary>
        private const float Take = WeaponDesign.MostUse, Keep = 2.2f;

        private static readonly Vector3 Hips = new Vector3(0f, 0.7f, 0f);

        private readonly KwArmTow tow;
        private readonly Collider[] parts;
        private readonly QuickStep step = new QuickStep();
        private Player? puller, clearing;
        private bool walked;

        public KwTowHitch(KwArmTow tow) => (this.tow, parts) = (tow, tow.Arm.GetComponentsInChildren<Collider>(true));

        /// <summary>Where the hauler is with the cannon now, on this game.</summary>
        public KwTowPhase Phase { get; private set; }

        /// <summary>The hauler hitched on this game; null when none.</summary>
        public Player? Puller => puller;

        /// <summary>Whether this game has the cannon hitched to its hauler now (stepping in to it too).</summary>
        public bool Attached => puller != null && (tow.OnDeck || Phase == KwTowPhase.StepIn);

        /// <summary>Whether it is hauled about its ship's deck now: he holds it.</summary>
        public bool Hauling => puller != null && tow.OnDeck && Phase == KwTowPhase.Holding;

        /// <summary>Where the hauler's feet go: behind its rear.</summary>
        public Vector3 Spot => tow.Handle;

        /// <summary>Whether a player stands near enough to take it up: within <see cref="Take"/> of <see cref="Spot"/>.</summary>
        public bool InReach(Player player) => Vector3.Distance(player.transform.position, Spot) <= Take;

        /// <summary>Whether he can be brought to <see cref="Spot"/> from where he stands, round the cannon (not swimming, not too far up, nothing in the way).</summary>
        public bool Reachable(Player player) => OperatorReach.Why(player, Spot, tow.Arm.transform.position, tow.Arm.transform) == ReachBlock.None;

        /// <summary>Whether a player can take and keep it: alive, free, on its deck, near enough.</summary>
        public bool CanHold(Player? player)
        {
            if (player == null || player.IsDead() || player.InDodge() || player.IsTeleporting() || player.IsAttached() || player.IsSwimming())
                return false;
            if (player.GetDoodadController() != null || !tow.Footed(player))
                return false;
            if (Phase != KwTowPhase.Holding)
                return InReach(player);
            return Vector3.Distance(player.transform.position + Hips, tow.Handle) < Keep;
        }

        /// <summary>Hitches the player (this game owns the cannon's ZDO now), letting go of any other cannon first.</summary>
        public void Attach(Player player)
        {
            KwArmTow.Mine?.Hitch.Release();
            (puller, walked, Phase) = (player, player.GetWalk(), KwTowPhase.StepIn);
            step.Begin(player, Spot, tow.Arm.transform.position, tow.Arm.transform);
            Collide(player, false);
            tow.Link.Zdo.Set(tow.Link.Tower, player.GetZDOID());
            KwArmTow.Mine = tow;
            player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(KwArmWords.Hitched));
        }

        /// <summary>Lets go: the hauler's walk and collisions back, the cannon settled and the ZDO cleared (by its owner).</summary>
        public void Release()
        {
            bool held = puller != null && Phase != KwTowPhase.StepIn;
            step.Stop();
            if (puller != null)
                Unhitch(puller);
            (puller, Phase) = (null, KwTowPhase.None);
            if (KwArmTow.Mine == tow)
                KwArmTow.Mine = null;
            Settled(held);
        }

        // His walk setting back, and his body and the cannon's colliding again once he has stepped clear of it (he may
        // stand up against its rear): letting go never shoves him out of it.
        private void Unhitch(Player player)
        {
            player.SetWalk(walked);
            clearing = player;
        }

        // On the owner: a cannon that was held settled where it was left (<see cref="KwArmTow.Settle"/>), and nobody named
        // as its hauler any more.
        private void Settled(bool held)
        {
            if (!tow.Link.Valid || !tow.Link.View.IsOwner())
                return;
            if (held)
                tow.Settle();
            if (!tow.Link.Zdo.GetZDOID(tow.Link.Tower).IsNone())
                tow.Link.Zdo.Set(tow.Link.Tower, ZDOID.None);
        }

        /// <summary>
        /// Each frame: let go when the hitch may not hold; a hauler who let go of one collides with it again once he has
        /// stepped clear of it; on the owner, a hauler named who is not hitched here (he left the game, or the game gave this
        /// one the cannon) is cleared, the cannon settled where he left it.
        /// </summary>
        public void Check()
        {
            if (clearing != null && Clear(clearing))
            {
                Collide(clearing, true);
                clearing = null;
            }
            bool hitched = puller != null;
            if (hitched && (!Attached || !tow.Link.View.IsOwner() || !CanHold(puller)))
                Release();
            else if (!hitched && tow.Link.View.IsOwner() && !tow.Link.Zdo.GetZDOID(tow.Link.Tower).IsNone())
                Settled(true);
        }

        /// <summary>
        /// On the hauler's own game, each physics step after the game has worked out his movement: brought in behind its
        /// rear while he steps in; while he hauls it, steered as he pushes it and held back from a way it could not follow
        /// him (<see cref="KwDeckHaul.Leash"/>).
        /// </summary>
        public void Steer(Player player)
        {
            if (player != puller)
                return;
            if (Phase == KwTowPhase.StepIn)
                StepIn(player);
            else if (tow.OnDeck)
                player.m_moveDir = tow.Deck.Leash(player, player.m_moveDir);
        }

        // Brought in behind its rear at once, round the cannon; there he takes it up and its haul begins.
        private void StepIn(Player player)
        {
            player.m_moveDir = Vector3.zero;
            if (step.Step(player, Spot, Time.fixedDeltaTime))
                return;
            Phase = KwTowPhase.Holding;
            if (tow.OnDeck)
                tow.Deck.Begin();
        }

        // His body and the cannon's parts pass through each other while he hauls it (his capsule would catch on them as it
        // swings round him); only his own game moves his body.
        private void Collide(Player player, bool collide)
        {
            if (player.m_collider == null)
                return;
            foreach (Collider part in parts)
            {
                if (part != null)
                    Physics.IgnoreCollision(player.m_collider, part, !collide);
            }
        }

        // Whether none of the cannon's colliders overlaps his body (or he is gone or dead).
        private bool Clear(Player player)
        {
            Collider? body = player.m_collider;
            if (body == null || player.IsDead())
                return true;
            Transform him = body.transform;
            foreach (Collider part in parts)
            {
                if (part != null && part.enabled && Physics.ComputePenetration(body, him.position, him.rotation, part, part.transform.position, part.transform.rotation, out _, out _))
                    return false;
            }
            return true;
        }
    }
}
