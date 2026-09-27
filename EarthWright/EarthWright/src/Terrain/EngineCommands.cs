using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// <c>ew limits</c>: the height limits where the player stands (biome, raise, dig, whether a dig exception lifts the
    /// dig limit here, the admin limit and the display clamp) and how far the ground there is from its original height.
    /// Read-only, for players and admins checking a server's limits.
    /// </summary>
    public static class EngineCommands
    {
        public static void Register()
        {
            Command.Add("limits", "limits             shows the height limits where you stand", Show);
        }

        private static void Show(Terminal.ConsoleEventArgs args)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                args.Context.AddString("EarthWright: no player in the world.");
                return;
            }
            Vector3 pos = player.transform.position;
            HeightLimits.At(pos.x, pos.z, out float raise, out float dig);
            string lifted = HeightLimits.DigLifted(pos) ? " (lifted here)" : "";
            args.Context.AddString($"EarthWright height limits at {pos.x:0}, {pos.z:0} ({LimitBiomes.At(pos)}): raise {raise:0.##} m, dig {dig:0.##} m{lifted}.");
            args.Context.AddString($"  Admin edits up to {HeightLimits.Admin:0.##} m; ground is drawn up to {HeightLimits.Absolute:0.##} m from its original height; per-biome rules: {(HeightLimits.HasBiomeRules ? "yes" : "none")}.");
            ShowGround(args, pos);
        }

        private static void ShowGround(Terminal.ConsoleEventArgs args, Vector3 pos)
        {
            float original = EngineBaseHeights.At(pos, float.NaN);
            float shown = TerrainRead.GroundHeight(pos, float.NaN);
            if (float.IsNaN(original) || float.IsNaN(shown))
                return;
            args.Context.AddString($"  Ground here: {shown:0.##} m, originally {original:0.##} m ({shown - original:+0.##;-0.##;0} m).");
        }
    }
}
