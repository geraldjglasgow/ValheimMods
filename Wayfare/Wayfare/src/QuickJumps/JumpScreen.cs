using UnityEngine;
using Wayfare.Core;
using Wayfare.SeaGates;

namespace Wayfare.QuickJumps
{
    /// <summary>The game's teleport screen only when there is something to load. A long jump fades the screen to black
    /// and shows the teleport swirl (<c>Hud.UpdateBlackScreen</c>, while <c>Player.IsTeleporting</c>) for as long as the
    /// jump lasts. When the jump starts, <see cref="Started"/> checks whether the target area is already loaded on this
    /// client (<c>ZNetScene.IsAreaReady</c>: the zone and every object the client knows there exist); a jump inside the
    /// loaded area then keeps the screen clear (<see cref="JumpScreenPatch"/>), a jump into an area still to load gets the
    /// game's screen, black within <see cref="FastFadeSeconds"/> instead of the game's second, because Quick Portals
    /// moves the player a quarter of a second after stepping in. Decided once per jump, so the screen never flickers
    /// during it. A sea gate jump keeps the game's screen: its crew hold fades and lands the crew itself.</summary>
    public static class JumpScreen
    {
        /// <summary>The game's fade time when no player state gives another (<c>Hud.GetFadeDuration</c>).</summary>
        private const float FadeSeconds = 1f;

        /// <summary>The fade to black of a quick jump that loads: done before the move (<see cref="JumpTiming"/>, at most 0.25 s).</summary>
        private const float FastFadeSeconds = 0.2f;

        private static bool clearJump;

        public static void Started(Vector3 target)
        {
            clearJump = WayfareConfig.Enabled.Value && WayfareConfig.ScreenOnlyWhenLoading.Value
                && ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(target);
            if (clearJump)
                Plugin.Log.LogInfo($"Wayfare: jump target ({target.x:F0}, {target.z:F0}) is loaded already, no teleport screen");
        }

        /// <summary>True while the local player's current jump needs no screen.</summary>
        public static bool Clear(Player player)
        {
            return clearJump && Local(player) && player.IsTeleporting() && !CrewHold.Holding;
        }

        /// <summary>True while the local player's current quick jump loads behind the teleport screen.</summary>
        public static bool Loading(Player player)
        {
            return !clearJump && JumpTiming.Applies && Local(player) && player.ShowTeleportAnimation();
        }

        private static bool Local(Player player)
        {
            return player != null && player == Player.m_localPlayer && !player.IsDead() && !player.IsSleeping();
        }

        /// <summary>Brings the loading screen in faster than the game would; the game's own update then runs as usual.</summary>
        public static void FadeInFast(Hud hud, float dt)
        {
            hud.m_loadingScreen.gameObject.SetActive(true);
            hud.m_loadingScreen.alpha = Mathf.MoveTowards(hud.m_loadingScreen.alpha, 1f, dt / FastFadeSeconds);
        }

        /// <summary>The game's own fade-out, as it runs when nothing holds the screen.</summary>
        public static void FadeOut(Hud hud, float dt)
        {
            hud.m_haveSetupLoadScreen = false;
            hud.m_loadingScreen.alpha = Mathf.MoveTowards(hud.m_loadingScreen.alpha, 0f, dt / FadeSeconds);
            if (hud.m_loadingScreen.alpha <= 0f)
                hud.m_loadingScreen.gameObject.SetActive(false);
        }
    }
}
