using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using PatchGuard;
using SyncedConfig;

namespace Broadside
{
    /// <summary>
    /// Plugin entry. Broadside designs ships on a dry dock (the dock pieces, the hull tools, the fittings and the
    /// blueprint table) and builds the saved ones on the water from the hammer, ships that float, sail and are steered at
    /// their rudder (CLAUDE.md has the map). Its one setting is in <see cref="KwSettings"/>. Every patch class is applied on its
    /// own, the pieces and the ship are built whenever the game's network scene wakes (<see cref="KwPrefabs"/>), and
    /// Guard.Install goes last.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Broadside : BaseUnityPlugin
    {
        public const string PluginGuid = "milkyteam.broadside";
        public const string PluginName = "Broadside";
        public const string PluginVersion = "0.7.1";

        private void Awake()
        {
            Log.Bind(Logger);
            SyncedConfiguration synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            KwSettings.Bind(synced);
            Harmony harmony = new Harmony(PluginGuid);
            int failed = PatchEverything(harmony);
            InstallPieces(harmony);
            KwBpCamera.Install();
            KwHelmKeys.Install();
            KwReadyShips.Install();
            Log.Info($"Loading [{PluginName} {PluginVersion}]" + (failed > 0 ? $" with {failed} failed patches, see above" : ""));

            // Writes the .cfg, hot reloads it on edit; Charter binds every player to the server's values.
            synced.Finish(harmony);

            // Exceptions thrown by this mod's patches are logged under the Broadside log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// The pieces, items and the ship, made whenever the network scene wakes, in this order: Broadside's own pieces first
        /// (the plank wood and the rope the others wear), then the cannons, crates, piles, the hammock, the net winch, the dockside
        /// decorations, the Auto-decorate tool, the Dock tool and its docks, the cargo crane and the repair kit.
        /// </summary>
        private static void InstallPieces(Harmony harmony)
        {
            KwPrefabs.Install(harmony);
            KwCannonPrefabs.Install(harmony);
            KwCrates.Install(harmony);
            KwPiles.Install(harmony);
            KwHammocks.Install(harmony);
            KwTrawl.Install(harmony);
            KwDockside.Install(harmony);
            KwDecorTool.Install(harmony);
            KwPierPrefabs.Install(harmony);
            KwCargoPrefabs.Install(harmony);
            KwRepairKit.Install(harmony);
        }

        /// <summary>Applies every patch class on its own so one failure is logged and the others still apply.</summary>
        private static int PatchEverything(Harmony harmony)
        {
            int failed = 0;
            foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0)
                    continue;
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    failed++;
                    Log.Error($"Patch {type.FullName} failed to apply: {e.Message}");
                }
            }
            return failed;
        }
    }
}
