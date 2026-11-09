using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The catch onto the deck, on the winch's owner only (one game spawns the fish, so each is made once): each fish in
    /// the net is the game's own fish item, dead (<see cref="KwTrawlDead"/>), made as a networked item under the bag where it hangs with the davit swung
    /// in (spread round it a little), standing on whatever of its ship is under it (a ray where the ship's colliders are,
    /// carried to its frame; else the winch's own deck), marked as riding that ship from there (<see cref="KwTrawlDeck"/>:
    /// the ship's own id, its place and turn in the ship's frame, how far it falls and when), so every peer draws it falling
    /// out of the bag onto the deck and riding the ship until someone picks it up. The net is emptied in the ZDO first. A
    /// winch removed or broken with fish in its net spills them where it stood (<see cref="Spill"/>), riding its ship.
    /// </summary>
    public static class KwTrawlDrop
    {
        private const float Spread = 0.45f, Lift = 0.05f, Reach = 4f;

        /// <summary>The bag's foot with the davit swung in (winch kit: the sheave turned about the post, the bunched bag under it).</summary>
        private static readonly Vector3 Bag = new Vector3(KwTrawlShapes.PivotAt.x - KwTrawlShapes.SheaveAt.x, 0f, 0.5f);
        private static readonly Vector3 Foot = new Vector3(-0.8f, 0f, 0.4f);
        private static readonly RaycastHit[] hits = new RaycastHit[16];
        private static int mask;

        /// <summary>On the owner: the net emptied onto the deck under the bag.</summary>
        public static void Land(KwTrawlWinch winch, KwShipRiders ship, ZDO zdo) => Pour(winch, ship, zdo, Bag);

        /// <summary>On the owner, the winch removed or broken: what was in its net, where it stood.</summary>
        public static void Spill(KwTrawlWinch winch)
        {
            ZDO? zdo = winch.Zdo;
            if (zdo != null && zdo.IsOwner() && KwTrawlState.Count(zdo) > 0)
                Pour(winch, winch.Ship, zdo, Foot);
        }

        private static void Pour(KwTrawlWinch winch, KwShipRiders? ship, ZDO zdo, Vector3 kit)
        {
            List<string> fish = KwTrawlState.Catch(zdo);
            KwTrawlState.Empty(zdo);
            long at = ZNet.instance != null ? ZNet.instance.GetTime().Ticks : 0L;
            for (int i = 0; i < fish.Count; i++)
            {
                Vector3 start = winch.transform.TransformPoint(KwMesh.ToUnity(kit + Ring(i, fish.Count)));
                if (ship != null && ship.Id != 0L)
                    Aboard(fish[i], winch, ship, start, at);
                else
                    Loose(fish[i], start);
            }
        }

        // A spot round the middle (kit x and y), sunflower-wise, so the fish lie apart.
        private static Vector3 Ring(int i, int count)
        {
            float r = Spread * Mathf.Sqrt((i + 0.5f) / Mathf.Max(count, 1)), a = i * 2.39996f;
            return new Vector3(r * Mathf.Cos(a), r * Mathf.Sin(a), 0f);
        }

        // A fish riding the ship from where it lands, lying on its side, turned by chance.
        private static void Aboard(string name, KwTrawlWinch winch, KwShipRiders ship, Vector3 start, long at)
        {
            GameObject? prefab = Fish(name);
            if (prefab == null)
                return;
            (Vector3 local, float fall) = Landing(winch, ship, start);
            Quaternion turn = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Quaternion.Euler(0f, 0f, 90f);
            (Vector3 world, Quaternion rot) = KwShipRiderPose.World(ship.transform.position, ship.transform.rotation, local, turn);
            GameObject made = Object.Instantiate(prefab, world, rot);
            ZDO? zdo = made.GetComponent<ZNetView>()?.GetZDO();
            ItemDrop.OnCreateNew(made);
            KwTrawlDead.Mark(made, zdo);
            if (zdo == null)
                return;
            KwTrawlDeck.Mark(zdo, ship.Id, local, turn, fall, at);
            made.AddComponent<KwTrawlDeck>();
        }

        // Off any ship: an ordinary item where the winch stood.
        private static void Loose(string name, Vector3 at)
        {
            GameObject? prefab = Fish(name);
            if (prefab == null)
                return;
            GameObject made = Object.Instantiate(prefab, at, Quaternion.Euler(0f, Random.Range(0f, 360f), 90f));
            ItemDrop.OnCreateNew(made);
            KwTrawlDead.Mark(made, made.GetComponent<ZNetView>()?.GetZDO());
        }

        private static GameObject? Fish(string name)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null || prefab.GetComponent<ItemDrop>() == null)
                Log.Warn($"Broadside: the trawl caught a {name}, which the game does not have; left out.");
            return prefab != null && prefab.GetComponent<ItemDrop>() != null ? prefab : null;
        }

        // Where a fish let go at a point lands on its ship (the ship's frame) and how far it falls.
        private static (Vector3 Local, float Fall) Landing(KwTrawlWinch winch, KwShipRiders ship, Vector3 start)
        {
            Transform s = ship.transform;
            Vector3 local = KwShipRiderPose.Local(s.position, s.rotation, start, Quaternion.identity).At;
            float deck = Deck(winch, ship, local);
            float fall = Mathf.Max(0f, local.y - deck);
            local.y = deck + Lift;
            return (local, fall);
        }

        // The height (ship frame) of what of the ship is under a point, by a ray where its colliders are; else the winch's foot.
        private static float Deck(KwTrawlWinch winch, KwShipRiders ship, Vector3 local)
        {
            (Vector3 at, Quaternion turn) = ship.Pose;
            Rigidbody? body = ship.GetComponent<Rigidbody>();
            if (mask == 0)
                mask = LayerMask.GetMask("Default", "piece", "vehicle");
            int count = Physics.RaycastNonAlloc(at + turn * local, turn * Vector3.down, hits, Reach, mask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i].collider;
                if (hit != null && hit.attachedRigidbody == body && !hit.transform.IsChildOf(winch.transform) && hits[i].distance < nearest)
                    nearest = hits[i].distance;
            }
            if (nearest < float.MaxValue)
                return local.y - nearest;
            return KwShipRiderPose.Local(ship.transform.position, ship.transform.rotation, winch.transform.position, Quaternion.identity).At.y;
        }
    }
}
