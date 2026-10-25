using StatusTemplates;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The status effect a player wears while standing in a trail's patch - "Slick ice" or "Deep mud" - or for a moment
    /// after roots take them ("Rooted": no movement and no jump, <see cref="Root"/>), made at runtime
    /// as a plain game <c>SE_Stats</c> so the HUD shows it as it shows any other: an icon borrowed from the game, its
    /// name, and a tooltip that, besides its own words, lists the movement it takes away. The slow is the effect's own
    /// speed modifier, so the game applies it wherever it applies any status effect's. It has no timer while the player
    /// stands in a patch, and a short one (the kind's linger) once they step out, after which the game removes it.
    /// Handed to the player's status effects as an object, never by name, so it needs no place in the object database,
    /// and the game never writes status effects into the character file, so a character saved in mud loads cleanly with
    /// or without the mod.
    /// </summary>
    internal static class TrailStatus
    {
        private static readonly StatusTemplate<SE_Stats>?[] Templates = new StatusTemplate<SE_Stats>?[TrailKind.All.Length];

        /// <summary>The kind's status on <paramref name="player"/> now, or null.</summary>
        public static StatusEffect? On(Player player, TrailKind kind) => Template(kind).On(player);

        /// <summary>
        /// Keeps <paramref name="player"/> in the kind's status, with no timer, taking <paramref name="slow"/> of their
        /// speed (0 to 1). Null only when the game refused the effect.
        /// </summary>
        public static SE_Stats? Hold(Player player, TrailKind kind, float slow)
        {
            SE_Stats? status = Template(kind).Hold(player);
            if (status != null)
            {
                status.m_speedModifier = -slow;
                status.m_ttl = 0f; // endless while they stand in it
            }
            return status;
        }

        /// <summary>
        /// Holds <paramref name="player"/> fast for <paramref name="seconds"/>: the kind's status takes all their speed and
        /// their jump, and the game removes it when the time is up. Null only when the game refused the effect.
        /// </summary>
        public static SE_Stats? Root(Player player, TrailKind kind, float seconds)
        {
            SE_Stats? status = Template(kind).Add(player, resetTime: true);
            if (status != null)
            {
                status.m_speedModifier = -1f;
                status.m_jumpModifier = new Vector3(-1f, -1f, -1f); // the jump's every part cancelled: no jump at all
                status.m_ttl = seconds;
            }
            return status;
        }

        /// <summary>True while roots hold <paramref name="player"/>.</summary>
        public static bool Rooted(Player player) => Template(TrailKind.Roots).Has(player);

        /// <summary>
        /// The player stepped out (or off the ground): the status stays the kind's linger longer, then goes.
        /// </summary>
        public static void Release(Player player, TrailKind kind)
        {
            StatusEffect? status = On(player, kind);
            if (status != null && status.m_ttl <= 0f)
            {
                status.m_ttl = status.GetDuration() + kind.Linger;
            }
        }

        private static StatusTemplate<SE_Stats> Template(TrailKind kind) =>
            Templates[kind.Index] ??= new StatusTemplate<SE_Stats>(kind.StatusKey,
                se => (se.m_name, se.m_tooltip) = (kind.StatusName, kind.Tooltip),
                () => GameIcons.Effect(kind.IconEffects) ?? GameIcons.Item(kind.IconItems));
    }
}
