using System;
using OpenKeep.Reach;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// What a container offers a hungry pet (<see cref="PetEating"/>) and where the pet stands to eat from it. A food is
    /// one of the creature's own foods (<c>m_consumeItems</c>, its order) matched by shared name, of the world's level
    /// or later (the rule <see cref="NearbyTake"/> applies) and accepted by the container's <c>containers:</c> rule.
    /// </summary>
    public static class PetFood
    {
        /// <summary>A container's radius when it has no collider to measure, about a wooden chest's.</summary>
        private const float DefaultRadius = 0.7f;

        /// <summary>The largest radius taken from a collider, so a ship's hold does not send a wolf metres away.</summary>
        private const float MaxRadius = 1.5f;

        /// <summary>The first of the creature's foods the container holds; null when none.</summary>
        public static ItemDrop In(Container chest, MonsterAI ai)
        {
            foreach (ItemDrop food in ai.m_consumeItems)
            {
                if (food != null && ReachCount.CountIn(chest, Matches(food)) > 0)
                    return food;
            }
            return null;
        }

        public static Func<ItemDrop.ItemData, bool> Matches(ItemDrop food)
        {
            string name = food.m_itemData.m_shared.m_name;
            return item => item.m_shared.m_name == name && item.m_worldLevel >= Game.m_worldLevel;
        }

        /// <summary>Still loaded and still holding the food; the full access checks run again when it is taken.</summary>
        public static bool StillHolds(Container chest, ItemDrop food) =>
            chest != null && chest.m_nview != null && chest.m_nview.IsValid() && chest.GetInventory() != null
            && ReachCount.CountIn(chest, Matches(food)) > 0;

        /// <summary>The point beside the container on the creature's side, where a creature of its size stands to eat.</summary>
        public static Vector3 Spot(MonsterAI ai, Container chest)
        {
            Vector3 centre = chest.transform.position;
            Vector3 away = ai.transform.position - centre;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = Vector3.forward;
            return centre + away.normalized * (Radius(chest) + ai.m_character.GetRadius());
        }

        public static bool InReach(MonsterAI ai, Container chest) =>
            Utils.DistanceXZ(ai.transform.position, chest.transform.position)
            <= Radius(chest) + ai.m_character.GetRadius() + ai.m_consumeRange + 0.5f;

        /// <summary>Half the container's footprint from its collider's world bounds, at most <see cref="MaxRadius"/>.</summary>
        private static float Radius(Container chest)
        {
            Collider collider = chest.GetComponentInChildren<Collider>();
            if (collider == null)
                return DefaultRadius;
            Vector3 extents = collider.bounds.extents;
            return Mathf.Clamp(Mathf.Max(extents.x, extents.z), 0.3f, MaxRadius);
        }
    }
}
