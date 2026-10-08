using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// Placeholders the workshop painted with another game item's artwork, worn with that item's own material: Iron v019's
    /// shin and upper thigh plates use the iron chest's (<c>IronStudy_IronArmorChest_mat</c>, the cuirass's
    /// <c>IronArmorChest_mat</c>). Found once in the prefab list the boots are made from; a missing one is logged and its
    /// pieces wear the set's own material.
    /// </summary>
    internal static class BorrowedLooks
    {
        private static readonly Dictionary<string, (string Prefab, string Material)> table =
            new Dictionary<string, (string Prefab, string Material)>
            {
                ["IronStudy_IronArmorChest_mat"] = ("ArmorIronChest", "IronArmorChest_mat"),
            };

        public static Dictionary<string, Material> Find(List<GameObject> prefabs)
        {
            var found = new Dictionary<string, Material>();
            foreach (KeyValuePair<string, (string Prefab, string Material)> entry in table)
            {
                GameObject prefab = prefabs.Find(p => p != null && p.name == entry.Value.Prefab);
                Material material = prefab != null ? Named(prefab, entry.Value.Material) : null;
                if (material != null)
                    found[entry.Key] = material;
                else
                    Plugin.Log.LogWarning($"OpenKeep: no {entry.Value.Material} on {entry.Value.Prefab}; {entry.Key} wears the set's own material");
            }
            return found;
        }

        private static Material Named(GameObject prefab, string name)
        {
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.name == name)
                        return material;
                }
            }
            return null;
        }
    }
}
