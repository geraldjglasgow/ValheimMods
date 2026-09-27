using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The stations auto feed acts on, where it acts, and how far they reach: a <c>Smelter</c> (the game's class for
    /// smelters, kilns, refineries, spinning wheels, windmills and the hot tub; modded ones too) whose ZDO this machine
    /// owns (the game ticks every loaded station on every client, only the owner works it) and whose <c>Piece</c> a
    /// player built. The battering ram is left alone: its <c>SiegeMachine</c> engine is a <c>Smelter</c> that rams
    /// while it holds wood and burns one every 20 s, so feeding it would ram and eat a chest's wood for nothing.
    /// </summary>
    public static class FeedStations
    {
        private static readonly List<Collider> colliders = new List<Collider>();

        public static bool OwnedPlayerStation(Smelter station)
        {
            ZNetView view = station != null ? station.m_nview : null;
            if (view == null || !view.IsValid() || !view.IsOwner())
                return false;
            Piece piece = view.GetComponent<Piece>();
            return piece != null && piece.IsPlacedByPlayer() && view.GetComponentInChildren<SiegeMachine>() == null;
        }

        /// <summary>
        /// The station's outline in the world: the box around its solid colliders. Triggers (the base and warmth
        /// areas, 5 to 20 m wide) are left out; a station without solid colliders is the point it stands on.
        /// </summary>
        public static Bounds Outline(Smelter station)
        {
            Transform root = station.m_nview != null ? station.m_nview.transform : station.transform;
            Bounds outline = new Bounds(root.position, Vector3.zero);
            bool first = true;
            root.GetComponentsInChildren(false, colliders);
            foreach (Collider collider in colliders)
            {
                if (!collider.enabled || collider.isTrigger)
                    continue;
                if (first)
                    outline = collider.bounds;
                else
                    outline.Encapsulate(collider.bounds);
                first = false;
            }
            colliders.Clear();
            return outline;
        }
    }
}
