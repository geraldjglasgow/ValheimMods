using System.Collections.Generic;
using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One eitr food of the casters' (features/casters.md, "Eitr food before the Mistlands": the game's food curve, its
    /// eitr dishes' shape): its prefab, the game dish it is a copy of, the workshop model and icon, its word
    /// (translations/English.yml: the name, and the description as the word + "_desc"), what it gives
    /// (health, stamina, eitr, how long, health regen), its recipe, where it is made and how many a craft makes.
    /// </summary>
    public sealed class FoodPiece
    {
        public string Prefab;
        public string Base;
        public string Bundle;
        public string Asset;
        public string Word;
        public float Health;
        public float Stamina;
        public float Eitr;
        public float Seconds;
        public float Regen;
        public string RecipeBeside;
        public int Makes = 1;
        public GearCost[] Cost;

        public GameObject Item;

        public string Token => Lang.Token(Word);
        public string DescriptionToken => Lang.Token(Word + "_desc");
    }

    /// <summary>The casters' twelve eitr foods (agreed 2026-10-10), the Meadows to the Plains.</summary>
    public static class CasterFood
    {
        public static readonly FoodPiece DandelionMash = new FoodPiece
        {
            Prefab = "EE_Food_DandelionMash", Base = "CarrotSoup", Bundle = "ee_food_meadows", Asset = "ee_food_dandelionmash",
            Word = "ee_food_dandelionmash",
            Health = 12f, Stamina = 6f, Eitr = 35f, Seconds = 1500f, Regen = 1f,
            // The workbench, beside the flint axe (the club is made by hand, at no station): the Meadows have no cauldron yet.
            RecipeBeside = "AxeFlint",
            Cost = new[] { new GearCost("Dandelion", 3, 0), new GearCost("Honey", 1, 0), new GearCost("Mushroom", 1, 0) },
        };

        // The Black Forest's three (made 2026-10-11): a sweet snack giving an even third, a broth and a porridge giving the
        // biome's 45 in eitr at their best; all at the cauldron, level 1, beside the game's dishes of that level.
        public static readonly FoodPiece CandiedThistle = new FoodPiece
        {
            Prefab = "EE_Food_CandiedThistle", Base = "BoarJerky", Bundle = "ee_food_blackforest", Asset = "ee_food_candiedthistle",
            Word = "ee_food_candiedthistle",
            Health = 15f, Stamina = 15f, Eitr = 15f, Seconds = 900f, Regen = 1f, RecipeBeside = "QueensJam", Makes = 2,
            Cost = new[] { new GearCost("Thistle", 3, 0), new GearCost("Honey", 1, 0) },
        };

        public static readonly FoodPiece GlowcapBroth = new FoodPiece
        {
            Prefab = "EE_Food_GlowcapBroth", Base = "CarrotSoup", Bundle = "ee_food_blackforest", Asset = "ee_food_glowcapbroth",
            Word = "ee_food_glowcapbroth",
            Health = 15f, Stamina = 8f, Eitr = 40f, Seconds = 1500f, Regen = 2f, RecipeBeside = "CarrotSoup",
            Cost = new[] { new GearCost("MushroomYellow", 2, 0), new GearCost("Thistle", 2, 0), new GearCost("Carrot", 1, 0) },
        };

        public static readonly FoodPiece SeersPorridge = new FoodPiece
        {
            Prefab = "EE_Food_SeersPorridge", Base = "TurnipStew", Bundle = "ee_food_blackforest", Asset = "ee_food_seersporridge",
            Word = "ee_food_seersporridge",
            Health = 15f, Stamina = 8f, Eitr = 45f, Seconds = 1800f, Regen = 2f, RecipeBeside = "DeerStew",
            Cost = new[] { new GearCost("Blueberries", 4, 0), new GearCost("Thistle", 2, 0), new GearCost("Honey", 1, 0) },
        };

        // Swamp, Mountains and Plains (models by ChatGPT, user 2026-10-11), at the cauldron of each biome's level, beside a
        // game dish of that level. Bases by shape: snacks copy the jerky, drinks and broths the carrot soup, stews the turnip stew.
        public static readonly FoodPiece TurnipCrisps = Food("turnipcrisps", "TurnipCrisps", "BoarJerky", "ee_food_swamp",
            18f, 18f, 18f, 900f, 1f, "TurnipStew", 2, new GearCost("Turnip", 3, 0), new GearCost("Thistle", 1, 0));

        public static readonly FoodPiece BloodDraught = Food("blooddraught", "BloodDraught", "CarrotSoup", "ee_food_swamp",
            18f, 9f, 50f, 1500f, 2f, "TurnipStew", 1, new GearCost("Bloodbag", 2, 0), new GearCost("Thistle", 2, 0), new GearCost("Turnip", 1, 0));

        public static readonly FoodPiece MireStew = Food("mirestew", "MireStew", "TurnipStew", "ee_food_swamp",
            18f, 9f, 55f, 1800f, 2f, "TurnipStew", 1, new GearCost("Turnip", 3, 0), new GearCost("Bloodbag", 1, 0), new GearCost("MushroomYellow", 1, 0));

        public static readonly FoodPiece GlacierTea = Food("glaciertea", "GlacierTea", "CarrotSoup", "ee_food_mountains",
            22f, 11f, 60f, 1500f, 2f, "OnionSoup", 1, new GearCost("Onion", 2, 0), new GearCost("FreezeGland", 1, 0), new GearCost("Honey", 1, 0), new GearCost("Thistle", 1, 0));

        public static readonly FoodPiece WolfheartStew = Food("wolfheartstew", "WolfheartStew", "TurnipStew", "ee_food_mountains",
            22f, 11f, 65f, 1800f, 2f, "OnionSoup", 1, new GearCost("WolfMeat", 1, 0), new GearCost("Onion", 3, 0), new GearCost("FreezeGland", 2, 0));

        public static readonly FoodPiece CandiedCloudberries = Food("candiedcloudberries", "CandiedCloudberries", "BoarJerky", "ee_food_plains",
            25f, 25f, 25f, 900f, 1f, "BloodPudding", 2, new GearCost("Cloudberry", 4, 0), new GearCost("Honey", 1, 0));

        public static readonly FoodPiece LoxMarrowBroth = Food("loxmarrowbroth", "LoxMarrowBroth", "CarrotSoup", "ee_food_plains",
            25f, 12f, 70f, 1500f, 3f, "BloodPudding", 1, new GearCost("LoxMeat", 1, 0), new GearCost("Cloudberry", 2, 0), new GearCost("BarleyFlour", 3, 0));

        public static readonly FoodPiece SunbarleyPorridge = Food("sunbarleyporridge", "SunbarleyPorridge", "TurnipStew", "ee_food_plains",
            25f, 12f, 75f, 1800f, 3f, "BloodPudding", 1, new GearCost("BarleyFlour", 4, 0), new GearCost("Cloudberry", 3, 0), new GearCost("Honey", 1, 0));

        public static readonly IReadOnlyList<FoodPiece> All = new[]
        {
            DandelionMash, CandiedThistle, GlowcapBroth, SeersPorridge, TurnipCrisps, BloodDraught, MireStew, GlacierTea,
            WolfheartStew, CandiedCloudberries, LoxMarrowBroth, SunbarleyPorridge,
        };

        private static FoodPiece Food(string key, string prefab, string dish, string bundle, float health,
            float stamina, float eitr, float seconds, float regen, string beside, int makes, params GearCost[] cost) =>
            new FoodPiece
            {
                Prefab = "EE_Food_" + prefab, Base = dish, Bundle = bundle,
                Asset = "ee_food_" + key, Word = "ee_food_" + key, Health = health,
                Stamina = stamina, Eitr = eitr, Seconds = seconds, Regen = regen, RecipeBeside = beside, Makes = makes, Cost = cost,
            };
    }
}
