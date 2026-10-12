using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One eitr food of the casters' (features/casters.md, "Eitr food before the Mistlands": the game's food curve, its
    /// eitr dishes' shape): its prefab, the game dish it is a copy of, the workshop model and icon, words, what it gives
    /// (health, stamina, eitr, how long, health regen), its recipe, where it is made and how many a craft makes.
    /// </summary>
    public sealed class FoodPiece
    {
        public string Prefab;
        public string Base;
        public string Bundle;
        public string Asset;
        public string Word;
        public string Name;
        public string Description;
        public float Health;
        public float Stamina;
        public float Eitr;
        public float Seconds;
        public float Regen;
        public string RecipeBeside;
        public int Makes = 1;
        public GearCost[] Cost;

        public GameObject Item;

        public string Token => "$" + Word;
        public string DescriptionToken => "$" + Word + "_desc";
    }

    /// <summary>The casters' twelve eitr foods (agreed 2026-10-10), the Meadows to the Plains.</summary>
    public static class CasterFood
    {
        public static readonly FoodPiece DandelionMash = new FoodPiece
        {
            Prefab = "EE_Food_DandelionMash", Base = "CarrotSoup", Bundle = "ee_food_meadows", Asset = "ee_food_dandelionmash",
            Word = "ee_food_dandelionmash", Name = "Dandelion Mash",
            Description = "Dandelion greens and flowers mashed with honey and mushroom. Little to fill you, but eitr for the wand.",
            Health = 12f, Stamina = 6f, Eitr = 35f, Seconds = 1500f, Regen = 1f,
            // The workbench, as the club is made: the Meadows have no cauldron yet.
            RecipeBeside = "Club",
            Cost = new[] { new GearCost("Dandelion", 3, 0), new GearCost("Honey", 1, 0), new GearCost("Mushroom", 1, 0) },
        };

        // The Black Forest's three (made 2026-10-11): a sweet snack giving an even third, a broth and a porridge giving the
        // biome's 45 in eitr at their best; all at the cauldron, level 1, beside the game's dishes of that level.
        public static readonly FoodPiece CandiedThistle = new FoodPiece
        {
            Prefab = "EE_Food_CandiedThistle", Base = "BoarJerky", Bundle = "ee_food_blackforest", Asset = "ee_food_candiedthistle",
            Word = "ee_food_candiedthistle", Name = "Candied Thistle",
            Description = "Thistle heads glazed in honey. A sweet bite on the trail, a little of everything.",
            Health = 15f, Stamina = 15f, Eitr = 15f, Seconds = 900f, Regen = 1f, RecipeBeside = "QueensJam", Makes = 2,
            Cost = new[] { new GearCost("Thistle", 3, 0), new GearCost("Honey", 1, 0) },
        };

        public static readonly FoodPiece GlowcapBroth = new FoodPiece
        {
            Prefab = "EE_Food_GlowcapBroth", Base = "CarrotSoup", Bundle = "ee_food_blackforest", Asset = "ee_food_glowcapbroth",
            Word = "ee_food_glowcapbroth", Name = "Glowcap Broth",
            Description = "Yellow mushrooms simmered with thistle and carrot until they glow faintly. Seers drink it before a long night.",
            Health = 15f, Stamina = 8f, Eitr = 40f, Seconds = 1500f, Regen = 2f, RecipeBeside = "CarrotSoup",
            Cost = new[] { new GearCost("MushroomYellow", 2, 0), new GearCost("Thistle", 2, 0), new GearCost("Carrot", 1, 0) },
        };

        public static readonly FoodPiece SeersPorridge = new FoodPiece
        {
            Prefab = "EE_Food_SeersPorridge", Base = "TurnipStew", Bundle = "ee_food_blackforest", Asset = "ee_food_seersporridge",
            Word = "ee_food_seersporridge", Name = "Seer's Porridge",
            Description = "Thick blueberry porridge with thistle and a drizzle of honey. It keeps the mind clear for hours.",
            Health = 15f, Stamina = 8f, Eitr = 45f, Seconds = 1800f, Regen = 2f, RecipeBeside = "DeerStew",
            Cost = new[] { new GearCost("Blueberries", 4, 0), new GearCost("Thistle", 2, 0), new GearCost("Honey", 1, 0) },
        };

        // Swamp, Mountains and Plains (models by ChatGPT, user 2026-10-11), at the cauldron of each biome's level, beside a
        // game dish of that level. Bases by shape: snacks copy the jerky, drinks and broths the carrot soup, stews the turnip stew.
        public static readonly FoodPiece TurnipCrisps = Food("turnipcrisps", "Turnip Crisps", "BoarJerky", "ee_food_swamp",
            "Thin turnip slices fried crisp, a thistle sprig on top. A snack for the bog road.",
            18f, 18f, 18f, 900f, 1f, "TurnipStew", 2, new GearCost("Turnip", 3, 0), new GearCost("Thistle", 1, 0));

        public static readonly FoodPiece BloodDraught = Food("blooddraught", "Blood Draught", "CarrotSoup", "ee_food_swamp",
            "A thick red draught of bloodbag, thistle and turnip. It tastes of iron and keeps the eitr flowing.",
            18f, 9f, 50f, 1500f, 2f, "TurnipStew", 1, new GearCost("Bloodbag", 2, 0), new GearCost("Thistle", 2, 0), new GearCost("Turnip", 1, 0));

        public static readonly FoodPiece MireStew = Food("mirestew", "Mire Stew", "TurnipStew", "ee_food_swamp",
            "A murky stew of turnip, bloodbag and yellow mushroom. It looks like the swamp and works like a charm.",
            18f, 9f, 55f, 1800f, 2f, "TurnipStew", 1, new GearCost("Turnip", 3, 0), new GearCost("Bloodbag", 1, 0), new GearCost("MushroomYellow", 1, 0));

        public static readonly FoodPiece GlacierTea = Food("glaciertea", "Glacier Tea", "CarrotSoup", "ee_food_mountains",
            "Onion and honey steeped with a freeze gland, frost on the rim. Clears the head on the high slopes.",
            22f, 11f, 60f, 1500f, 2f, "OnionSoup", 1, new GearCost("Onion", 2, 0), new GearCost("FreezeGland", 1, 0), new GearCost("Honey", 1, 0), new GearCost("Thistle", 1, 0));

        public static readonly FoodPiece WolfheartStew = Food("wolfheartstew", "Wolfheart Stew", "TurnipStew", "ee_food_mountains",
            "Wolf meat and onions stewed with freeze glands. A hunter's meal that leaves a cold fire behind the eyes.",
            22f, 11f, 65f, 1800f, 2f, "OnionSoup", 1, new GearCost("WolfMeat", 1, 0), new GearCost("Onion", 3, 0), new GearCost("FreezeGland", 2, 0));

        public static readonly FoodPiece CandiedCloudberries = Food("candiedcloudberries", "Candied Cloudberries", "BoarJerky", "ee_food_plains",
            "Cloudberries glazed in honey. A sweet handful of everything.",
            25f, 25f, 25f, 900f, 1f, "BloodPudding", 2, new GearCost("Cloudberry", 4, 0), new GearCost("Honey", 1, 0));

        public static readonly FoodPiece LoxMarrowBroth = Food("loxmarrowbroth", "Lox Marrow Broth", "CarrotSoup", "ee_food_plains",
            "A creamy broth boiled from a lox bone, with barley and cloudberries.",
            25f, 12f, 70f, 1500f, 3f, "BloodPudding", 1, new GearCost("LoxMeat", 1, 0), new GearCost("Cloudberry", 2, 0), new GearCost("BarleyFlour", 3, 0));

        public static readonly FoodPiece SunbarleyPorridge = Food("sunbarleyporridge", "Sunbarley Porridge", "TurnipStew", "ee_food_plains",
            "Golden barley porridge with cloudberries and honey. A seer's breakfast under the open sky.",
            25f, 12f, 75f, 1800f, 3f, "BloodPudding", 1, new GearCost("BarleyFlour", 4, 0), new GearCost("Cloudberry", 3, 0), new GearCost("Honey", 1, 0));

        public static readonly IReadOnlyList<FoodPiece> All = new[]
        {
            DandelionMash, CandiedThistle, GlowcapBroth, SeersPorridge, TurnipCrisps, BloodDraught, MireStew, GlacierTea,
            WolfheartStew, CandiedCloudberries, LoxMarrowBroth, SunbarleyPorridge,
        };

        private static FoodPiece Food(string key, string name, string dish, string bundle, string description, float health,
            float stamina, float eitr, float seconds, float regen, string beside, int makes, params GearCost[] cost) =>
            new FoodPiece
            {
                Prefab = "EE_Food_" + name.Replace(" ", ""), Base = dish, Bundle = bundle,
                Asset = "ee_food_" + key, Word = "ee_food_" + key, Name = name, Description = description, Health = health,
                Stamina = stamina, Eitr = eitr, Seconds = seconds, Regen = regen, RecipeBeside = beside, Makes = makes, Cost = cost,
            };
    }
}
