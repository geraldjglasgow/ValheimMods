using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// TerrainComp.Awake postfix: every terrain compiler answers OpenKeep's ground RPC, whatever the switch says here,
    /// since the switch that counts is the one of the player who sends the work.
    /// </summary>
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.Awake))]
    public static class BlueprintGroundRpcPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TerrainComp __instance)
        {
            BlueprintSafe.Run("OpenKeep blueprint ground RPC", () => GroundWriter.Register(__instance));
        }
    }

    /// <summary>Player.TryPlacePiece prefix: a click with an entry of the hammer's Blueprints tab selected is OpenKeep's, not the game's.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class BlueprintPlacePatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            return BlueprintTool.BeforePlace(__instance, piece, ref __result);
        }
    }

    /// <summary>Player.AddKnownPiece prefix (private): the Blueprints tab's entries become known without the "new piece" message.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownPiece))]
    public static class BlueprintKnownPiecePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance, Piece piece)
        {
            return BlueprintSafe.Call("OpenKeep blueprint entry", () => BlueprintMenu.LearnQuietly(__instance, piece), true);
        }
    }

    /// <summary>
    /// Player.RemovePiece prefix (private): while an entry of the Blueprints tab is selected, the hammer's remove button
    /// (the middle button by default) takes nothing down for the local player, so aiming a tool at a building never removes a piece of it.
    /// Every other prefix still runs (HarmonyX runs them all), so the build camera's reach swap is put back as usual.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.RemovePiece))]
    public static class BlueprintNoRemovePatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Player __instance, ref bool __result)
        {
            if (__instance != Player.m_localPlayer || !BlueprintMenu.IsOurs(__instance.GetSelectedPiece()))
                return true;
            __result = false;
            return false;
        }
    }

    /// <summary>Runs the blueprint feature every frame and draws its HUD; on the plugin's own object.</summary>
    public sealed class BlueprintRunner : MonoBehaviour
    {
        private void Update()
        {
            BlueprintSafe.Run("OpenKeep blueprint menu", BlueprintMenu.Refresh);
            BlueprintSafe.Run("OpenKeep blueprints tab", BlueprintTab.Tick);
            BlueprintSafe.Run("OpenKeep blueprint picks", Tab.TabPicks.Tick);
            BlueprintSafe.Run("OpenKeep blueprint box", Tab.TabBox.Tick);
            BlueprintSafe.Run("OpenKeep blueprint drag", Tab.TabDrag.Tick);
            BlueprintSafe.Run("OpenKeep blueprints tab memory", Tab.TabMemory.Tick);
            BlueprintSafe.Run("OpenKeep blueprint rename", BlueprintRename.Tick);
            BlueprintSafe.Run("OpenKeep blueprint session", BlueprintSession.Tick);
            BlueprintSafe.Run("OpenKeep fix ground session", GroundFixSession.Tick);
            BlueprintSafe.Run("OpenKeep blueprint zoom", HammerZoom.Tick);
            BlueprintSafe.Run("OpenKeep blueprint camera", BlueprintCamera.Tick);
            BlueprintSafe.Run("OpenKeep blueprint build", BuildJob.Tick);
            Sites.SiteHooks.Update();
        }

        private void OnGUI()
        {
            BlueprintSafe.Run("OpenKeep blueprint HUD", BlueprintHud.Draw);
            Sites.SiteHooks.Gui();
        }
    }
}
