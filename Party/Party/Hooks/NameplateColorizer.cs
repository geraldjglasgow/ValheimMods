using Party.Client;

namespace Party.Hooks
{
    /// <summary>
    /// Recolors a party member's floating name where <see cref="EnemyHud"/> already draws it (the game's one class
    /// for every floating name-and-bar, players included - see PLAN.md, "Floating names and map pins"). A poll, not
    /// a Harmony patch: nothing needs to be intercepted, only read and re-colored every frame; left alone for
    /// everyone else.
    /// </summary>
    public static class NameplateColorizer
    {
        public static void Tick()
        {
            EnemyHud hud = EnemyHud.instance;
            if (hud == null || !PartyClientState.InParty)
                return;
            foreach (System.Collections.Generic.KeyValuePair<Character, EnemyHud.HudData> entry in hud.m_huds)
                Recolor(entry.Key, entry.Value);
        }

        private static void Recolor(Character character, EnemyHud.HudData data)
        {
            if (!(character is Player player) || data?.m_name == null)
                return;
            PartyMemberView member = PartyClientState.Find(player.GetPlayerID());
            if (member == null)
                return;
            data.m_name.color = member.IsLeader ? ColorHelper.Parse(PartyConfig.LeaderColor.Value) : ColorHelper.Parse(PartyConfig.PartyColor.Value);
        }
    }
}
