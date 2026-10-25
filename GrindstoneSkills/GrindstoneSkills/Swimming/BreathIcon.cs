using StatusTemplates;
using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// The Breath icon among the local player's status effects, with the seconds of breath left under it: the game's own
    /// status effect display, so it looks like the game's. A <see cref="BreathEffect"/> is added to the player's SEMan
    /// directly, on their own client: it is never networked, never saved and not in ObjectDB, so nobody else sees it and
    /// it is gone after a relog. Its icon is the Swim skill's, its name in the player's language.
    /// </summary>
    public static class BreathIcon
    {
        private static readonly StatusTemplate<BreathEffect> Breath = new StatusTemplate<BreathEffect>("grindstone_breath");

        private static BreathEffect shown;

        public static void Show(Player player, float left)
        {
            if (shown == null || !Breath.Has(player))
            {
                Dress(player);
                shown = Breath.Hold(player);
            }
            if (shown != null)
                shown.Left = left;
        }

        public static void Hide(Player player)
        {
            shown = null;
            Breath.Drop(player);
        }

        // The words and icon again before each new copy: the language may have changed since the last one.
        private static void Dress(Player player)
        {
            BreathEffect template = Breath.Get();
            template.m_name = Lang.Get("gs_swim_breath");
            template.m_tooltip = Lang.Get("gs_swim_breath_tooltip");
            Skills.SkillDef swim = player.GetSkills().GetSkillDef(Skills.SkillType.Swim);
            template.m_icon = swim != null ? swim.m_icon : null;
        }
    }
}
