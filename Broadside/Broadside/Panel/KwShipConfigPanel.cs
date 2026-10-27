using BepInEx.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace Broadside
{
    /// <summary>
    /// ShipConfig's own ship panel kept off screen while the local player is aboard a Broadside ship, so only Broadside's
    /// shows there (ShipConfig is a separate mod a player may also run; nothing of it is referenced). Its panel is found by
    /// the name ShipConfig publishes for it, <see cref="Name"/>, a direct child of the HUD root
    /// (<c>Hud.instance.m_rootObject</c>), made the first time its player is aboard a ship. ShipConfig shows and hides that
    /// object itself every frame, so it is left active: its Canvas and GraphicRaycaster are switched off instead (drawn and
    /// hit by nothing, ShipConfig's own hover finding none of its lines), and back on when the player leaves the ship. Looked
    /// for only when ShipConfig is installed, and again only when the HUD root's children changed.
    /// </summary>
    internal sealed class KwShipConfigPanel
    {
        public const string Name = "ShipConfig_ShipPanel";
        private const string Guid = "com.ShipConfig";

        private static bool? installed;

        private Transform? hudRoot;
        private int searchedChildren = -1;
        private Canvas? canvas;
        private GraphicRaycaster? raycaster;
        private bool hidden;

        /// <summary>Hides ShipConfig's panel under <paramref name="root"/> (the HUD root), finding it first if it is new.</summary>
        public void Hide(Transform root)
        {
            if (!Installed())
                return;
            if (canvas == null)
                Find(root);
            if (canvas == null)
                return;
            if (canvas.enabled)
                canvas.enabled = false;
            if (raycaster != null && raycaster.enabled)
                raycaster.enabled = false;
            hidden = true;
        }

        /// <summary>ShipConfig's panel shown again as ShipConfig leaves it, if it was hidden here.</summary>
        public void Restore()
        {
            if (!hidden)
                return;
            hidden = false;
            if (canvas != null)
                canvas.enabled = true;
            if (raycaster != null)
                raycaster.enabled = true;
        }

        // Looked for when the HUD root is new or its children changed since the last look (ShipConfig adds its panel there).
        private void Find(Transform root)
        {
            if (root == hudRoot && root.childCount == searchedChildren)
                return;
            (hudRoot, searchedChildren) = (root, root.childCount);
            Transform? panel = root.Find(Name);
            canvas = panel != null ? panel.GetComponent<Canvas>() : null;
            raycaster = panel != null ? panel.GetComponent<GraphicRaycaster>() : null;
        }

        private static bool Installed() => installed ??= Chainloader.PluginInfos.ContainsKey(Guid);
    }
}
