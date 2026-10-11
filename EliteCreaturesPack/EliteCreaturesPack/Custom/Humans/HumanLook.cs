using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// `human:` the ranges a human's random look is rolled within (features/custom-creatures.md section 4), read from a
    /// definition and handed to <see cref="HumanBody.Dress"/>. Each human rolls its own look on its owner and keeps it in
    /// its ZDO, so every player sees the same person. A value the definition leaves out is null and keeps what its base
    /// gave, as every other setting does: on a human built on another custom human only the ranges it names change, and
    /// on the human base itself an unset value is "anything the game draws on players" (<see cref="HumanAppearance"/>'s
    /// own values). An empty list is a value: any of the game's again.
    /// </summary>
    public sealed class HumanLook
    {
        /// <summary>`gender`: male, female or random.</summary>
        public string? Gender;

        /// <summary>`hair`: hair item prefab names; empty = any of the game's; "none" allowed (bald).</summary>
        public List<string>? Hair;

        /// <summary>`beard`: beard item prefab names; empty = any; "none" allowed.</summary>
        public List<string>? Beard;

        /// <summary>`beardless women`: never a beard on a woman.</summary>
        public bool? BeardlessWomen;

        /// <summary>`hair colours`: the colours hair is picked from; empty = the game's natural range.</summary>
        public List<Color>? HairColours;

        /// <summary>`skin tone`: 0 lightest .. 1 darkest, rolled within (x the low end, y the high end).</summary>
        public Vector2? SkinTone;
    }
}
