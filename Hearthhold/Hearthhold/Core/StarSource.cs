namespace Hearthhold
{
    /// <summary>
    /// Where a star roll happens, and so whose skill counts: wild picks, honey and sap (Foraging), crops (Farming), meat
    /// from creatures (Husbandry), dishes, mead bases and meads (Cooking) and raw fish cleaned from a catch (Cooking, with
    /// the cleaner's Fishing for the Angler profession).
    /// </summary>
    public enum StarSource
    {
        Forage,
        Crop,
        Meat,
        Dish,
        Fillet,
    }

    public static class StarSources
    {
        /// <summary>The skill, by its panel name, whose level rolls this source's stars.</summary>
        public static string Skill(StarSource source)
        {
            switch (source)
            {
                case StarSource.Forage: return "Foraging";
                case StarSource.Crop: return "Farming";
                case StarSource.Meat: return "Husbandry";
                case StarSource.Fillet: return "Fishing";
                default: return "Cooking";
            }
        }
    }
}
