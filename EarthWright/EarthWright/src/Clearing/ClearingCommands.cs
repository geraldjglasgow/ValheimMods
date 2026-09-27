using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Costs;
using EarthWright.Terrain;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The console subcommands around the player: <c>ew reset [radius]</c> (free, like the reset keys),
    /// <c>ew forestry [radius]</c> (trees, logs, stumps) and <c>ew debris [radius]</c> (rocks, logs, loose stones,
    /// branches and flint; ore only where allowed). The clearing commands follow the server's clearing mode and the
    /// protection rules (<see cref="ClearRules"/>). A player needs clearing switched on and a terrain tool in hand, and
    /// pays what a click of the Clear entry costs (on that tool, within its cooldown); admins may always use them, free
    /// and without the survival tool check. Radii are capped by "Command Max Radius" (admins: 128 m), terrain radii also
    /// by the Engine's "Max Radius". Only a client with a player runs them; on a dedicated server they do nothing.
    /// </summary>
    public static class ClearingCommands
    {
        private const ClearCategory Forestry = ClearCategory.Trees | ClearCategory.Logs | ClearCategory.Stumps;
        private const ClearCategory Debris = ClearCategory.Rocks | ClearCategory.Logs | ClearCategory.Debris;
        private const float DefaultClearRadius = 10f;
        private const string NoPlayer = "join a world first.";

        public static void Register()
        {
            Command.Add("reset", "reset [radius]      resets the ground around you (height and paint) to the world's generated state", Reset);
            Command.Add("forestry", "forestry [radius]   clears trees, logs and stumps around you (hoe in hand)", args => Clear(args, Forestry));
            Command.Add("debris", "debris [radius]     clears rocks, logs and loose stones, branches and flint around you (hoe in hand)", args => Clear(args, Debris));
            Command.Add("pieces", "pieces [radius] [refund]  removes player-built pieces around you (refund drops their materials)", PieceRemover.Run, adminOnly: true);
        }

        private static void Reset(Terminal.ConsoleEventArgs args)
        {
            Player player = Player.m_localPlayer;
            string refusal = player == null ? NoPlayer : SwitchedOff();
            if (refusal != null)
            {
                args.Context.AddString("EarthWright: " + Language.Localize(refusal));
                return;
            }
            float typed = CommandInput.Number(args, 2, ClearingSettings.ResetAroundRadius.Value);
            float radius = ClearingSettings.TerrainRadius(typed, Side.LocalIsAdmin);
            bool sent = Dispatcher.Submit(ResetEdits.Around(player.transform.position, radius, ResetEdits.CommandSource));
            args.Context.AddString(sent
                ? "EarthWright: " + ClearingWords.Format(ClearingWords.ResetAround, ClearingWords.Metres(radius))
                : "EarthWright: the reset was refused (the reason is shown on screen).");
        }

        private static void Clear(Terminal.ConsoleEventArgs args, ClearCategory kinds)
        {
            Player player = Player.m_localPlayer;
            string refusal = Refusal(player);
            if (refusal != null)
            {
                args.Context.AddString("EarthWright: " + Language.Localize(refusal));
                return;
            }
            bool admin = Side.LocalIsAdmin;
            float radius = ClearingSettings.CommandRadius(CommandInput.Number(args, 2, DefaultClearRadius), admin);
            ClearArea area = ClearArea.Circle(player.transform.position, radius);
            ClearPlan plan = ClearPlanner.Prepare(player, area, ClearingSettings.AllowOres(kinds), admin, null);
            if (!plan.Empty && !admin && !Charge(player))
                return;
            int cleared = plan.Empty ? 0 : ClearJob.Execute(plan, player);
            ClearJob.Report(plan, cleared, line => args.Context.AddString("EarthWright: " + line), line => args.Context.AddString("EarthWright: " + line));
        }

        /// <summary>Why this player may not clear by command now, or null (the place rules come with the plan).</summary>
        private static string Refusal(Player player)
        {
            if (player == null)
                return NoPlayer;
            string off = SwitchedOff();
            if (off != null || Side.LocalIsAdmin)
                return off;
            if (!ClearingSettings.Enabled)
                return ClearingWords.Disabled;
            return LocalTool.IsToolName(LocalTool.RightItemName) ? null : ClearingWords.NeedTool;
        }

        /// <summary>The server's master switch is off; admins may still use the commands.</summary>
        private static string SwitchedOff()
        {
            return !GeneralSettings.Enabled.Value && !Side.LocalIsAdmin ? ClearingWords.ModOff : null;
        }

        /// <summary>Players pay what a click of the Clear entry costs, on the terrain tool in their hand.</summary>
        private static bool Charge(Player player)
        {
            ToolAction action = ActionCatalog.ById("ew_clear");
            return action == null || CostApi.TryCharge(player, action, new EditEstimate());
        }
    }
}
