using System;
using BundlePrefabs;
using HarmonyLib;
using PackPanel.Crafting;
using UnityEngine;

namespace PackPanel.Backpacks
{
    /// <summary>
    /// Builds every backpack's prefabs when ZNetScene wakes, the same on the server and every client, and registers the
    /// items with ZNetScene and ObjectDB (the BundlePrefabs library), so a dropped pack and one in a chest mean the same
    /// prefab everywhere. The models come from the embedded bundle <c>packpanel_backpacks</c>, dressed as every crafted
    /// item's model is (<see cref="CraftedModels"/>): the item (<see cref="CraftedItem"/>) and the copy worn on the back.
    /// Built once per process; a pack whose model is missing or fails is logged and left out, the others still come.
    /// </summary>
    public static class BackpackPrefab
    {
        private const string Bundle = "packpanel_backpacks";

        public static void Install(Harmony harmony) => NetPrefabs.OnSceneAwake(harmony, scene => Setup(scene, harmony));

        private static void Setup(ZNetScene scene, Harmony harmony)
        {
            foreach (BackpackKind kind in BackpackCatalog.All)
            {
                try
                {
                    if (kind.Item == null)
                        Build(scene, kind);
                    NetPrefabs.Register(scene, kind.Item);
                    ItemPrefabs.Register(harmony, kind.Item);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"the {kind.Name} could not be built and is not in the game: {e.Message}");
                }
            }
            CraftRecipes.Install(ObjectDB.instance);
        }

        private static void Build(ZNetScene scene, BackpackKind kind)
        {
            GameObject model = CraftedModels.Model(Bundle, kind);
            Material look = CraftedModels.Look(scene, model);
            GameObject worn = CraftedModels.Dressed(model, "PackPanel_worn_" + kind.Word, look);
            foreach (Collider collider in worn.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            kind.Item = CraftedItem.Build(scene.GetPrefab(CraftedItem.BaseItem), CraftedModels.Dressed(model, "model", look), kind);
            kind.Worn = worn;
            Plugin.Log.LogInfo($"{kind.Name} ready: {kind.Id}");
        }
    }
}
