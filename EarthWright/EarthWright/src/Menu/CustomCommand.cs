using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Costs;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// The special action "custom": a click with a custom entry runs its console command on this machine, through the
    /// game's console as if the player had typed it, so the command's own rules apply (admin-only EarthWright
    /// subcommands, the game's cheat gate). Admin-only entries are refused for other players. The swing's base costs are
    /// charged through the Costs module first. One press runs the command once; a repeating entry keeps running it
    /// while the button is held (<see cref="CustomRepeat"/>).
    /// </summary>
    public sealed class CustomCommand : ISpecialAction
    {
        public void OnClick(Player player, ToolAction action, Vector3 ghostPosition)
        {
            CustomEntry entry = CustomEntries.ForAction(action);
            if (entry == null || CustomRepeat.HeldSinceRun)
                return;
            if (Run(player, action, entry, ghostPosition))
                CustomRepeat.Started(entry, action);
        }

        /// <summary>Checks, charges and runs the entry's command once. False when refused or not run.</summary>
        public static bool Run(Player player, ToolAction action, CustomEntry entry, Vector3 ghostPosition)
        {
            if (entry.Admin && !Side.LocalIsAdmin)
            {
                Messages.Center(MenuWords.CustomAdminOnly);
                return false;
            }
            if (!EntryLevels.Unlocked(entry.PrefabName))
            {
                Messages.Center(MenuWords.LevelLocked);
                return false;
            }
            if (!CostApi.TryCharge(player, action, new EditEstimate()))
                return false;
            return Execute(CommandTemplate.Fill(entry.Command, player, ghostPosition));
        }

        private static bool Execute(string text)
        {
            Terminal terminal = global::Console.instance != null ? (Terminal)global::Console.instance : Chat.instance;
            if (terminal == null)
            {
                Messages.Center(MenuWords.CustomNoConsole);
                return false;
            }
            if (GeneralSettings.DebugLog.Value)
                Plugin.Log.LogInfo("EarthWright custom entry runs: " + text);
            terminal.TryRunCommand(text);
            return true;
        }
    }
}
