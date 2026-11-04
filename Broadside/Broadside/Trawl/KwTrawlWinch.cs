using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A net winch in the world, on every peer that has its object (a placement ghost, without a ZDO, does nothing). Its
    /// state is its ZDO's (<see cref="KwTrawlState"/>), written only by its owner (<see cref="KwTrawlOwner"/>: the steps,
    /// the catch, the fish landed on the deck); E asks the owner (<see cref="UseCall"/>, routed to it) to cast the net or
    /// haul it in, after this game has checked it may (<see cref="KwTrawlCast"/>). Every peer follows the net in the water
    /// itself (<see cref="KwTrawlTow"/>, from the ship's speed as it sees it, <see cref="KwTrawlSpeed"/>), tells the ship's
    /// drag how much of its rope is out (<see cref="KwTrawlDrag"/>, used by the ship's owner) and, where there is a screen,
    /// draws it (<see cref="KwTrawlLook"/>); a player aboard its ship is told when the net snags or fills. The hover says
    /// what E does and how full the net is (<see cref="KwTrawlWords"/>).
    /// </summary>
    public sealed class KwTrawlWinch : MonoBehaviour, Hoverable, Interactable
    {
        public const string UseCall = "BS_Trawl_Use";
        public const int Cast = 1, Haul = 2;
        private const float LookEvery = 0.5f;

        private readonly KwTrawlTow tow = new KwTrawlTow();
        private readonly KwTrawlSpeed speed = new KwTrawlSpeed();
        private KwTrawlLook? look;
        private KwTrawlOwner? owner;
        private KwShipRiders? ship;
        private Transform? sheave;
        private System.Action? tick;
        private KwTrawlFare? told;
        private float nextLook;

        public ZNetView Net { get; private set; } = null!;

        public ZDO? Zdo => Net != null && Net.IsValid() ? Net.GetZDO() : null;

        /// <summary>The launched ship it rides in this game's scene now, or null (on land, or its ship not loaded here).</summary>
        public KwShipRiders? Ship => ship != null && ship.Zdo != null ? ship : null;

        /// <summary>Whether its ZDO says it rides a ship (loaded here or not).</summary>
        public bool Riding => Zdo is ZDO zdo && zdo.GetLong(KwShipRider.ShipHash, 0L) != 0L;

        public KwTrawlTow Tow => tow;

        /// <summary>The ship's speed through the water as this game sees it (m/s).</summary>
        public float Speed => speed.Value;

        /// <summary>The davit's sheave, where the tow rope leaves the winch.</summary>
        public Transform Sheave => sheave != null ? sheave : transform;

        /// <summary>How much of the rope is out with the net in the water (0 to 1), for the ship's drag.</summary>
        public float InWater { get; private set; }

        private void Awake()
        {
            Net = GetComponent<ZNetView>();
            sheave = transform.Find(KwTrawlBuild.Davit + "/" + KwTrawlBuild.Sheave);
            if (Net == null || Net.GetZDO() == null)
            {
                enabled = false;
                return;
            }
            Net.Register<int>(UseCall, (sender, want) => KwShipBody.Step("Broadside trawl use", () => owner?.Use(want)));
            owner = new KwTrawlOwner(this);
            // removed or broken (on the game that destroys it, its owner): what is in its net spills where it stood
            if (TryGetComponent(out WearNTear wear))
                wear.m_onDestroyed += () => KwShipBody.Step("Broadside trawl spill", () => KwTrawlDrop.Spill(this));
            if (KwShipBody.Drawn)
                look = new KwTrawlLook(transform);
        }

        private void OnDestroy()
        {
            KwTrawlDrag.Remove(ship, this);
            look?.Destroy();
        }

        private void Update() => KwShipBody.Step("Broadside trawl winch", tick ??= Tick);

        private void Tick()
        {
            ZDO? zdo = Zdo;
            if (zdo == null)
                return;
            if (Time.time >= nextLook)
                Board();
            KwTrawlStep step = KwTrawlState.Step(zdo);
            float t = KwTrawlState.Since(zdo), dt = Time.deltaTime;
            speed.Step(Ship != null ? Ship.transform.position : transform.position, dt);
            Pull(step, t, dt);
            if (zdo.IsOwner())
                owner!.Tick(zdo, step, t);
            look?.Draw(this, step, t, dt);
            Tell(zdo);
        }

        // The ship it rides now (its rider's), the ship's drag told of it as it comes and goes.
        private void Board()
        {
            nextLook = Time.time + LookEvery;
            KwShipRiders? on = TryGetComponent(out KwShipRider rider) ? rider.On : null;
            if (on == ship)
                return;
            KwTrawlDrag.Remove(ship, this);
            ship = on;
            if (ship != null)
                KwTrawlDrag.Add(ship, this);
        }

        // The net in the water followed; out of it, forgotten, so the next cast begins at the davit.
        private void Pull(KwTrawlStep step, float t, float dt)
        {
            float rope = KwTrawlTune.Rope(step, t);
            InWater = KwTrawlState.InWater(step) ? rope / KwTrawlTune.RopeOut : 0f;
            if (InWater <= 0f)
            {
                tow.Reset();
                return;
            }
            Vector3 astern = Ship != null ? -Ship.transform.forward : -transform.forward;
            tow.Step(Sheave.position, astern, speed.Value, rope, dt);
        }

        // A word on this screen as the net snags or fills, for the local player aboard its ship (not for what a late joiner finds).
        private void Tell(ZDO zdo)
        {
            KwTrawlFare fare = KwTrawlState.Fare(zdo);
            bool heard = told.HasValue;
            if (heard && told == fare)
                return;
            told = fare;
            string? words = fare == KwTrawlFare.Snagged ? KwTrawlWords.SnaggedNow : fare == KwTrawlFare.Full ? KwTrawlWords.FullNow : null;
            Player? me = Player.m_localPlayer;
            if (heard && words != null && me != null && Ship != null && Ship.TryGetComponent(out Ship game) && game.IsPlayerInBoat(me))
                me.Message(MessageHud.MessageType.Center, words);
        }

        public bool Interact(Humanoid user, bool hold, bool alt) => !hold && Zdo != null && KwTrawlCast.Use(this, user);

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText() => Zdo is ZDO zdo ? KwTrawlWords.Hover(this, zdo) : KwTrawl.Label;

        public string GetHoverName() => KwTrawl.Label;

        public float GetHoverOffset() => 0f;
    }
}
