using System;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>What a trail's patch does to a player standing in it.</summary>
    internal enum TrailFeel
    {
        /// <summary>Slows them while they stand in it (and, on ice, takes their grip).</summary>
        Slow,

        /// <summary>Sets them on fire, the game's own Burning, once a second while they stand in it.</summary>
        Burn,

        /// <summary>Holds them fast for a moment as they step in, then lets them walk out.</summary>
        Root,
    }

    /// <summary>
    /// What makes one ground trail itself rather than another: which mutation lays it, the ZDO keys its drops travel
    /// under, the colour its patches are drawn in, what a patch does to a player (<see cref="TrailFeel"/>), the status
    /// effect a player standing in one wears (its name, words and icon), how long that lingers after they step out, and
    /// whether it slips. Everything else - laying, drawing, the player's footing - is shared, so Frostbound's ice,
    /// Mudbound's mud, Flamebound's fire and Binding's roots are one mechanism with four descriptions. Ice and mud take
    /// their numbers from the rule file; fire and roots have fixed ones, here (<see cref="Life"/> and on).
    /// </summary>
    internal sealed class TrailKind
    {
        /// <summary>Frostbound's ice: pale, glossy blue; the player slides on it and is a little slower.</summary>
        public static readonly TrailKind Frost = new TrailKind
        {
            Index = 0,
            Mutation = Mutation.Frostbound,
            Name = "ice",
            TrailKey = TraitKeys.FrostTrail,
            SeqKey = TraitKeys.FrostTrailSeq,
            Colour = new Color(0.78f, 0.91f, 1f, 0.8f),
            StatusKey = "ecr_slick_ice",
            StatusName = "Slick ice",
            Tooltip = "Frostbound ice underfoot: your feet slide, so starting, turning and stopping all take longer.",
            IconEffects = Array.Empty<string>(),
            IconItems = new[] { "Ice", "Crystal", "FreezeGland" },
            Linger = 0.3f,
            Slips = true,
            Keywords = new[] { "frost", "ice", "cold", "blob", "splat" },
        };

        /// <summary>Mudbound's mud: thick, dark and wet; the player wades through it slower.</summary>
        public static readonly TrailKind Mud = new TrailKind
        {
            Index = 1,
            Mutation = Mutation.Mudbound,
            Name = "mud",
            TrailKey = TraitKeys.MudTrail,
            SeqKey = TraitKeys.MudTrailSeq,
            Colour = new Color(0.2f, 0.14f, 0.07f, 0.93f),
            StatusKey = "ecr_deep_mud",
            StatusName = "Deep mud",
            Tooltip = "Mudbound mud underfoot: thick and deep, it drags at every step and clings a moment after.",
            IconEffects = new[] { "Tared" },
            IconItems = new[] { "Tar", "Guck" },
            Linger = 1f,
            Slips = false,
            Keywords = new[] { "tar", "mud", "blob", "splat" },
        };

        /// <summary>Flamebound's fire: glowing embers with flames over them; a player in it catches fire.</summary>
        public static readonly TrailKind Fire = new TrailKind
        {
            Index = 2,
            Mutation = Mutation.Flamebound,
            Name = "fire",
            TrailKey = TraitKeys.FireTrail,
            SeqKey = TraitKeys.FireTrailSeq,
            Colour = new Color(1f, 0.38f, 0.06f, 0.9f),
            Feel = TrailFeel.Burn,
            Keywords = new[] { "tar", "lava", "blob", "splat" },
            Life = 10f, Radius = 1.5f, Spacing = 1.5f, Strength = 6f, DecalEffect = "vfx_blobtar_death",
        };

        /// <summary>Binding's roots: tangled roots in the ground, apart from each other; a player stepping in is held.</summary>
        public static readonly TrailKind Roots = new TrailKind
        {
            Index = 3,
            Mutation = Mutation.Binding,
            Name = "roots",
            TrailKey = TraitKeys.BindTrail,
            SeqKey = TraitKeys.BindTrailSeq,
            Colour = new Color(0.33f, 0.4f, 0.13f, 0.95f),
            StatusKey = "ecr_rooted",
            StatusName = "Rooted",
            Tooltip = "Binding roots hold your feet: you cannot walk, run, jump or roll until they let go.",
            IconItems = new[] { "Root", "ElderBark", "RoundLog" },
            Feel = TrailFeel.Root,
            Keywords = new[] { "tar", "mud", "blob", "splat" },
            Life = 10f, Radius = 1.2f, Spacing = 3f, Strength = 1f, DecalEffect = "vfx_blobtar_death",
        };

        /// <summary>Every kind, by <see cref="Index"/>.</summary>
        public static readonly TrailKind[] All = { Frost, Mud, Fire, Roots };

        /// <summary>The kind <paramref name="mutation"/> lays; mud for any mutation that lays none.</summary>
        public static TrailKind Of(Mutation mutation)
        {
            foreach (TrailKind kind in All)
            {
                if (kind.Mutation == mutation)
                {
                    return kind;
                }
            }
            return Mud;
        }

        public int Index { get; private set; }
        public Mutation Mutation { get; private set; }

        /// <summary>A lower-case word for logs and object names.</summary>
        public string Name { get; private set; } = "";

        public string TrailKey { get; private set; } = "";
        public string SeqKey { get; private set; } = "";

        /// <summary>
        /// The tint every patch of this kind is drawn with; its alpha is the patch's opacity at full strength.
        /// </summary>
        public Color Colour { get; private set; }

        /// <summary>The status effect's object name, from which the game takes its identity hash.</summary>
        public string StatusKey { get; private set; } = "";

        public string StatusName { get; private set; } = "";
        public string Tooltip { get; private set; } = "";

        /// <summary>The game's own status effects whose icon to borrow, in order, before the items.</summary>
        public string[] IconEffects { get; private set; } = Array.Empty<string>();

        /// <summary>Item icons to borrow, in order, should no status effect above give one.</summary>
        public string[] IconItems { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Seconds the status effect (and with it the slow, or the slide) stays after the player steps out.
        /// </summary>
        public float Linger { get; private set; }

        /// <summary>True for ice: a player standing on it loses most of their grip.</summary>
        public bool Slips { get; private set; }

        public TrailFeel Feel { get; private set; } = TrailFeel.Slow;

        /// <summary>Fire and roots only: seconds a patch lasts, metres it reaches, metres walked between two.</summary>
        public float Life { get; private set; }
        public float Radius { get; private set; }
        public float Spacing { get; private set; }

        /// <summary>
        /// Fire and roots only: the fire a second a patch burns a player with before stars (fire), or the seconds it
        /// holds them (roots).
        /// </summary>
        public float Strength { get; private set; }

        /// <summary>Fire and roots only: the game effect whose ground decal draws the patches.</summary>
        public string DecalEffect { get; private set; } = "";

        /// <summary>True when the rule file sets the trail's numbers (ice and mud); false for the fixed ones.</summary>
        public bool Ruled => Feel == TrailFeel.Slow;

        /// <summary>
        /// Words a fallback ground decal's effect name is matched against, should the configured one have none.
        /// </summary>
        public string[] Keywords { get; private set; } = Array.Empty<string>();

        private int _statusHash;
        private int _seqHash;

        /// <summary>The status effect's identity hash, worked out once.</summary>
        public int StatusHash => _statusHash != 0 ? _statusHash : _statusHash = StatusKey.GetStableHashCode();

        /// <summary>The drop counter's ZDO hash, worked out once: every machine reads it every frame.</summary>
        public int SeqHash => _seqHash != 0 ? _seqHash : _seqHash = SeqKey.GetStableHashCode();

        /// <summary>
        /// The rule-file setting the decal effect comes from, named in a log line when it fails to resolve (for fire and
        /// roots, which have no setting, the trail's own name).
        /// </summary>
        public string Setting => Ruled ? $"{MutationCatalog.Word(Mutation)} trail effect" : $"{MutationCatalog.Word(Mutation)} trail";
    }
}
