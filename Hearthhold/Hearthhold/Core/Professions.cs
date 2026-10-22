using Translations;

namespace Hearthhold
{
    /// <summary>
    /// A profession for reaching level 100 in a skill: a floor under every star roll that skill makes. Botanist
    /// (Foraging): wild picks, honey and sap are always gold. Tiller (Farming): crops at least silver. Rancher (Husbandry):
    /// meat at least silver. Gourmet (Cooking): dishes, mead bases and meads at least silver. Angler (Fishing): raw fish you
    /// clean at least silver. The level is the actor's, carried to wherever the roll happens with the rest of the roll.
    /// </summary>
    public static class Professions
    {
        public const float Level = 100f;

        /// <summary>The fewest stars a roll of this source gives at this level; 0 below level 100.</summary>
        public static int Floor(StarSource source, float level) => Has(level) ? FloorOf(source) : 0;

        public static bool Has(float level) => level >= Level - 0.01f;

        public static int FloorOf(StarSource source) => source == StarSource.Forage ? 3 : 2;

        /// <summary>The profession's name in the player's language.</summary>
        public static string Name(StarSource source)
        {
            switch (source)
            {
                case StarSource.Forage: return Lang.Get("hh_profession_botanist");
                case StarSource.Crop: return Lang.Get("hh_profession_tiller");
                case StarSource.Meat: return Lang.Get("hh_profession_rancher");
                case StarSource.Fillet: return Lang.Get("hh_profession_angler");
                default: return Lang.Get("hh_profession_gourmet");
            }
        }

        /// <summary>What the profession does, in the player's language.</summary>
        public static string Description(StarSource source)
        {
            switch (source)
            {
                case StarSource.Forage: return Lang.Get("hh_profession_botanist_desc");
                case StarSource.Crop: return Lang.Get("hh_profession_tiller_desc");
                case StarSource.Meat: return Lang.Get("hh_profession_rancher_desc");
                case StarSource.Fillet: return Lang.Get("hh_profession_angler_desc");
                default: return Lang.Get("hh_profession_gourmet_desc");
            }
        }
    }
}
