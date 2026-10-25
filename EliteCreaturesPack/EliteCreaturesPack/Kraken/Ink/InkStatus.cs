using EliteCreaturesPack.Core;
using HarmonyLib;
using StatusTemplates;
using UnityEngine;

namespace EliteCreaturesPack.Kraken
{
    /// <summary>
    /// "Inked": the status effect a kraken's ink hit carries. The hit is an ordinary game hit, so the game applies the
    /// effect on the victim's own machine and already drops it for a dodged, blocked or parried hit. When it starts on
    /// the local player, or is renewed by another hit while it lasts, the screen is covered in ink for <see cref="Hold"/>
    /// seconds and then fades (<see cref="InkScreen"/>). A copy of the effect on anyone else draws nothing.
    /// <para>
    /// The one registered instance is kept in the game's status effect table (<c>ObjectDB.m_StatusEffects</c>), where
    /// the hit's hash is looked up: in the live database now, and in every database the game builds or copies later.
    /// </para>
    /// </summary>
    public class InkStatus : StatusEffect
    {
        /// <summary>The ScriptableObject's name; the game hashes it (<c>NameHash</c>) to find the effect.</summary>
        public const string Name = "ECP_KrakenInk";
        public const string WordName = "$se_ecp_inked";
        public const string WordTooltip = "$se_ecp_inked_tooltip";

        /// <summary>Seconds the ink takes to fade once the hold is over.</summary>
        public const float Fade = 0.7f;

        // The game treats a time to live of 0 as "forever", so the effect always keeps a little.
        private const float ShortestLife = 0.1f;

        private static readonly StatusTemplate<InkStatus> Ink = new StatusTemplate<InkStatus>(Name, se =>
        {
            se.m_name = WordName;
            se.m_tooltip = WordTooltip;
            se.m_ttl = Life;
        }, () => sprite);

        private static Sprite? sprite;
        private static float hold = 1.5f;

        /// <summary>The hash a hit carries in <c>HitData.m_statusEffectHash</c>.</summary>
        public static int Hash { get; } = Name.GetStableHashCode();

        /// <summary>Seconds the screen stays covered; also the effect's time to live. Never below 0.</summary>
        public static float Hold
        {
            get => hold;
            set
            {
                hold = Mathf.Max(0f, value);
                Ink.Get().m_ttl = Life;
            }
        }

        private static float Life => Mathf.Max(hold, ShortestLife);

        /// <summary>
        /// Builds the effect (once) and keeps it in every ObjectDB, the live one now and every later one (the
        /// StatusTemplates library's <c>StatusTable</c>). Calling it again only updates the icon.
        /// </summary>
        public static void Register(Harmony harmony, Sprite? icon)
        {
            sprite = icon;
            Ink.Get().m_icon = icon;
            StatusTable.Register(harmony, Ink.Get);
        }

        public override void Setup(Character character)
        {
            base.Setup(character);
            Splash(character);
        }

        /// <summary>Hit again while inked: the game renews the effect, and the screen is splashed afresh.</summary>
        public override void ResetTime()
        {
            base.ResetTime();
            Splash(m_character);
        }

        private static void Splash(Character character)
        {
            if (character != null && character == Player.m_localPlayer)
            {
                SafeCall.Run("InkStatus splash", () => InkScreen.Splat(hold, Fade));
            }
        }
    }
}
