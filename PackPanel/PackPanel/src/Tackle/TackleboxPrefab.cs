using System;
using BundlePrefabs;
using HarmonyLib;
using PackPanel.Crafting;
using UnityEngine;

namespace PackPanel.Tackle
{
    /// <summary>
    /// Builds every tacklebox's item when ZNetScene wakes, the same on the server and every client, and registers it with
    /// ZNetScene and ObjectDB (the BundlePrefabs library), so a dropped box and one in a chest mean the same prefab
    /// everywhere. The models come from the embedded bundle <c>packpanel_tackleboxes</c> (AssetWorkshop
    /// <c>packpanel_*_tacklebox</c>), dressed as every crafted item's model is (<see cref="CraftedModels"/>). A box is never
    /// worn on the body, so the item is all there is. Built once per process; a box whose model is missing or fails is
    /// logged and left out, the others still come.
    /// </summary>
    public static class TackleboxPrefab
    {
        private const string Bundle = "packpanel_tackleboxes";

        public static void Install(Harmony harmony) => NetPrefabs.OnSceneAwake(harmony, scene => Setup(scene, harmony));

        private static void Setup(ZNetScene scene, Harmony harmony)
        {
            foreach (TackleboxKind kind in TackleboxCatalog.All)
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

        private static void Build(ZNetScene scene, TackleboxKind kind)
        {
            GameObject model = CraftedModels.Model(Bundle, kind);
            Material look = CraftedModels.Look(scene, model);
            kind.Item = CraftedItem.Build(scene.GetPrefab(CraftedItem.BaseItem), CraftedModels.Dressed(model, "model", look), kind);
            Plugin.Log.LogInfo($"{kind.Name} ready: {kind.Id}");
        }
    }
}
