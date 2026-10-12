using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Deerhide Robe has a worn mesh per body (<c>robe_male</c>, <c>robe_female</c>: the male and female bodies are
    /// different meshes): after the game attaches the robe's worn parts (<c>VisEquipment.AttachArmor</c>, on every machine
    /// that draws the wearer, from the equipment the game already replicates) the one the wearer's body does not use is
    /// switched off. The body is the game's model index (0 male, 1 female); an armour stand shows the male mesh.
    /// </summary>
    [HarmonyPatch(typeof(VisEquipment), "AttachArmor")]
    public static class RobeBody
    {
        private const string Male = "robe_male";
        private const string Female = "robe_female";

        [HarmonyPostfix]
        private static void Postfix(VisEquipment __instance, int itemHash, List<GameObject> __result)
        {
            if (__result == null || !IsRobe(itemHash))
                return;
            bool female = !__instance.m_isArmorStand && __instance.m_modelIndex == 1;
            foreach (GameObject worn in __result)
            {
                if (worn == null)
                    continue;
                Show(worn.transform, Male, !female);
                Show(worn.transform, Female, female);
            }
        }

        private static bool IsRobe(int itemHash)
        {
            foreach (RobeSet set in RobeSets.All)
            {
                GameObject robe = set.Robe.Item;
                if (robe != null && itemHash == robe.name.GetStableHashCode())
                    return true;
            }
            return false;
        }

        private static void Show(Transform worn, string mesh, bool on)
        {
            Transform part = BundlePrefabs.GameMaterials.Find(worn, mesh);
            if (part != null && part.gameObject.activeSelf != on)
                part.gameObject.SetActive(on);
        }
    }
}
