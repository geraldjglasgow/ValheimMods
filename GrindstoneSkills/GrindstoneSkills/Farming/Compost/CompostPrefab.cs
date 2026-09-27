using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The compost bin piece: a copy of the game's barrel container (piece_chest_barrel, same model, container, health and
    /// workbench need) named <see cref="Keys.CompostPrefab"/>, with its own name, cost (10 wood, 4 stone), the Misc
    /// category the cultivator's menu lists, and a <see cref="CompostBin"/>. It is made once per process under an inactive
    /// holder (so the copy never wakes up as a world object), in ZNetScene.Awake before the scene registers its prefabs,
    /// and added to every new scene's list: every machine builds it from code alone, so its name and hash agree
    /// everywhere and bins exist for every peer. <see cref="CompostTable"/> puts it in the cultivator's menu.
    /// </summary>
    public static class CompostPrefab
    {
        private const string Barrel = "piece_chest_barrel";
        private const string Workbench = "piece_workbench";
        private const string Name = "Compost bin";
        private const string Description = "Turns food scraps and spare crops into compost that feeds the crops around it: they grow faster and ripen with better stars.";

        private static GameObject holder;

        public static GameObject Prefab { get; private set; }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class SceneAwake
        {
            [HarmonyPrefix]
            private static void Prefix(ZNetScene __instance) => HookGuard.Run("Compost bin prefab", () => Register(__instance.m_prefabs));

            [HarmonyPostfix]
            private static void Postfix() => HookGuard.Run("Compost bin menu", CompostTable.AddToCultivator);
        }

        private static void Register(List<GameObject> prefabs)
        {
            if (Prefab == null)
                Build(prefabs);
            if (Prefab != null && !prefabs.Contains(Prefab))
                prefabs.Add(Prefab);
        }

        private static void Build(List<GameObject> prefabs)
        {
            GameObject barrel = Find(prefabs, Barrel);
            if (barrel == null || barrel.GetComponent<Piece>() == null || barrel.GetComponentInChildren<Container>(true) == null)
            {
                GrindstoneSkills.Log.LogWarning("Farming: the game's barrel (" + Barrel + ") was not found, so there is no compost bin.");
                return;
            }
            holder = new GameObject("GrindstoneSkills_Compost");
            holder.SetActive(false);
            Object.DontDestroyOnLoad(holder);
            GameObject clone = Object.Instantiate(barrel, holder.transform, false);
            clone.name = Keys.CompostPrefab;
            Dress(clone, prefabs);
            clone.AddComponent<CompostBin>();
            Prefab = clone;
        }

        private static void Dress(GameObject clone, List<GameObject> prefabs)
        {
            Piece piece = clone.GetComponent<Piece>();
            piece.m_name = Name;
            piece.m_description = Description;
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_enabled = true;
            if (piece.m_craftingStation == null)
                piece.m_craftingStation = Find(prefabs, Workbench)?.GetComponent<CraftingStation>();
            piece.m_resources = new[] { Requirement(prefabs, "Wood", 10), Requirement(prefabs, "Stone", 4) }
                .Where(requirement => requirement.m_resItem != null).ToArray();
            Container container = clone.GetComponentInChildren<Container>(true);
            if (container != null)
                container.m_name = Name;
        }

        private static GameObject Find(List<GameObject> prefabs, string name) => prefabs.Find(prefab => prefab != null && prefab.name == name);

        private static Piece.Requirement Requirement(List<GameObject> prefabs, string item, int amount)
        {
            GameObject prefab = Find(prefabs, item);
            return new Piece.Requirement { m_resItem = prefab != null ? prefab.GetComponent<ItemDrop>() : null, m_amount = amount, m_recover = true };
        }
    }
}
