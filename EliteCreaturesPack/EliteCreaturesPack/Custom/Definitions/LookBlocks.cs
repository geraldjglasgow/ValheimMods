using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Definitions
{
    /// <summary>
    /// `effects:` its effect lists replaced, each a list of prefab names (an empty list removes them all). A name that is
    /// a creature (game, mod or custom, even one that spawns this one back) spawns that creature. Applied by the look
    /// step (Custom/Look). Null keeps the base's list.
    /// </summary>
    public sealed class EffectsBlock
    {
        /// <summary>`hit`: played when a hit with physical damage takes more than a tenth of its health (the game's rule).</summary>
        public List<string>? Hit;

        /// <summary>`death`: played (or spawned) when it dies.</summary>
        public List<string>? Death;

        /// <summary>`alert`: played when it notices a target.</summary>
        public List<string>? Alert;

        /// <summary>`idle`: played now and then while idle.</summary>
        public List<string>? Idle;
    }

    /// <summary>`look:` how the base's body is drawn. Applied by the look step (Custom/Look): the size on every peer, the
    /// colours and the overlay on every peer that draws.</summary>
    public sealed class LookBlock
    {
        /// <summary>`size`: one number for all axes or [x, y, z], each 0.05 to 20, relative to the base.</summary>
        public Vector3? Size;

        /// <summary>`body tint`: a colour on its body (its materials' tint colour, which multiplies the texture),
        /// <c>"#rrggbb"</c> or [r, g, b] from 0 to 1. A human's skin keeps its skin tone: the player's body has no tint.</summary>
        public Color? BodyTint;

        /// <summary>`item tint`: a colour on the items it carries and wears (hair and beards keep theirs).</summary>
        public Color? ItemTint;

        /// <summary>`overlay`: smoke or flame over the whole body: the game's own look of a smoked or burning character,
        /// worn for good, drawn on each player's machine and never hurting anything.</summary>
        public OverlayKind? Overlay;

        /// <summary>`overlay colour`: the overlay's colour (an alpha below 1 makes it fainter); unset keeps the game's.</summary>
        public Color? OverlayColour;
    }

    /// <summary>
    /// `elite:` what Elite Creatures Reborn puts on it (features/custom-creatures.md section 8). Inactive without ECR: the
    /// creature loads all the same and one warning per load names it. Applied by the elite step (Custom/Elite).
    /// </summary>
    public sealed class EliteBlock
    {
        /// <summary>`mutations`: ECR mutation names, replacing ECR's random mutation roll (stars still roll).</summary>
        public readonly List<string> Mutations = new List<string>();

        /// <summary>`aspect`: one ECR boss aspect, or a list it may roll from; only on a creature that is a boss.</summary>
        public readonly List<string> Aspects = new List<string>();

        /// <summary>`portal attacks`: the projectile attacks (item names) the Portalbound aspect may send through portals.</summary>
        public readonly List<string> PortalAttacks = new List<string>();

        /// <summary>`summon`: what the Summoner aspect calls instead of its biome's choice.</summary>
        public readonly List<SummonEntry> Summon = new List<SummonEntry>();

        /// <summary>Whether any line asks ECR for something.</summary>
        public bool HasAny => Mutations.Count > 0 || Aspects.Count > 0 || PortalAttacks.Count > 0 || Summon.Count > 0;
    }

    /// <summary>One entry of `elite: summon:`, written as a creature name or as <c>{ creature: name, stars: n }</c>.</summary>
    public sealed class SummonEntry
    {
        /// <summary>`creature`: the prefab summoned.</summary>
        public string Creature = "";

        /// <summary>`stars`: its stars, 0 to 10; null lets ECR roll them.</summary>
        public int? Stars;
    }
}
