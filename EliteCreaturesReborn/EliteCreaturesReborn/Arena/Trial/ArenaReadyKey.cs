using Hotkeys;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// F for "ready" (<see cref="ArenaReady"/>), on the arena core's object on clients, so it is there exactly while the
    /// arena is loaded here; a machine with no screen switches it off. F is fixed (no setting: it switches no feature)
    /// and read through ValheimModLibs' Hotkeys, so it does nothing while the player types in chat or the console or a
    /// text box, nor with Shift, Ctrl or Alt held; and only when the game itself takes the player's input (no menu, no
    /// inventory, no map open, alive). During the countdown or a break, for a player alive in the arena, it sends the vote
    /// to the core's owner, which turns it over (a second press takes it back).
    /// </summary>
    internal sealed class ArenaReadyKey : MonoBehaviour
    {
        private const KeyCode ReadyKey = KeyCode.F;

        private ZNetView _nview = null!;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            enabled = _nview != null && !ArenaPortalLook.NoScreen;
        }

        private void Update()
        {
            if (Hotkey.Pressed(ReadyKey))
            {
                Guard.Run("Hel's Trial ready", Vote);
            }
        }

        private void Vote()
        {
            Player player = Player.m_localPlayer;
            ZDO? zdo = _nview.IsValid() ? _nview.GetZDO() : null;
            if (player == null || zdo == null || !player.TakeInput() || !ArenaReady.Resting(zdo) || !HallPlayers.LocalInHall())
            {
                return;
            }
            _nview.InvokeRPC(ArenaKeys.ReadyRpc); // to the core's owner, this machine included
        }
    }
}
