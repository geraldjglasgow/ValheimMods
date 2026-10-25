using StatusTemplates;
using UnityEngine;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// HUD icons for the affix states a player could not otherwise see: Evader's Fury and Steel Rhythm while their
    /// window runs (with the game's own countdown), the Runic Ward while charged. Each is a plain status effect added
    /// locally to the local player (never registered in ObjectDB, never sent); its icon is the equipped item that grants
    /// the affix, its name the English word <c>$ecf_fx_*</c>. Local player only.
    /// </summary>
    internal static class Indicators
    {
        public const int Fury = 0, Rhythm = 1, Ward = 2;

        private static readonly EffectKind[] Kinds = { EffectKind.DodgeFury, EffectKind.ComboFinisher, EffectKind.CalmWard };
        private static readonly StatusTemplate<StatusEffect>[] Templates =
        {
            Indicator("ECF_Indicator_Fury", "$ecf_fx_fury"),
            Indicator("ECF_Indicator_Rhythm", "$ecf_fx_rhythm"),
            Indicator("ECF_Indicator_Ward", "$ecf_fx_ward"),
        };
        private static readonly Sprite?[] Icons = new Sprite?[3];

        /// <summary>Rebuild: forget the icons before the equipped affixes are walked again.</summary>
        public static void ClearIcons()
        {
            Icons[0] = Icons[1] = Icons[2] = null;
        }

        /// <summary>Rebuild: the first equipped item feeding an indicated kind lends its icon.</summary>
        public static void Note(EffectKind kind, ItemDrop.ItemData item)
        {
            for (int i = 0; i < Kinds.Length; i++)
            {
                if (Kinds[i] == kind && Icons[i] == null)
                {
                    Icons[i] = item.GetIcon();
                }
            }
        }

        /// <summary>Shows (or restarts) an indicator; <paramref name="seconds"/> 0 = until hidden.</summary>
        public static void Show(int indicator, float seconds)
        {
            Player? player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                return;
            }
            StatusEffect? effect = Templates[indicator].Hold(player);
            if (effect != null)
            {
                effect.m_ttl = Mathf.Max(0f, seconds);
                effect.m_time = 0f;
                effect.m_icon = Icons[indicator];
            }
        }

        public static void Hide(int indicator) => Templates[indicator].Drop(Player.m_localPlayer);

        private static StatusTemplate<StatusEffect> Indicator(string name, string title) =>
            new StatusTemplate<StatusEffect>(name, se => se.m_name = title);
    }
}
