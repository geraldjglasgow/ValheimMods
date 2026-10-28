using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// Plugin entry. Broadside designs ships on a dry dock (the dock pieces, the hull tools, the fittings and the
    /// blueprint table) and builds the saved ones on the water from the hammer, ships that float, sail and are steered at
    /// their rudder (CLAUDE.md has the map). It has no settings. Every patch class is applied on its own, the pieces and
    /// the ship are built whenever the game's network scene wakes (<see cref="KwPrefabs"/>), and Guard.Install goes last.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Broadside : BaseUnityPlugin
    {
        public const string PluginGuid = "milkyteam.broadside";
        public const string PluginName = "Broadside";
        public const string PluginVersion = "0.2.0";

        private void Awake()
        {
            Log.Bind(Logger);
            Harmony harmony = new Harmony(PluginGuid);
            int failed = PatchEverything(harmony);
            KwPrefabs.Install(harmony);
            KwCannonPrefabs.Install(harmony);
            KwRepairKit.Install(harmony);
            KwBpCamera.Install();
            KwHelmKeys.Install();
            KwReadyShips.Install();
            Log.Info($"Loading [{PluginName} {PluginVersion}]" + (failed > 0 ? $" with {failed} failed patches, see above" : ""));

            // Exceptions thrown by this mod's patches are logged under the Broadside log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
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
