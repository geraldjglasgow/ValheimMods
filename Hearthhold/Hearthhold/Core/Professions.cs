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

        public static string Name(StarSource source)
        {
            switch (source)
            {
                case StarSource.Forage: return "Botanist";
                case StarSource.Crop: return "Tiller";
                case StarSource.Meat: return "Rancher";
                case StarSource.Fillet: return "Angler";
                default: return "Gourmet";
            }
        }

        public static string Description(StarSource source)
        {
            switch (source)
            {
                case StarSource.Forage: return "Wild picks, honey and sap are always gold.";
                case StarSource.Crop: return "Crops you harvest are at least silver.";
                case StarSource.Meat: return "Meat from creatures you kill is at least silver.";
                case StarSource.Fillet: return "Raw fish you clean is at least silver.";
                default: return "Dishes and meads you make are at least silver.";
            }
        }
    }
}
