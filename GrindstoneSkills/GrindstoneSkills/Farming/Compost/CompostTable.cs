using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Puts the compost bin (<see cref="CompostPrefab"/>) at the end of the cultivator's menu: the build pieces of the
    /// "Cultivator" item (its m_buildPieces table, shared by every cultivator). Runs after ZNetScene.Awake and after
    /// ObjectDB.Awake, so whichever comes second finds both the bin and the item; adding is idempotent. Players see it once
    /// they know its materials, as for every piece.
    /// </summary>
    public static class CompostTable
    {
        private const string Cultivator = "Cultivator";

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabaseAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => HookGuard.Run("Compost bin menu", AddToCultivator);
        }

        public static void AddToCultivator()
        {
            GameObject bin = CompostPrefab.Prefab;
            GameObject tool = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(Cultivator) : null;
            PieceTable table = tool != null ? tool.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces : null;
            if (bin != null && table != null && !table.m_pieces.Contains(bin))
                table.m_pieces.Add(bin);
        }
    }
}
