using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Torch Switch Key, polled after the local player's update: only when the game itself takes input
    /// (<c>Player.TakeInput</c>: no inventory, chat, console, map, menu or text input) and no radial menu is open, as for
    /// the game's Use. The target is the game's own hover object (<c>Player.GetHoverObject</c>, what Use would act on).
    /// The key is not the game's Use and is read separately, so it never adds fuel or toggles a fire itself.
    /// </summary>
    public static class TorchKeyPatch
    {
        /// <summary>After the local player's update (<see cref="PlayerUpdatePatch"/>).</summary>
        public static void Tick(Player __instance)
        {
            if (__instance != Player.m_localPlayer || !Keys.Pressed(TorchSettings.SwitchKey))
                return;
            if (!__instance.TakeInput() || Hud.InRadial())
                return;
            GameObject hovered = __instance.GetHoverObject();
            Fireplace fire = hovered != null ? hovered.GetComponentInParent<Fireplace>() : null;
            if (fire != null)
                TorchKeep.Request(fire);
        }
    }
}
