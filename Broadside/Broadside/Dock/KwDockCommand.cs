using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// <c>kwship clear</c> removes every Broadside piece within 40 m, generated hull parts and ships built from the hammer
    /// included. A cheat like the game's spawn (devcommands first).
    /// </summary>
    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    public static class KwDockCommand
    {
        private const float Reach = 40f;
        private static bool registered;

        [HarmonyPostfix]
        private static void Postfix() => Guard.Run("Broadside kwship command", Register);

        private static void Register()
        {
            if (registered)
                return;
            registered = true;
            new Terminal.ConsoleCommand("kwship", "clear - Broadside: remove every Broadside piece within 40 m",
                (Terminal.ConsoleEvent)Run, isCheat: true, optionsFetcher: () => new List<string> { "clear" });
        }

        private static void Run(Terminal.ConsoleEventArgs args) => Guard.Run("kwship", Dispatch, args);

        private static void Dispatch(Terminal.ConsoleEventArgs args)
        {
            Player player = Player.m_localPlayer;
            string what = args.Length > 1 ? args[1].ToLowerInvariant() : "";
            if (player == null || ZNetScene.instance == null)
                return;
            if (what == "clear")
                args.Context?.AddString($"Removed {Clear(player.transform.position)} Broadside pieces.");
            else
                args.Context?.AddString("kwship clear");
        }

        private static int Clear(UnityEngine.Vector3 centre)
        {
            List<Piece> pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(centre, Reach, pieces);
            int count = 0;
            foreach (Piece piece in pieces)
            {
                ZNetView view = piece.GetComponent<ZNetView>();
                if (piece == null || view == null || !view.IsValid() || !Utils.GetPrefabName(piece.gameObject).StartsWith("KW_"))
                    continue;
                view.ClaimOwnership();
                piece.GetComponent<KwShieldRack>()?.DropAll();
                ZNetScene.instance.Destroy(piece.gameObject);
                count++;
            }
            return count;
        }
    }
}
