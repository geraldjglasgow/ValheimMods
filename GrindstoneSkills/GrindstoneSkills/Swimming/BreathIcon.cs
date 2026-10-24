using Translations;
using UnityEngine;

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
        private const string EffectName = "grindstone_breath";
        private static readonly int Hash = EffectName.GetStableHashCode();

        private static BreathEffect template;
        private static BreathEffect shown;

        public static void Show(Player player, float left)
        {
            SEMan seman = player.GetSEMan();
            if (shown == null || !seman.HaveStatusEffect(Hash))
                shown = seman.AddStatusEffect(Template(player)) as BreathEffect;
            if (shown != null)
                shown.Left = left;
        }

        public static void Hide(Player player)
        {
            shown = null;
            player.GetSEMan().RemoveStatusEffect(Hash, true);
        }

        private static BreathEffect Template(Player player)
        {
            if (template == null)
            {
                template = ScriptableObject.CreateInstance<BreathEffect>();
                template.name = EffectName;
            }
            template.m_name = Lang.Get("gs_swim_breath");
            template.m_tooltip = Lang.Get("gs_swim_breath_tooltip");
            Skills.SkillDef swim = player.GetSkills().GetSkillDef(Skills.SkillType.Swim);
            template.m_icon = swim != null ? swim.m_icon : null;
            return template;
        }
    }
}
