using Party.Client;

namespace Party.Hooks
{
    /// <summary>Recolors a party member's floating name via <see cref="EnemyHud"/>. A poll, not a patch.</summary>
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
