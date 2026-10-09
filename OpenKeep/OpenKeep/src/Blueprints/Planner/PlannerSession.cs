using System.Collections.Generic;
using OpenKeep.Blueprints.Sites;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The Site planner while its entry is selected in the hammer's Blueprints tab with the build menu closed (and blueprints
    /// on): every frame the keys, the ghost piece under the crosshair, the panel's site, the three glows (the hovered
    /// piece, or with Shift, G or both what a click takes; the selection; the queue row under the mouse) and the HUD block. While the entry
    /// is not active the glows and the HUD go; when it is no longer selected at all the panel closes. The selection
    /// stays until it is queued or cleared, its site goes, or the local player changes.
    /// </summary>
    public static class PlannerSession
    {
        private static Player lastPlayer;

        public static bool Active => IsActive(Player.m_localPlayer, menuClosed: true);

        /// <summary>The Site planner entry is selected in the build tool in hand (with its menu closed when asked), and blueprints are on.</summary>
        public static bool IsActive(Player player, bool menuClosed)
        {
            if (!BlueprintSettings.Enabled || player == null || player.IsDead() || !player.InPlaceMode())
                return false;
            return (!menuClosed || !Hud.IsPieceSelectionVisible()) && BlueprintMenu.IsPlanner(player.GetSelectedPiece());
        }

        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            TrackPlayer(player);
            PlannerSelection.DropLost();
            if (!IsActive(player, menuClosed: true))
            {
                Hide(release: !IsActive(player, menuClosed: false));
                return;
            }
            PlannerKeys.Handle(player);
            PlannerAim.Update();
            SiteMarker panelSite = PanelSite(player);
            PlannerPanel.SetSite(panelSite);
            ShowHover();
            ShowSelection();
            ShowPanelRow();
            PlannerHud.Show(PlannerAim.Any ? PlannerAim.Site : panelSite, PlannerPanel.Showing);
        }

        /// <summary>The site the panel shows: the selection's, else the nearest loaded one within <see cref="PlannerPanel.SiteRange"/>.</summary>
        private static SiteMarker PanelSite(Player player) =>
            PlannerSelection.Site != null ? PlannerSelection.Site : SiteMarker.Nearest(player.transform.position, PlannerPanel.SiteRange);

        private static void ShowHover()
        {
            if (!PlannerAim.Any)
            {
                PlannerGlow.Hide(PlannerGlow.HoverKey);
                return;
            }
            SiteMarker site = PlannerAim.Site;
            int piece = PlannerAim.Piece;
            bool sameType = PlannerKeys.SameType, shift = PlannerKeys.Shift;
            PlannerGlow.Show(PlannerGlow.HoverKey, site, (piece, shift, sameType), () => HoverPieces(site, piece, shift, sameType), PlannerGlow.Hover);
        }

        /// <summary>What a click would take: the piece, with Shift its house, with G its joined pieces of one type, with both every piece of that type in the house.</summary>
        private static List<int> HoverPieces(SiteMarker site, int piece, bool shift, bool sameType)
        {
            if (sameType)
                return shift ? PlannerGroup.InHouse(site, piece) : PlannerGroup.Of(site, piece);
            return shift ? PlannerHouse.Of(site, piece) : new List<int> { piece };
        }

        private static void ShowSelection()
        {
            if (PlannerSelection.Count == 0)
                PlannerGlow.Hide(PlannerGlow.SelectionKey);
            else
                PlannerGlow.Show(PlannerGlow.SelectionKey, PlannerSelection.Site, PlannerSelection.Version, PlannerSelection.Copy, PlannerGlow.Selection);
        }

        private static void ShowPanelRow()
        {
            int row = PlannerPanel.HoveredRow;
            if (row < 0 || row >= PanelModel.Rows.Count || PanelModel.Site == null)
            {
                PlannerGlow.Hide(PlannerGlow.PanelKey);
                return;
            }
            List<int> pieces = PanelModel.Rows[row].Unbuilt;
            PlannerGlow.Show(PlannerGlow.PanelKey, PanelModel.Site, (row, PanelModel.Stamp), () => pieces, PlannerGlow.Panel);
        }

        private static void Hide(bool release)
        {
            PlannerGlow.HideAll();
            PlannerAim.Reset();
            PlannerHud.Clear();
            if (release)
                PlannerPanel.Close();
        }

        /// <summary>A new local player (login, death, another world): no selection, the panel shut, no glow left behind.</summary>
        private static void TrackPlayer(Player player)
        {
            if (ReferenceEquals(player, lastPlayer))
                return;
            lastPlayer = player;
            PlannerGlow.HideAll();
            PlannerSelection.Clear();
            PlannerPanel.Close();
        }
    }
}
