using OpenKeep.Core;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The Construction ghosts switch of the Blueprints tab (the user, 2026-10-05: "some option in the hammer to turn off
    /// unbuilt building outlines"): this player's own choice whether the see-through ghosts of construction sites are
    /// drawn on this machine, never synced, kept with the tab's folder in BepInEx/config/OpenKeep.BlueprintsTab.txt.
    /// Hidden, every site ghost here is hidden and cannot be aimed at, except while the Site planner is in hand (it works
    /// on them). The site posts, their hover text and the building are not touched. A click on the entry in the menu flips
    /// it, and the menu stays open with the entry's icon and words showing the new state.
    /// </summary>
    public static class GhostSwitch
    {
        /// <summary>This player wants site ghosts drawn.</summary>
        public static bool Shown
        {
            get
            {
                Tab.TabMemory.Load();
                return Tab.TabMemory.GhostsShown;
            }
        }

        /// <summary>Site ghosts are drawn now: wanted, or the Site planner is in hand.</summary>
        public static bool Visible => Shown || PlannerInHand;

        private static bool PlannerInHand
        {
            get
            {
                Player player = Player.m_localPlayer;
                return BlueprintMenu.InHand(player) && BlueprintMenu.IsPlanner(player.GetSelectedPiece());
            }
        }

        /// <summary>A click on the entry: flips the switch, says so, and draws the tab again (its icon and words).</summary>
        public static void Toggle()
        {
            Tab.TabMemory.Load();
            Tab.TabMemory.GhostsShown = !Tab.TabMemory.GhostsShown;
            Player.m_localPlayer?.PlayButtonSound();
            Messages.TopLeft(Shown ? BlueprintWords.GhostsOn : BlueprintWords.GhostsOff);
            BlueprintEntries.Ghosts();
            BlueprintTab.Redraw();
        }
    }
}
