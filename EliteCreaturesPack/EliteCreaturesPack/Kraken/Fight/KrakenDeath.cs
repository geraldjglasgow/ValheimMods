using UnityEngine;

namespace EliteCreaturesPack.Kraken
{
    /// <summary>
    /// OWNER, as the kraken dies. The game makes a creature's corpse where its body is and drops its loot round it; the
    /// kraken's body may be deep under a ship, so first it is moved to where its head is seen (up beside the ship, facing
    /// it, or where it swam), a little under the surface, and its loot is aimed at the held ship's deck (or just over the
    /// water where it died, where the game's items float).
    /// </summary>
    public static class KrakenDeath
    {
        private const float CorpseDepth = 1.5f;
        private const float OverDeck = 0.6f;

        public static void Prepare(KrakenBrain brain)
        {
            Ship? ship = Held(brain);
            Vector3 at = ship != null
                ? KrakenTargets.HeadSpot(ship, brain.State.Side, brain.State.Along, CorpseDepth)
                : Surface(brain.transform.position, -CorpseDepth);
            Vector3 facing = ship != null ? ship.transform.position - at : brain.transform.forward;
            facing.y = 0f;
            Quaternion turn = facing.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(facing) : brain.transform.rotation;
            brain.transform.SetPositionAndRotation(at, turn);
            brain.Character.m_body.position = at;
        }

        public static void AimLoot(KrakenBrain brain, CharacterDrop drop)
        {
            Ship? ship = Held(brain);
            ShipHull? hull = ship != null ? ShipHull.Of(ship) : null;
            Vector3 target = ship != null && hull != null
                ? ship.transform.TransformPoint(new Vector3(0f, hull.Rail + OverDeck, hull.MiddleZ))
                : Surface(brain.transform.position, OverDeck);
            drop.m_spawnOffset = drop.transform.InverseTransformVector(target - brain.Character.GetCenterPoint());
        }

        private static Ship? Held(KrakenBrain brain) => brain.State.Grips ? KrakenShips.Find(brain.State.Ship) : null;

        private static Vector3 Surface(Vector3 at, float up) => new Vector3(at.x, KrakenShips.Water(at) + up, at.z);
    }
}
