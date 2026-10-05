namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// The Copy building entry while it is selected in the build tool in hand (and blueprints are on): with the build
    /// menu closed, every frame the keys, the piece under the crosshair, what a click would take (the hover), the glows
    /// and the HUD block; with the menu open only the selection's glow. While the entry is not selected the glows and
    /// the HUD go. The selection stays until it is saved or cleared, its pieces go, or the local player changes.
    /// </summary>
    public static class CopySession
    {
        private static Player lastPlayer;

        /// <summary>The tool is shown: its entry selected with the build menu closed.</summary>
        public static bool Active => IsActive(Player.m_localPlayer, menuClosed: true);

        /// <summary>The Copy entry is selected, with the build menu open or not (the selection glows).</summary>
        public static bool Selected => IsActive(Player.m_localPlayer, menuClosed: false);

        /// <summary>The Copy entry is selected in the build tool in hand (with its menu closed when asked), and blueprints are on.</summary>
        public static bool IsActive(Player player, bool menuClosed)
        {
            if (!BlueprintSettings.Enabled || player == null || player.IsDead() || !player.InPlaceMode())
                return false;
            return (!menuClosed || !Hud.IsPieceSelectionVisible()) && BlueprintMenu.IsCopy(player.GetSelectedPiece());
        }

        /// <summary>Per frame (SiteHooks).</summary>
        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            TrackPlayer(player);
            CopySave.Tick();
            CopySelection.DropLost();
            bool selected = IsActive(player, menuClosed: false);
            if (!selected || Hud.IsPieceSelectionVisible())
            {
                Hide(selectionGlows: selected);
                return;
            }
            CopyKeys.Handle(player);
            CopyAim.Update();
            CopyHover.Update();
            CopyGlow.Tick(shown: true);
            CopyHud.Show();
        }

        private static void Hide(bool selectionGlows)
        {
            CopyAim.Reset();
            CopyHover.Reset();
            CopyGlow.Tick(shown: selectionGlows);
            CopyHud.Clear();
        }

        /// <summary>A new local player (login, death, another world): no selection; the glow goes with it.</summary>
        private static void TrackPlayer(Player player)
        {
            if (ReferenceEquals(player, lastPlayer))
                return;
            lastPlayer = player;
            CopySelection.Clear();
            CopyBuildings.Clear();
        }
    }
}
