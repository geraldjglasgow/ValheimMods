using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Pickaxes' page in the info pane: the game's own damage and swing stamina at the player's level
    /// (Skills.GetRandomSkillRange, Attack.GetAttackStamina), then what the Pickaxes module adds (seams and clean strikes,
    /// splash, extra ore, wear, finds, experience) and its three milestones, each left out while its setting turns it off.
    /// The numbers are the synced settings scaled by the level exactly as the features scale them
    /// (<see cref="PickSkill.Share"/>, <see cref="PickSkill.Between"/>, <see cref="CleanStrikes.Multiplier"/>,
    /// <see cref="Splash.Amount"/>, <see cref="MineFinds.Chance"/>, <see cref="MineXp.BiomeStep"/>).
    /// </summary>
    public static class PickaxesPage
    {
        /// <summary>The game's cut in attack stamina at level 100: Attack.GetAttackStamina takes 33% times the skill factor off.</summary>
        private const float StaminaCut = 0.33f;

        public static void Write(SkillPage page)
        {
            page.About = "Mine faster and get more out of the rock. Trained by hitting rock.";
            GameLines(page);
            if (!PickSkill.Active)
            {
                page.Line("GrindstoneSkills extras are off on this server.");
                return;
            }
            SeamLines(page);
            SplashLine(page);
            PerkLines(page);
            FindsLine(page);
            ExperienceLine(page);
            Milestones(page);
        }

        /// <summary>Seams open at all: "0 with the next setting at 0 turns seams off".</summary>
        private static bool SeamsOn => SeamSettings.ChanceAt0.Value > 0f || SeamSettings.ChanceAt100.Value > 0f;

        private static void GameLines(SkillPage page)
        {
            page.Player.GetSkills().GetRandomSkillRange(out float min, out float max, page.Type);
            page.Line($"Mining damage {SkillPage.Range(min, max)}", "Mining damage",
                "The game's own: each pickaxe hit deals this share of the pickaxe's damage, rolled per hit (85–100% at level 100).");
            page.Line($"Swing stamina -{SkillPage.Percent(StaminaCut * page.Factor)}", "Swing stamina",
                $"The game's own, for every pickaxe swing: -{SkillPage.Percent(StaminaCut)} at level 100.");
        }

        private static void SeamLines(SkillPage page)
        {
            if (!SeamsOn)
                return;
            float level = page.Level;
            float chance = PickSkill.Between(SeamSettings.ChanceAt0.Value, SeamSettings.ChanceAt100.Value, level) / 100f;
            float window = Mathf.Max(0.1f, PickSkill.Between(SeamSettings.WindowAt0.Value, SeamSettings.WindowAt100.Value, level));
            page.Line($"Seams {SkillPage.Percent(chance)}, open {SkillPage.Duration(window)}", "Seams",
                "Chance a swing on a rock of many chunks makes one chunk within reach glow, for you alone, for that long. Hit it in time for a clean strike.");
            page.Line($"Clean strike ×{SkillPage.Number(CleanStrikes.Multiplier(1, level))} damage", "Clean strike", CleanStrikeTip());
        }

        private static string CleanStrikeTip()
        {
            float experience = Mathf.Max(0f, PickaxeExperienceSettings.CleanStrike.Value) * MineXp.Multiplier;
            string earns = experience > 0f ? $", +{SkillPage.Number(experience)} experience (more in later biomes and on ore)" : "";
            return $"A hit on the glowing chunk: that damage, one more drop roll when an ore chunk breaks{earns}, and the next seam opens at once. A hit elsewhere on the rock ends the chain.";
        }

        private static void SplashLine(SkillPage page)
        {
            float at100 = Mathf.Max(0f, PickaxePerkSettings.SplashDamage.Value);
            if (at100 <= 0f)
                return;
            float amount = Splash.Amount(new Miner(0L, page.Level, ZDOID.None));
            page.Line($"Splash {SkillPage.Number(amount)} damage", "Splash",
                $"Each swing's first hit on a chunk also deals this much, shared by the chunks touching it. It grows every 10 levels, to {SkillPage.Number(at100)} at level 100.");
        }

        private static void PerkLines(SkillPage page)
        {
            float level = page.Level;
            float ore = PickaxePerkSettings.ExtraOreChance.Value;
            if (ore > 0f)
                page.Line($"Extra ore {Share(ore, level)}", "Extra ore",
                    $"Chance each chunk of an ore deposit you break drops its loot once more. {Share(ore, PickSkill.MaxLevel)} at level 100.");
            float wear = PickaxePerkSettings.WearReduction.Value;
            if (wear > 0f)
                page.Line($"Pickaxe wear -{Share(wear, level)} on rock", "Pickaxe wear",
                    $"Less durability lost by swings that hit rock: -{Share(wear, PickSkill.MaxLevel)} at level 100.");
        }

        private static void FindsLine(SkillPage page)
        {
            if (MineFindSettings.ChanceAt0.Value <= 0f && MineFindSettings.ChanceAt100.Value <= 0f)
                return;
            page.Line($"Finds {SkillPage.Percent(MineFinds.Chance(page.Level))} per chunk", "Finds",
                $"Chance a chunk you break hides a valuable, such as amber or a ruby; chunks under 50 health get a share (a mud pile's a tenth). {SkillPage.Percent(MineFinds.Chance(PickSkill.MaxLevel))} at level 100.");
        }

        private static void ExperienceLine(SkillPage page)
        {
            float step = Mathf.Max(0f, PickaxeExperienceSettings.BiomeStep.Value);
            float ore = 1f + Mathf.Max(0f, PickaxeExperienceSettings.OreBonus.Value) / 100f;
            page.Line($"Experience +{SkillPage.Number(step)}% per biome, ×{SkillPage.Number(ore)} on ore", "Experience", ExperienceTip(step));
        }

        private static string ExperienceTip(float step)
        {
            string forest = SkillPage.Number(1f + step / 100f * MineXp.BiomeStep(Heightmap.Biome.BlackForest));
            string ashlands = SkillPage.Number(1f + step / 100f * MineXp.BiomeStep(Heightmap.Biome.AshLands));
            string tip = $"Swings that hit rock teach more in later biomes: ×{forest} in the Black Forest up to ×{ashlands} in the Ashlands.";
            float discovery = Mathf.Max(0f, PickaxeExperienceSettings.Discovery.Value);
            if (discovery > 0f)
                tip += $" The first hit on each kind of ore deposit gives +{SkillPage.Number(discovery)}, times the same scale.";
            if (MineXp.Multiplier != 1f)
                tip += $" All of it ×{SkillPage.Number(MineXp.Multiplier)} on this server.";
            return tip;
        }

        private static void Milestones(SkillPage page)
        {
            page.Perk("Read the rock", VeinSettings.ReadTheRockLevel.Value, ReadTheRockTip());
            page.Perk("Echo", VeinSettings.EchoLevel.Value,
                $"A swing that hits rock pings the nearest ore deposit within {SkillPage.Number(VeinSettings.EchoRadius.Value)} m: a sound from its direction, its name and distance. At most every {SkillPage.Duration(VeinSettings.EchoCooldown.Value)}; deposits the Wishbone finds are left to it.");
            if (SeamsOn)
                page.Perk("Unbroken", SeamSettings.UnbrokenLevel.Value, UnbrokenTip());
        }

        private static string ReadTheRockTip()
        {
            string stars = $"1 star {Setting(VeinSettings.Chance1.Value)}, 2 stars {Setting(VeinSettings.Chance2.Value)}, 3 stars {Setting(VeinSettings.Chance3.Value)}";
            return $"Hovering an ore deposit shows its vein stars and the chunks left. Rich veins: {stars} of deposits; each star gives +{Setting(VeinSettings.BonusPerStar.Value)} drop rolls to whoever mines it.";
        }

        private static string UnbrokenTip()
        {
            float level = SeamSettings.UnbrokenLevel.Value;
            float first = CleanStrikes.Multiplier(1, level);
            float second = CleanStrikes.Multiplier(2, level);
            float top = CleanStrikes.Multiplier(Mathf.Max(0, SeamSettings.UnbrokenMaxLinks.Value) + 1, level);
            return $"Clean strikes in a row hit harder: ×{SkillPage.Number(second)} for the second, +{SkillPage.Number(second - first)} for each after, up to ×{SkillPage.Number(top)}.";
        }

        /// <summary>A perk's share at this level as a percent, held to 0..1 as the perks hold it.</summary>
        private static string Share(float percentAt100, float level) => SkillPage.Percent(Mathf.Clamp01(PickSkill.Share(percentAt100, level)));

        /// <summary>A percent setting as it reads: 25 is "25%".</summary>
        private static string Setting(float percent) => SkillPage.Percent(Mathf.Max(0f, percent) / 100f);
    }
}
