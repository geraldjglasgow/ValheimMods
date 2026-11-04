using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What every sea edit redraws besides the kinds' own parts, on every peer: the ship's figures from its blueprint as it
    /// is now (<see cref="KwShipStats"/>: its speed, its sails' push and its sails by kind, its health, its steering;
    /// its weight, which is its hull's water, unchanged), and its ladder over the side
    /// placed again a frame later (<see cref="KwShipLadder"/>: it keeps clear of posts, shield hooks, openings and ladders), its old mesh
    /// let go; the shadow's boxes made again the frame after (<see cref="KwShipShadow"/>), so no one frame takes them all; the body's mass, centre and turning put back as they were (<see cref="KwBodyMass"/>).
    /// </summary>
    public static class KwSeaRedraw
    {
        public static void Common(KwSeaShip ship, KwSeaChanges changes)
        {
            KwShipBody.Step("Broadside sea stats", () => Stats(ship));
            ship.Later(ship.LadderLater);
            ship.Later(ship.ShadowLater);
        }

        /// <summary>The ship's shadow boxes made again from its colliders as they are now (<see cref="KwShipShadow"/>); screens only.</summary>
        public static void Shadow(KwSeaShip ship)
        {
            if (!KwShipBody.Drawn)
                return;
            Transform? old = ship.Visual.Find("shadow");
            if (old != null)
            {
                old.gameObject.SetActive(false);
                old.name = "shadow_old";
                Object.Destroy(old.gameObject);
            }
            KwShipShadow.Add(ship.transform, ship.Blueprint);
        }

        /// <summary>The ship's figures again: what its build sets, onto its trim (<see cref="KwShipTrim"/>).</summary>
        public static void Stats(KwSeaShip ship)
        {
            Ship game = ship.GetComponent<Ship>();
            KwShipHydro? hydro = ship.GetComponent<KwShipHydro>();
            if (game == null || hydro == null)
                return;
            KwShipStats stats = KwShipStats.Of(ship.Blueprint, ship.Hull, KwShipWeight.Sized(hydro.Slices, ship.Hull));
            stats.Trim(game);
            KwShipTrim.Of(game).Hold();
            KwShipPerks.Apply(game, ship.GetComponent<WearNTear>(), stats);
        }

        /// <summary>The ladder over the side placed again from the blueprint as it is now, its old mesh let go.</summary>
        public static void Ladder(KwSeaShip ship)
        {
            KwBodyMass kept = KwBodyMass.Of(ship.Rigid);
            Transform? ladder = ship.transform.Find("interactive/ladder");
            Mesh? before = ladder?.GetComponent<MeshFilter>()?.sharedMesh, supports = ladder?.Find("supports")?.GetComponent<MeshFilter>()?.sharedMesh;
            KwShipLadder.Place(ship.transform, ship.Blueprint, ship.Hull);
            foreach (Mesh? old in new[] { before, supports })
            {
                if (old != null && (old.name == "KW_gen_ladder" || old.name == "kw_ladder_supports") && old != ladder?.GetComponent<MeshFilter>()?.sharedMesh)
                    Object.Destroy(old);
            }
            kept.Put(ship.Rigid);
        }
    }
}
