using System.Collections.Generic;

namespace Broadside
{
    /// <summary>The three kinds of generated hull part a biome tier dresses: the planking, the keel (with its ribs and post caps), the deck.</summary>
    public enum KwPart
    {
        Hull,
        Keel,
        Deck,
    }

    /// <summary>
    /// One biome's tier for the hull parts: its saved id, its name in the tools' windows, and for each kind of part the two
    /// materials it is made of (the game's item prefab names), the bulk of it first, its fittings second. What it looks
    /// like is <see cref="KwBiomeLooks"/>; what a ship built from the hammer pays for it, <see cref="KwBiomeCost"/>. On the
    /// dock a tier is a look only: every part there is free. New work is offered Meadows and Black Forest only
    /// (<see cref="KwBiomeTiers.Offered"/>); a part saved in another keeps it.
    /// </summary>
    public sealed class KwBiomeTier
    {
        private readonly (string Main, string Trim)[] materials;

        public KwBiomeTier(string id, string name, (string, string) hull, (string, string) keel, (string, string) deck)
        {
            Id = id;
            Name = name;
            materials = new[] { hull, keel, deck };
        }

        public string Id { get; }
        public string Name { get; }

        public (string Main, string Trim) Materials(KwPart part) => materials[(int)part];
    }

    /// <summary>
    /// The seven biome tiers of the hull parts, Meadows to Ashlands, and where a part keeps its own: its ZDO's
    /// <see cref="Key"/>, set when a tool builds it, saved in blueprints with the part's other values (<see cref="KwBpData"/>).
    /// A part with no tier saved, or one this list does not know, is Meadows. The game's Elder Bark is the swamp hull's "elder bark" and the mountain deck's "ancient bark" alike
    /// (the game calls it Ancient Bark). Only the first two are offered for new work (<see cref="Offered"/>); all seven still
    /// load, draw and cost, so a part built or saved in a higher tier keeps working.
    /// </summary>
    public static class KwBiomeTiers
    {
        public const string Key = "kw_tier";
        public static readonly int Hash = Key.GetStableHashCode();

        public static readonly IReadOnlyList<KwBiomeTier> All = new[]
        {
            new KwBiomeTier("meadows", "Meadows", ("Wood", "Resin"), ("Wood", "Stone"), ("Wood", "LeatherScraps")),
            new KwBiomeTier("blackforest", "Black Forest", ("FineWood", "BronzeNails"), ("RoundLog", "Bronze"), ("FineWood", "Resin")),
            new KwBiomeTier("swamp", "Swamp", ("ElderBark", "IronNails"), ("Iron", "Chain"), ("ElderBark", "FineWood")),
            new KwBiomeTier("mountains", "Mountains", ("Silver", "WolfPelt"), ("Silver", "Obsidian"), ("ElderBark", "WolfPelt")),
            new KwBiomeTier("plains", "Plains", ("BlackMetal", "Tar"), ("BlackMetal", "LinenThread"), ("Flax", "LinenThread")),
            new KwBiomeTier("mistlands", "Mistlands", ("YggdrasilWood", "Carapace"), ("BlackMarble", "Eitr"), ("YggdrasilWood", "Sap")),
            new KwBiomeTier("ashlands", "Ashlands", ("Blackwood", "FlametalNew"), ("FlametalNew", "MoltenCore"), ("Blackwood", "CharredBone")),
        };

        /// <summary>
        /// The tiers the tools' and edit windows offer for new work: Meadows and Black Forest for now (the user's words,
        /// 2026-10-26: "right now we should only be supporting the Meadows and Black Forest textures and sails and everything
        /// like that. We should not have any of the higher tier stuff yet"). An edit window on a part of another tier shows
        /// that tier after these, kept unless changed (<see cref="KwBiomeField"/>).
        /// </summary>
        public static readonly IReadOnlyList<KwBiomeTier> Offered = new[] { All[0], All[1] };

        public static KwBiomeTier Meadows => All[0];

        /// <summary>Whether new work may pick this tier.</summary>
        public static bool Offers(KwBiomeTier tier)
        {
            foreach (KwBiomeTier offered in Offered)
            {
                if (offered == tier)
                    return true;
            }
            return false;
        }

        /// <summary>The tier saved under this id; Meadows for none or an unknown one.</summary>
        public static KwBiomeTier Of(string? id)
        {
            foreach (KwBiomeTier tier in All)
            {
                if (tier.Id == id)
                    return tier;
            }
            return Meadows;
        }

        public static KwBiomeTier Of(ZDO? zdo) => Of(zdo?.GetString(Key));

        /// <summary>A blueprint piece's tier, from its saved values.</summary>
        public static KwBiomeTier Of(List<KwBpValue> data) => Of(KwBpData.Text(data, Hash));

        /// <summary>The kind of part a tool's generated part is, when the tiers dress it (planking, deck, keel, rib); null for the others.</summary>
        public static KwPart? PartOf(string tool) => tool switch
        {
            "planking" => KwPart.Hull,
            "deck" => KwPart.Deck,
            KwShapeTool.Keel or KwShapeTool.Rib => KwPart.Keel,
            _ => (KwPart?)null,
        };
    }
}
