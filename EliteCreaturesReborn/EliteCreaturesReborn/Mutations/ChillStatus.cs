using StatusTemplates;
using Translations;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The "Chilled" status a Frostbound creature's aura puts on a player, made at runtime as a <see cref="ChillEffect"/>
    /// so the HUD shows it as it shows any other: an icon (the game's own frost one), its name, and the stamina line in
    /// its tooltip. It is handed to the player's status effects as an object rather than by name, so it needs no place in
    /// the game's object database, and the game never writes status effects into the character file, so a character saved
    /// while chilled loads cleanly with or without the mod. Lives on this machine's own player only.
    /// </summary>
    internal static class ChillStatus
    {
        /// <summary>Seconds it lingers after the last aura check in reach: a few checks, so leaving drops it quickly.</summary>
        private const float Linger = 1f;

        /// <summary>Item icons to borrow when the game's own frost and cold effects have none.</summary>
        private static readonly string[] IconItems = { "FreezeGland", "FrostCore", "Wishbone" };

        private static readonly int[] IconEffects = { SEMan.s_statusEffectFrost, SEMan.s_statusEffectCold };

        // Word tokens: the game words them in the player's language.
        private static readonly StatusTemplate<ChillEffect> Chilled = new StatusTemplate<ChillEffect>("ecr_chilled",
            se => (se.m_name, se.m_tooltip) = (Lang.Token("ecr_se_chilled"), Lang.Token("ecr_se_chilled_tooltip")),
            () => GameIcons.Effect(IconEffects) ?? GameIcons.Item(IconItems));

        /// <summary>Chills <paramref name="player"/> by <paramref name="cut"/> percent stamina regeneration, for now.</summary>
        public static void Hold(Player player, float cut) => Chilled.Hold(player)?.Hold(cut, Linger);
    }
}
