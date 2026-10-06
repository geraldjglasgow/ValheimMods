using PackPanel.Core;

namespace PackPanel.Panels
{
    /// <summary>The $packpanel_tip_ words of the stat sheet's breakdowns (<see cref="StatTips"/>, <see cref="GearTips"/>).</summary>
    public static class StatTipWords
    {
        public static string Base { get; private set; }
        public static string Other { get; private set; }
        public static string Nothing { get; private set; }
        public static string Effect { get; private set; }
        public static string CarryWeight { get; private set; }
        public static string World { get; private set; }
        public static string Heaviest { get; private set; }
        public static string MissingHealth { get; private set; }
        public static string ParryArmour { get; private set; }
        public static string Skill { get; private set; }
        public static string Capped { get; private set; }

        public static void Register()
        {
            Base = Language.Add("packpanel_tip_base", "Base");
            Other = Language.Add("packpanel_tip_other", "Other effects");
            Nothing = Language.Add("packpanel_tip_nothing", "Nothing you wear or carry gives this");
            Effect = Language.Add("packpanel_tip_effect", "An effect");
            CarryWeight = Language.Add("packpanel_tip_carry", "Carry weight");
            World = Language.Add("packpanel_tip_world", "World modifier");
            Heaviest = Language.Add("packpanel_tip_heaviest", "Heaviest");
            MissingHealth = Language.Add("packpanel_tip_missinghealth", "Missing health");
            ParryArmour = Language.Add("packpanel_tip_parryarmor", "Block armor on a parry");
            Skill = Language.Add("packpanel_tip_skill", "Skill");
            Capped = Language.Add("packpanel_tip_capped", "Over the cap: the game applies less than the sum");
        }
    }
}
