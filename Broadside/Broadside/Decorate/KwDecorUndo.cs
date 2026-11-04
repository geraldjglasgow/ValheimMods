using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The last auto-decoration taken back (DECORATE.md, "Undo"): Ctrl + Z with the tool in hand removes each piece of the
    /// last batch still standing the hammer's way (<c>WearNTear.Remove</c>: its owner removes it and drops its recipe, the
    /// usual refund), each checked as the hammer checks: this player made it, it may be removed, no no-build place, a ward
    /// the player may build in, the workbench in range, not in use. Only the last batch, once, this session; a piece
    /// already removed or out of the loaded world is passed over.
    /// </summary>
    public static class KwDecorUndo
    {
        private static readonly List<ZDOID> last = new List<ZDOID>(), building = new List<ZDOID>();

        public static bool Any => last.Count > 0;

        public static void Begin() => building.Clear();

        public static void Placed(Piece? piece)
        {
            ZDO? zdo = piece != null && piece.m_nview != null ? piece.m_nview.GetZDO() : null;
            if (zdo != null)
                building.Add(zdo.m_uid);
        }

        public static void End()
        {
            if (building.Count == 0)
                return;
            last.Clear();
            last.AddRange(building);
            building.Clear();
        }

        public static void Undo(Player player)
        {
            if (last.Count == 0)
            {
                player.Message(MessageHud.MessageType.Center, "No auto-decoration to take back");
                return;
            }
            int taken = 0, kept = 0;
            foreach (ZDOID id in last)
            {
                GameObject? found = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(id) : null;
                Piece? piece = found != null ? found.GetComponent<Piece>() : null;
                if (piece == null)
                    continue;
                if (Take(player, piece))
                    taken++;
                else
                    kept++;
            }
            last.Clear();
            string held = kept > 0 ? $"; {kept} kept (a ward, no workbench near, or in use)" : "";
            player.Message(MessageHud.MessageType.Center, $"{taken} decorations taken back, their materials dropped{held}");
        }

        private static bool Take(Player player, Piece piece)
        {
            WearNTear wear = piece.GetComponent<WearNTear>();
            if (wear == null || !Removable(player, piece))
                return false;
            piece.GetComponent<IRemoved>()?.OnRemoved();
            wear.Remove();
            return true;
        }

        private static bool Removable(Player player, Piece piece)
        {
            Vector3 at = piece.transform.position;
            CraftingStation station = piece.m_craftingStation;
            bool near = player.NoCostCheat() || station == null || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench)
                || CraftingStation.HaveBuildStationInRange(station.m_name, player.transform.position);
            return piece.GetCreator() == player.GetPlayerID() && piece.m_canBeRemoved && near && piece.CanBeRemoved()
                && !Location.IsInsideNoBuildLocation(at) && PrivateArea.CheckAccess(at, 0f, false, false);
        }
    }
}
