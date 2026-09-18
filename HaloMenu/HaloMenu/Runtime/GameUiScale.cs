using UnityEngine;

namespace HaloMenu.Runtime
{
    /// <summary>
    /// The multiplier that already reflects both screen resolution and Valheim's own UI Scale setting: the scale
    /// factor of the Hud's own root Canvas. Reading it, rather than re-deriving Valheim's formula, is what "respect
    /// Valheim's own UI scale setting" means here - whatever the game computes for its HUD, HaloMenu inherits.
    /// Falls back to a plain Screen.height / 1080 ratio when Hud is unavailable (no local player, main menu).
    /// </summary>
    public static class GameUiScale
    {
        public static float Factor()
        {
            Canvas hudCanvas = Hud.instance != null && Hud.instance.m_rootObject != null
                ? Hud.instance.m_rootObject.GetComponentInParent<Canvas>()
                : null;
            return hudCanvas != null ? hudCanvas.scaleFactor : Mathf.Max(Screen.height / 1080f, 0.01f);
        }
    }
}
