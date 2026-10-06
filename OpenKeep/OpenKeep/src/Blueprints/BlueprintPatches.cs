using System;
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

    /// <summary>
    /// Runs the blueprint feature every frame, on the plugin's own object, and switches its IMGUI drawing
    /// (<see cref="BlueprintGui"/>) on only while there is something to draw. Nothing runs on a dedicated server (no
    /// player, no screen). While Blueprints is off the work idles once things have settled: it runs on for
    /// <see cref="Settle"/> seconds after the switch was last on and after a new local player or build menu came (the
    /// tab, the entries and the tools put away), and whenever a construction site is loaded or a build still runs.
    /// </summary>
    public sealed class BlueprintRunner : MonoBehaviour
    {
        private const float Settle = 3f;

        private static readonly (string Name, Action Run)[] Ticks =
        {
            ("OpenKeep blueprint menu", BlueprintMenu.Refresh),
            ("OpenKeep blueprints tab", BlueprintTab.Tick),
            ("OpenKeep blueprint picks", Tab.TabPicks.Tick),
            ("OpenKeep blueprint box", Tab.TabBox.Tick),
            ("OpenKeep blueprint drag", Tab.TabDrag.Tick),
            ("OpenKeep blueprints tab memory", Tab.TabMemory.Tick),
            ("OpenKeep blueprint rename", BlueprintRename.Tick),
            ("OpenKeep blueprint session", BlueprintSession.Tick),
            ("OpenKeep fix ground session", GroundFixSession.Tick),
            ("OpenKeep blueprint zoom", HammerZoom.Tick),
            ("OpenKeep blueprint build", BuildJob.Tick),
        };

        private BlueprintGui gui;
        private float busyUntil;
        private Player seenPlayer;
        private BuildUi seenMenu;

        private void Awake()
        {
            gui = gameObject.AddComponent<BlueprintGui>();
            gui.enabled = false;
        }

        private void Update()
        {
            if (ZNet.instance != null && ZNet.instance.IsDedicated())
            {
                enabled = false;
                return;
            }
            bool busy = Busy();
            if (busy)
            {
                foreach ((string name, Action run) in Ticks)
                    BlueprintSafe.Run(name, run);
                Sites.SiteHooks.Update();
            }
            gui.Want(busy && (BlueprintHud.Wanted || Sites.SiteHooks.GuiWanted));
        }

        private bool Busy()
        {
            float now = Time.unscaledTime;
            if (BlueprintSettings.Enabled || Player.m_localPlayer != seenPlayer || BlueprintTab.Menu != seenMenu)
            {
                busyUntil = now + Settle;
                seenPlayer = Player.m_localPlayer;
                seenMenu = BlueprintTab.Menu;
            }
            return now < busyUntil || Sites.SiteMarker.Loaded.Count > 0 || BuildJob.Busy || Sites.SiteDeliveries.Waiting;
        }
    }
}
