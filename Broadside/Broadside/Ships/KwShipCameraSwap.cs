using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's own colliders on the layers the game's camera stops at: its working fittings copied from pieces
    /// (the rudder's blade and post, hatches, a captain's wheel, an anchor windlass) are on the piece layer, and the rudder's
    /// box stands taller than the helmsman's head right behind him. While the game places its camera
    /// (<see cref="KwShipCamera"/>) they are on the Ignore Raycast layer, and back on their own straight after, so the camera
    /// passes through them as through the rest of the ship. Nothing is simulated or drawn in between, and nothing else is
    /// asked of the physics. Looked for again every second (a rudder or hatch built again has new colliders). Local only.
    /// </summary>
    public static class KwShipCameraSwap
    {
        private const float Again = 1f;
        private static readonly List<Collider> all = new List<Collider>();
        private static readonly List<GameObject> found = new List<GameObject>();
        private static readonly List<(GameObject Part, int Layer)> moved = new List<(GameObject, int)>();
        private static KwShipBody? cached;
        private static float looked = float.MinValue;
        private static int foundFor, ignore = -1;

        /// <summary>Moves the ship's colliders on the mask's layers off them, until <see cref="On"/>.</summary>
        public static void Off(KwShipBody ship, int mask)
        {
            if (ship != cached || mask != foundFor || Time.time - looked > Again)
                Find(ship, mask);
            ignore = ignore >= 0 ? ignore : LayerMask.NameToLayer("Ignore Raycast");
            foreach (GameObject part in found)
            {
                if (part == null || ((1 << part.layer) & mask) == 0)
                    continue;
                moved.Add((part, part.layer));
                part.layer = ignore;
            }
        }

        /// <summary>Puts every moved collider back on its own layer.</summary>
        public static void On()
        {
            foreach ((GameObject part, int layer) in moved)
            {
                if (part != null)
                    part.layer = layer;
            }
            moved.Clear();
        }

        // The ship's objects with a collider on one of the mask's layers, each once.
        private static void Find(KwShipBody ship, int mask)
        {
            (cached, foundFor, looked) = (ship, mask, Time.time);
            found.Clear();
            ship.GetComponentsInChildren(false, all);
            foreach (Collider collider in all)
            {
                GameObject part = collider.gameObject;
                if (((1 << part.layer) & mask) != 0 && !found.Contains(part))
                    found.Add(part);
            }
            all.Clear();
        }
    }
}
