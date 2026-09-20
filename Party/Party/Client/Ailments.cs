using UnityEngine;

namespace Party.Client
{
    /// <summary>
    /// The ailment status effects reported alongside vitals, one bit per entry. Other players' status effects
    /// are not replicated by the game, so each client reports its own as a mask and the panel draws the icons
    /// from the receiving client's ObjectDB.
    /// </summary>
    public static class Ailments
    {
        private static readonly int[] hashes =
        {
            SEMan.s_statusEffectBurning,
            SEMan.s_statusEffectSpirit,
            SEMan.s_statusEffectPoison,
            SEMan.s_statusEffectFrost,
            SEMan.s_statusEffectLightning,
            SEMan.s_statusEffectSmoked,
            SEMan.s_statusEffectTared,
            SEMan.s_statusEffectFreezing,
            SEMan.s_statusEffectCold,
            SEMan.s_statusEffectWet,
        };

        public static int Count => hashes.Length;

        /// <summary>Which tracked ailments this character currently has.</summary>
        public static int Mask(Character character)
        {
            SEMan seman = character != null ? character.GetSEMan() : null;
            if (seman == null)
                return 0;
            int mask = 0;
            for (int i = 0; i < hashes.Length; i++)
            {
                if (seman.HaveStatusEffect(hashes[i]))
                    mask |= 1 << i;
            }
            return mask;
        }

        /// <summary>The game's own icon for the ailment at a bit index, or null when the ObjectDB has none.</summary>
        public static Sprite Icon(int index)
        {
            StatusEffect effect = ObjectDB.instance != null ? ObjectDB.instance.GetStatusEffect(hashes[index]) : null;
            return effect != null ? effect.m_icon : null;
        }
    }
}
