using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Companion planting: how many other kinds of crop grow or stand ripe near a crop, up to Companion Kinds. A growing
    /// crop is a Plant, a ripe one a Pickable; both count by their plant's kind (<see cref="CropPlant.Kind"/>), so a seed
    /// plant and its crop plant are one kind and a field of carrots beside its seed carrots has no companions.
    /// </summary>
    public static class Companions
    {
        private static readonly Collider[] Hits = new Collider[128];
        private static readonly HashSet<string> kinds = new HashSet<string>();

        public static int Count(Vector3 position, CropPlant crop)
        {
            int max = FarmingSettings.CompanionKinds.Value;
            float radius = FarmingSettings.CompanionRadius.Value;
            if (crop == null || max <= 0 || radius <= 0f)
                return 0;
            kinds.Clear();
            int count = Physics.OverlapSphereNonAlloc(position, radius, Hits, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                string kind = KindOf(Hits[i]);
                if (kind != null && kind != crop.Kind)
                    kinds.Add(kind);
            }
            return Mathf.Min(kinds.Count, max);
        }

        private static string KindOf(Collider hit)
        {
            if (hit == null)
                return null;
            Plant plant = hit.GetComponentInParent<Plant>();
            if (plant != null)
                return CropCatalog.OfPlant(plant)?.Kind;
            Pickable pickable = hit.GetComponentInParent<Pickable>();
            return pickable != null ? CropCatalog.OfPickable(Utils.GetPrefabName(pickable.gameObject))?.Kind : null;
        }
    }
}
