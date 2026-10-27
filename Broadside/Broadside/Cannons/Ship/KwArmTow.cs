using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a ship cannon is hauled by, on every peer: its rear (a child of the cannon, with its own collider, so hovering
    /// it finds this and not the cannon). Asked for on 2026-10-26: "E is to mount but some other key should be to move it".
    /// E anywhere on it, its rear too, mans it, and the game's alternate use, Shift+E (<see cref="KwArm"/>'s, the
    /// MannedWeapons library's <c>AltUse</c>), anywhere on it hauls it about its ship's deck, Shift+E again lets go. The
    /// hover offers the key exactly where it works (<see cref="Barred"/>: within the manned weapons' use range of its rear,
    /// and he can be brought there). The key asks the cannon's owner for it (<see cref="KwArmLink.Tow"/>); the owner hands
    /// its ZDO over, unless someone mans the cannon or hauls it already (<see cref="KwArmLink.TowDenied"/>). Once this peer
    /// owns it the hauler is hitched (<see cref="KwTowHitch"/>), who hauls it is written to its ZDO for every peer (its
    /// hover, the hauler's hands, <see cref="KwTowHands"/>, its breeching hidden, <see cref="KwBreechingTie"/>), and from
    /// then on this peer moves it about the deck in the ship's frame (<see cref="KwDeckHaul"/>), its pose written to
    /// Broadside's rider record for every peer (<see cref="KwShipRider"/>). Nobody can man it while it is hauled
    /// (<see cref="KwArm"/>'s owner refuses), so it cannot be fired; nobody can haul it while it is manned
    /// (<see cref="Busy"/>). Only while it rides a launched ship (<see cref="OnDeck"/>), the only place it is built.
    /// </summary>
    public sealed class KwArmTow : MonoBehaviour, Hoverable, Interactable
    {
        /// <summary>How long an asking game waits for the owner's answer before it gives up; how far from the cannon a hauler still counts.</summary>
        private const float Patience = 3f, Reach = 4f;

        /// <summary>The cannon the local player hauls now; null when he hauls none.</summary>
        public static KwArmTow? Mine { get; internal set; }

        private KwArm? arm;
        private ZNetView? view;
        private KwArmLink? link;
        private KwShipSpec? spec;
        private KwTowHitch? hitch;
        private KwDeckHaul? deck;
        private Player? asking;
        private float askedAt, barredAt = -1f;
        private string? barred;
        private bool broken;

        public KwArm Arm => arm!;
        public KwArmLink Link => link!;
        public KwShipSpec Spec => spec!;
        public KwTowHitch Hitch => hitch!;

        /// <summary>Its haul about the deck it rides.</summary>
        public KwDeckHaul Deck => deck!;

        /// <summary>Where the hauler's feet go: behind its rear.</summary>
        public Vector3 Handle => transform.parent.TransformPoint(KwMesh.ToUnity(Spec.Handle));

        /// <summary>Whether it rides a launched Broadside ship now (the ship's child, <see cref="KwArm.Aboard"/>): only then is it hauled.</summary>
        public bool OnDeck => link != null && link.Valid && arm != null && arm.Aboard;

        private bool Ready => hitch != null && OnDeck;

        // After every Awake: the cannon's view has its ZDO by now.
        private void Start() => Guard.Run("Broadside cannon tow", static t => t.Init(), this);

        private void Init()
        {
            (arm, view) = (GetComponentInParent<KwArm>(), GetComponentInParent<ZNetView>());
            spec = arm != null ? KwArmory.Arm(arm.Id)?.Ship : null;
            if (arm == null || view == null || view.GetZDO() == null || spec == null)
            {
                enabled = false;
                return;
            }
            link = new KwArmLink(view);
            view.Register(link.Tow, sender => Guard.Run("Broadside cannon tow", () => Asked(sender)));
            view.Register(link.TowDenied, _ => Guard.Run("Broadside cannon tow", Denied));
            (hitch, deck) = (new KwTowHitch(this), new KwDeckHaul(this, spec));
        }

        private void OnDestroy() => hitch?.Release();

        // A throw is reported once and stops this haul, rather than once a frame.
        private void Update()
        {
            if (broken || hitch == null || link == null || !link.Valid)
                return;
            broken = true;
            Guard.Run("Broadside cannon tow", static t => t.Tick(), this);
            broken = false;
        }

        // The hauler's game moves it each frame, as he and the ship are drawn.
        private void Tick()
        {
            if (asking != null)
                Waiting();
            hitch!.Check();
            if (hitch.Hauling && OnDeck)
                deck!.Step(hitch.Puller!, Time.deltaTime);
        }

        // On the asking game: hitched once the cannon's ZDO is ours; given up after a while without an answer.
        private void Waiting()
        {
            if (view!.IsOwner())
            {
                Player player = asking!;
                asking = null;
                if (hitch!.CanHold(player))
                    hitch.Attach(player);
                else
                    player.Message(MessageHud.MessageType.Center, KwArmWords.Reach);
            }
            else if (Time.time - askedAt > Patience)
                asking = null;
        }

        // On the owner: the cannon goes to the asker, unless it is manned or hauled.
        private void Asked(long sender)
        {
            if (!view!.IsOwner())
                return;
            if (Busy())
                view.InvokeRPC(sender, Link.TowDenied);
            else
                view.GetZDO().SetOwner(sender);
        }

        private void Denied()
        {
            asking?.Message(MessageHud.MessageType.Center, "Someone is using it.");
            asking = null;
        }

        /// <summary>Whether someone mans the cannon or hauls it now (one who has gone does not count), from its ZDO.</summary>
        public bool Busy() => Arm.Manned || Present(view!.GetZDO().GetZDOID(Link.Tower));

        // A hauler still there: his character in the world and near the cannon (one logged out or gone off is not).
        private bool Present(ZDOID who)
        {
            ZDO? character = who.IsNone() ? null : ZDOMan.instance.GetZDO(who);
            return character != null && Vector3.Distance(character.GetPosition(), Arm.transform.position) <= Reach;
        }

        /// <summary>The use key on its rear mans the cannon; the alternate use hauls it.</summary>
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || !(user is Player player) || player != Player.m_localPlayer || arm == null || arm.Def == null)
                return false;
            if (!alt)
                return Arm.Interact(user, hold, alt);
            Take(player);
            return false;
        }

        /// <summary>
        /// Shift+E anywhere on it, on the local player's game: lets go of the one he hauls, else asks its owner for it, or
        /// says why not.
        /// </summary>
        public void Take(Player player)
        {
            if (!Ready)
                return;
            if (Mine == this)
                hitch!.Release();
            else if (Barred(player) is string why)
            {
                if (why.Length > 0)
                    player.Message(MessageHud.MessageType.Center, why);
            }
            else
            {
                (asking, askedAt) = (player, Time.time);
                if (!view!.IsOwner())
                    view.InvokeRPC(Link.Tow);
            }
        }

        /// <summary>
        /// Why the player may not haul it from where he is: someone mans or hauls it, he hauls another, he is not on its
        /// deck, he cannot be brought to its rear; empty when he is busy or too far (nothing said); null: he may.
        /// </summary>
        public string? Barred(Player player)
        {
            string? why = Why(player);
            if (why != null)
                return why;
            if (player.GetDoodadController() != null || player.IsAttached() || !hitch!.InReach(player))
                return "";
            return hitch.Reachable(player) ? null : KwArmWords.Reach;
        }

        // Why the player may not haul it now: someone mans or hauls it, he hauls another, or he is off its deck.
        private string? Why(Player player)
        {
            if (Busy())
                return Arm.Manned ? "Someone mans it." : "Someone is hauling it.";
            if (Mine != null)
                return "You are hauling another cannon.";
            return Footed(player) ? null : KwArmWords.OffDeck;
        }

        /// <summary>Whether the player stands where he may haul it: on the deck of its own ship, or a moment in the air.</summary>
        public bool Footed(Player player)
        {
            if (!OnDeck)
                return false;
            Ship? on = player.GetStandingOnShip();
            return on != null ? on.transform == Arm.transform.parent : !player.IsOnGround();
        }

        /// <summary>As the hauler lets go, on its owner: written where it was left, and its breeching made fast there if it can be (<see cref="KwBreechingTie"/>).</summary>
        public void Settle()
        {
            if (OnDeck)
                deck!.End();
            KwBreechingTie.Decide(Arm);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        /// <summary>Its rear is hovered as the whole cannon.</summary>
        public string GetHoverText() => arm == null || arm.Def == null ? "" : arm.GetHoverText();

        /// <summary>The haul line on its hover, for the local player: let go, haul it, or why not from here; nothing while someone mans or hauls it (the hover says so), or out of reach.</summary>
        public string HaulLine()
        {
            if (!Ready || Mine != this && Busy())
                return "";
            return KwArmWords.HaulLine(Mine == this, Mine == this ? null : Hovered());
        }

        // Why the local player may not haul it from here, looked at again a few times a second while he hovers it.
        private string? Hovered()
        {
            Player? me = Player.m_localPlayer;
            if (me != null && Time.time - barredAt > 0.25f)
                (barred, barredAt) = (Barred(me), Time.time);
            return me != null ? barred : "";
        }

        public string GetHoverName() => arm == null || arm.Def == null ? "" : arm.GetHoverName();

        public float GetHoverOffset() => 0f;
    }
}
