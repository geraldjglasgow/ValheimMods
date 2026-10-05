using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Stow
{
    /// <summary>
    /// Which containers take part in Auto Tidy and which of them may take a stray. Taking part: a piece a player placed
    /// (no dungeon chest or spawned treasure), no ship hold, no cart, not the private chest. Ready now: taking part, not open on this client, and usable as Stow uses containers
    /// (loaded, not in use by another player, section 0 switches, the prefab table, ward and privacy).
    /// </summary>
    internal static class TidyChests
    {
        /// <summary>Metres between two chests for items to move from one to the other.</summary>
        public const float Range = 15f;

        /// <summary>A home must hold this many times the source's share of the item, so items never go back and forth.</summary>
        private const float Margin = 2f;

        public static bool TakesPart(Container container)
        {
            if (container == null || container.m_piece == null || !container.m_piece.IsPlacedByPlayer())
                return false;
            return !ContainerScan.IsShip(container) && !ContainerScan.IsCart(container) && !ContainerScan.IsPrivateChest(container);
        }

        /// <summary>Net view valid and the inventory read from the ZDO at least once.</summary>
        public static bool IsLoaded(Container container) =>
            container.m_nview != null && container.m_nview.IsValid() && container.m_inventory != null && container.m_lastRevision != uint.MaxValue;

        public static bool IsReady(Container container) =>
            TakesPart(container) && !container.IsInUse() && ContainerScan.IsUsable(container, ContainerUse.Stow);

        /// <summary>
        /// The chests within <see cref="Range"/> of the source that are a home for the item - kept there, or holding it
        /// (and what is alike) at least <see cref="Margin"/> times as strongly as the source - nearest first (the user's
        /// rule for every automatic store: the closest chest, then the next closest when it is full). Readiness (the slow
        /// ward check among it) is left to the caller, which needs only the first few.
        /// </summary>
        public static List<Container> Homes(Container source, string prefab, float sourceShare)
        {
            Vector3 origin = source.transform.position;
            List<KeyValuePair<float, Container>> found = new List<KeyValuePair<float, Container>>();
            foreach (Container chest in ContainerScan.All())
            {
                float distance = Vector3.Distance(origin, chest.transform.position);
                if (chest == source || distance > Range || !TakesPart(chest) || !IsLoaded(chest))
                    continue;
                if (IsHome(TidyProfiles.Of(chest), prefab, sourceShare))
                    found.Add(new KeyValuePair<float, Container>(distance, chest));
            }
            found.Sort((a, b) => a.Key.CompareTo(b.Key));
            return found.ConvertAll(pair => pair.Value);
        }

        /// <summary>Kept there, or alike enough (the cheap test first), in a chest that is no junk chest.</summary>
        private static bool IsHome(TidyProfile profile, string prefab, float sourceShare) =>
            profile.IsKept(prefab) || (profile.Share(prefab) >= Margin * sourceShare && profile.IsHomeFor(prefab));
    }
}
