using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's holes (<see cref="KwHullHole"/>), on every peer. The owner makes and widens them as the ship's wear
    /// takes damage at a point (<see cref="Take"/>, by <see cref="KwHullDamage"/>'s rule) and closes one hole at a time as a
    /// player mends the ship with the hammer (<see cref="MendRpc"/>, asked by the repairing player's game, <see cref="AskMend"/>,
    /// <see cref="KwShipRepair"/>). The ship's ZDO keeps them (<see cref="KwHullHoleText"/>), so every peer cuts the same holes
    /// through its look and boxes (<see cref="KwHoleLooks"/>, <see cref="KwHoleBoxes"/>) and whoever owns the ship lets the
    /// sea in through those not patched (<see cref="Leaking"/>, <see cref="KwHullPatches"/>, <see cref="KwShipFlood"/>). A
    /// player aboard is told when a new hole below the waterline is made.
    /// </summary>
    public sealed class KwHullHoles : MonoBehaviour
    {
        public const string MendRpc = "KW_HoleMend";
        private const float Poll = 0.5f, BelowWaterline = 0.2f;

        private ZNetView? view;
        private Ship? ship;
        private KwBlueprint? blueprint;
        private KwHullPartMap? map;
        private string text = "";
        private float polled, waterline;

        /// <summary>The holes as the ZDO last held them.</summary>
        public List<KwHullHole> Holes { get; private set; } = new List<KwHullHole>();

        /// <summary>The ship's patches over its holes (a Wooden Repair Kit's, <see cref="KwHullPatches"/>).</summary>
        public KwHullPatches? Patches { get; set; }

        /// <summary>The holes that let the sea in: all but those patched.</summary>
        public List<KwHullHole> Leaking => Patches != null ? Patches.Leaking : Holes;

        /// <summary>Counts up whenever the holes change.</summary>
        public int Version { get; private set; }

        /// <summary>The ship's blueprint, in its own frame (its hull parts tell where the hold's bulkheads go).</summary>
        public KwBlueprint? Blueprint => blueprint;

        public void Set(KwBlueprint blueprint, float waterline)
        {
            (view, ship, this.blueprint, this.waterline) = (GetComponent<ZNetView>(), GetComponent<Ship>(), blueprint, waterline);
            if (view == null || !view.IsValid())
                return;
            view.Register<int>(MendRpc, (sender, id) => Guard.Run("Broadside hull mend", () => Mend(id)));
            text = view.GetZDO().GetString(KwHullHoleText.HolesHash);
            (Holes, polled) = (KwHullHoleText.Read(text), Time.time);
            Version++;
        }

        private void Update()
        {
            if (Time.time - polled < Poll)
                return;
            polled = Time.time;
            Read();
        }

        // The ZDO's holes, read again only when their text changed; a new one below the waterline told to a player aboard.
        private void Read()
        {
            string now = view != null && view.IsValid() ? view.GetZDO().GetString(KwHullHoleText.HolesHash) : "";
            if (now == text)
                return;
            int before = Below(Holes);
            (text, Holes) = (now, KwHullHoleText.Read(now));
            Version++;
            Player? me = Player.m_localPlayer;
            if (Below(Holes) > before && me != null && ship != null && ship.IsPlayerInBoat(me))
                me.Message(MessageHud.MessageType.Center, "The hull is holed below the waterline! Patch it with a Wooden Repair Kit, bail with B in the hold");
        }

        private int Below(List<KwHullHole> holes)
        {
            int count = 0;
            foreach (KwHullHole hole in holes)
                count += hole.Place.y < waterline + BelowWaterline ? 1 : 0;
            return count;
        }

        /// <summary>
        /// On the owner: damage the ship's wear took at a point of the world, coming along a way of the world (zero when
        /// not known; with face, the struck face's normal), put on the hull part it strikes (<see cref="KwHullPartMap.Nearest"/>).
        /// </summary>
        public void Take(Vector3 point, Vector3 way, float damage, float health, bool face = false)
        {
            if (view == null || !view.IsValid() || !view.IsOwner() || blueprint == null)
                return;
            map ??= KwHullPartMap.Of(blueprint);
            ZDO zdo = view.GetZDO();
            List<KwHullHole> holes = KwHullHoleText.Read(zdo.GetString(KwHullHoleText.HolesHash));
            Dictionary<int, float> wear = KwHullHoleText.ReadWear(zdo.GetString(KwHullHoleText.WearHash));
            Matrix4x4 into = Into();
            if (!KwHullDamage.Take(map, holes, wear, into.MultiplyPoint3x4(point), into.MultiplyVector(way), damage, health, face))
                return;
            zdo.Set(KwHullHoleText.HolesHash, KwHullHoleText.Write(holes));
            zdo.Set(KwHullHoleText.WearHash, KwHullHoleText.WriteWear(wear));
            Read();
        }

        /// <summary>
        /// On the owner: a heavy shot through the hull part it strikes at a point of the world, the part found facing a way
        /// of the world (the struck face's normal), holed there at once to a radius (<see cref="KwHullDamage.Pierce"/>).
        /// </summary>
        public void Pierce(Vector3 point, Vector3 way, float radius)
        {
            if (view == null || !view.IsValid() || !view.IsOwner() || blueprint == null)
                return;
            map ??= KwHullPartMap.Of(blueprint);
            ZDO zdo = view.GetZDO();
            List<KwHullHole> holes = KwHullHoleText.Read(zdo.GetString(KwHullHoleText.HolesHash));
            Dictionary<int, float> wear = KwHullHoleText.ReadWear(zdo.GetString(KwHullHoleText.WearHash));
            Matrix4x4 into = Into();
            if (!KwHullDamage.Pierce(map, holes, wear, into.MultiplyPoint3x4(point), into.MultiplyVector(way), radius))
                return;
            zdo.Set(KwHullHoleText.HolesHash, KwHullHoleText.Write(holes));
            zdo.Set(KwHullHoleText.WearHash, KwHullHoleText.WriteWear(wear));
            Read();
        }

        // The world into the ship's frame as its body stands (where its colliders are, which the hits struck), not as its
        // look is drawn between two physics steps.
        private Matrix4x4 Into()
        {
            Rigidbody? body = ship != null ? ship.m_body : null;
            return body != null ? Matrix4x4.TRS(body.position, body.rotation, Vector3.one).inverse : transform.worldToLocalMatrix;
        }

        // On the owner: one hole closed (a player mended it with the hammer), its part's wear gone with the part's last hole;
        // a patch on it is off with it (KwHullPatches).
        private void Mend(int id)
        {
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            ZDO zdo = view.GetZDO();
            List<KwHullHole> holes = KwHullHoleText.Read(zdo.GetString(KwHullHoleText.HolesHash));
            int i = holes.FindIndex(hole => hole.Id == id);
            if (i < 0)
                return;
            int part = holes[i].Part;
            holes.RemoveAt(i);
            Dictionary<int, float> wear = KwHullHoleText.ReadWear(zdo.GetString(KwHullHoleText.WearHash));
            if (!holes.Exists(hole => hole.Part == part))
                wear.Remove(part);
            zdo.Set(KwHullHoleText.HolesHash, KwHullHoleText.Write(holes));
            zdo.Set(KwHullHoleText.WearHash, KwHullHoleText.WriteWear(wear));
            Read();
        }

        /// <summary>On the repairing player's game: one hole (by its number, <see cref="KwHullHole.Id"/>) asked mended by the ship's owner.</summary>
        public void AskMend(int id)
        {
            if (view != null && view.IsValid())
                view.InvokeRPC(MendRpc, id);
        }
    }
}
