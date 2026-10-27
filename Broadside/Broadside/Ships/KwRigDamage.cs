using System;
using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's rig damage (the user, 2026-10-26: "Can it do damage to sails? Holes through sails. If you hit wood,
    /// damage the mast that holds the sail, and the sail then droops according to how it was damaged ... There should be a
    /// speed impact as well. If you shoot the post cap, it should get damaged too."), on every peer. Its marks
    /// (<see cref="KwRigMark"/>: notches in spars, holes in sails, chips off caps, chunks off a rudder, pieces fallen) live in
    /// the ship's ZDO (<see cref="KwRigMarks.Key"/>), written only by the ship's owner: by the shot router's takers
    /// (<see cref="KwRigDamageTakers"/>), by a shot that tore through a sail on the shooter's game (<see cref="ShotRpc"/>,
    /// <see cref="KwSailDamagePierce"/>), and by the hammer (<see cref="MendRpc"/>, <see cref="RaiseRpc"/>,
    /// <see cref="KwRigDamageMend"/>). Every peer reads them (<see cref="KwRigCondition"/>), sizes its hit colliders
    /// (<see cref="KwRigDamageHits"/>), and its ship's steering (<see cref="KwRigDamageDrive"/>); a screen draws them
    /// (<see cref="KwRigDamageLooks"/>), a piece that falls while it watches toppling over (<see cref="KwMastFall"/>).
    /// </summary>
    public sealed class KwRigDamage : MonoBehaviour
    {
        public const string ShotRpc = "KW_SailShot", MendRpc = "KW_RigMend", RaiseRpc = "KW_RigRaise", RaisedRpc = "KW_RigRaised";
        private const float Poll = 0.25f;

        private ZNetView? view;
        private string text = "";
        private float polled;
        private bool read, broken;

        public KwRigParts Parts { get; private set; } = new KwRigParts();
        public KwRigCondition Condition { get; private set; } = null!;
        public List<KwRigMark> Marks { get; private set; } = new List<KwRigMark>();

        /// <summary>The ship's frame its blueprint is in (its "ship" child, where its colliders and looks are).</summary>
        public Transform Frame { get; private set; } = null!;

        /// <summary>Counts up whenever the marks change.</summary>
        public int Version { get; private set; }

        public KwRigDamageHits? Hits { get; set; }
        public KwRigDamageLooks? Looks { get; set; }

        /// <summary>Its rig damage on a launched ship built from a blueprint; none for a ship with no mast, bowsprit, cap or rudder.</summary>
        public static KwRigDamage? Add(Transform ship, KwBlueprint blueprint)
        {
            KwRigParts parts = KwRigParts.Of(blueprint);
            Transform? frame = ship.Find("ship");
            if (parts.Pieces.Count == 0 || frame == null)
                return null;
            KwRigDamageTakers.Install();
            KwRigDamage damage = ship.gameObject.AddComponent<KwRigDamage>();
            (damage.Parts, damage.Frame, damage.view) = (parts, frame, ship.GetComponent<ZNetView>());
            damage.Condition = KwRigCondition.Of(parts, damage.Marks);
            damage.Register();
            KwShipBody.Step("Broadside rig hits", () => damage.Hits = KwRigDamageHits.Add(damage));
            if (KwShipBody.Drawn)
                KwShipBody.Step("Broadside rig looks", () => damage.Looks = KwRigDamageLooks.Add(damage));
            return damage;
        }

        /// <summary>
        /// On every peer, the ship's blueprint changed at sea (<see cref="KwSeaRigRedraw"/>): the rig read again from it, the
        /// remade pieces' hit colliders and looks made again (their fittings already are), every other piece's kept, and the
        /// ZDO's marks read at once (the owner dropped a changed or removed piece's own in the same frame as the blueprint).
        /// </summary>
        public void Refit(KwBlueprint blueprint, ICollection<int> remade)
        {
            Parts = KwRigParts.Of(blueprint);
            KwShipBody.Step("Broadside rig hits again", () => Hits?.Refit(this, remade));
            KwShipBody.Step("Broadside rig looks again", () => Looks?.Refit(this, remade));
            read = false;
            Read();
        }

        private void Register()
        {
            if (view == null || !view.IsValid())
                return;
            view.Register<ZPackage>(ShotRpc, (sender, package) => Guard.Run("Broadside sail shot", () => KwRigDamageTakers.Pierced(this, package)));
            view.Register<ZPackage>(MendRpc, (sender, package) => Guard.Run("Broadside rig mend", () => Mend(package)));
            view.Register<int>(RaiseRpc, (sender, piece) => Guard.Run("Broadside rig raise", () => Raise(sender, piece)));
            view.Register<int>(RaisedRpc, (sender, piece) => Guard.Run("Broadside rig raised", () => KwRigDamageMend.Raised(this, piece)));
        }

        private void OnDestroy()
        {
            if (view == null || !view.IsValid())
                return;
            foreach (string rpc in new[] { ShotRpc, MendRpc, RaiseRpc, RaisedRpc })
                view.Unregister(rpc);
        }

        // A throw is reported once and stops this ship's rig damage, rather than once a poll.
        private void Update()
        {
            if (broken || (read && Time.time - polled < Poll))
                return;
            (polled, broken) = (Time.time, true);
            Guard.Run("Broadside rig damage", Read);
            broken = false;
        }

        // The ZDO's marks, read again only when their text changed; what changed handed to the colliders, steering and looks.
        private void Read()
        {
            string now = view != null && view.IsValid() ? view.GetZDO().GetString(KwRigMarks.Hash) : "";
            if (read && now == text)
                return;
            List<int> fell = new List<int>();
            KwRigCondition was = Condition;
            (text, Marks) = (now, KwRigMarks.Read(now));
            Condition = KwRigCondition.Of(Parts, Marks);
            foreach (KwRigPiece piece in Parts.Pieces)
            {
                if (read && Condition.Fallen(piece.Index) && !was.Fallen(piece.Index))
                    fell.Add(piece.Index);
            }
            (read, Version) = (true, Version + 1);
            Changed(fell);
        }

        private void Changed(List<int> fell)
        {
            KwShipBody.Step("Broadside rig hits shown", () => Hits?.Show(Condition));
            KwShipBody.Step("Broadside rig steering", () => KwRigDamageDrive.Steer(this));
            KwShipBody.Step("Broadside rig looks shown", () => Looks?.Show(Condition, fell));
            if (fell.Count > 0)
                Tell(fell);
        }

        // A player aboard is told what fell.
        private void Tell(List<int> fell)
        {
            Player? me = Player.m_localPlayer;
            Ship? ship = GetComponent<Ship>();
            KwRigPiece? piece = Parts.Get(fell[0]);
            if (me != null && ship != null && piece != null && ship.IsPlayerInBoat(me))
                me.Message(MessageHud.MessageType.Center, $"{piece.Called} is shot down! The hammer's repair at its stump puts it back");
        }

        /// <summary>
        /// On the owner: the marks changed by a function (handed the ZDO's marks as they are now; false when it changed
        /// nothing), written back and read again here.
        /// </summary>
        public bool Edit(Func<List<KwRigMark>, bool> change)
        {
            if (view == null || !view.IsValid() || !view.IsOwner())
                return false;
            ZDO zdo = view.GetZDO();
            List<KwRigMark> marks = KwRigMarks.Read(zdo.GetString(KwRigMarks.Hash));
            if (!change(marks))
                return false;
            zdo.Set(KwRigMarks.Hash, KwRigMarks.Write(marks));
            Read();
            return true;
        }

        // On the owner: a part's damage mended (KwRigDamageMend.Ask): its marks of that type gone; a fall stays.
        private void Mend(ZPackage package)
        {
            int piece = package.ReadInt(), part = package.ReadInt();
            char type = (char)package.ReadInt();
            Edit(marks => marks.RemoveAll(m => Mends(m, piece, type, part)) > 0);
        }

        /// <summary>
        /// Whether a mend of a type and part of a piece takes a mark: a spar's notches; a sail's holes and the notches in its
        /// yard, boom and gaff (its lashings and lacing with them); a cap's chips and notches; a rudder's chunks. Never a fall
        /// (the hammer puts a fallen piece back, <see cref="RaiseRpc"/>).
        /// </summary>
        public static bool Mends(KwRigMark mark, int piece, char type, int part) => mark.Piece == piece && mark.Type != KwRigMark.Fall && type switch
        {
            KwRigMark.Chip => mark.Type == KwRigMark.Chip || mark.Type == KwRigMark.Notch,
            KwRigMark.Chunk => mark.Type == KwRigMark.Chunk,
            KwRigMark.Hole => mark.Part == part ? mark.Type == KwRigMark.Hole || mark.Type == KwRigMark.Notch
                : mark.Type == KwRigMark.Notch && part >= 0 && (mark.Part == KwRigSpar.Boom + part || mark.Part == KwRigSpar.Gaff + part),
            _ => mark.Type == type && mark.Part == part,
        };

        // On the owner: a fallen piece put back, every mark of it gone; the asker told, who then pays for it.
        private void Raise(long sender, int piece)
        {
            if (!Condition.Fallen(piece) || !Edit(marks => marks.RemoveAll(m => m.Piece == piece) > 0))
                return;
            view!.InvokeRPC(sender, RaisedRpc, piece);
        }
    }
}
