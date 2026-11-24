using StatusTemplates;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Cleanse's carrier: a status effect with no icon, words or stats that, as it is put on a character, takes the
    /// harmful effects off (<see cref="CleanseList"/>) and goes at the next status update. It travels the game's own way:
    /// the caster adds it by name hash and the game sends that to the character's owner (<c>SEMan.AddStatusEffect</c>'s
    /// <c>RPC_AddStatusEffect</c>), where the status effects live and its setup runs, so Cleanse needs no message of its
    /// own and works on a dedicated server and on a tame another machine owns. In every item database with the casters'
    /// other effects (<see cref="CasterStatus"/>).
    /// </summary>
    public sealed class CleanseEffect : StatusEffect
    {
        public const string PrefabName = "EE_SE_Cleanse";

        public static int Hash => PrefabName.GetStableHashCode();

        public static CleanseEffect Make()
        {
            CleanseEffect effect = StatusTemplate.Make<CleanseEffect>(PrefabName);
            effect.m_name = "";
            effect.m_tooltip = "";
            effect.m_icon = null;
            effect.m_ttl = 0f;
            return effect;
        }

        /// <summary>Runs on the owner, right after the game added this copy (not while it walks its effects), so removing is safe.</summary>
        public override void Setup(Character character)
        {
            base.Setup(character);
            SEMan seman = character != null ? character.GetSEMan() : null;
            if (seman != null)
                CasterSafe.Run("cleanse", () => CleanseList.Strip(seman));
        }

        public override bool IsDone() => true;
    }
}
