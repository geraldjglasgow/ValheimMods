using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// What each mutation does, in a player's words, with the numbers of the rules it is read from - the compendium's
    /// counterpart of <c>MutationReport</c>, which writes the same numbers for an admin. Read for a creature with no stars,
    /// so a value that grows with stars says so in words rather than in a number the player will rarely meet. The words
    /// are <c>ecr_book_mut_&lt;mutation&gt;</c> (and a few variants) in the player's language, the numbers their values.
    /// </summary>
    internal static class MutationWords
    {
        public static string Describe(Mutation m, BiomeRules r, CreatureTraits t) => m switch
        {
            Mutation.Mad => Says(m, Pct(r, m, Fields.Move), Pct(r, m, Fields.AttackSpeed), Pct(r, m, Fields.Health)),
            Mutation.Bloated => Says(m, N(r, m, Fields.Health), S(r, m, Fields.Delay), N(r, m, Fields.Damage),
                N(r, m, Fields.Radius)),
            Mutation.Cloaked => Says(m, N(r, m, Fields.RevealDistance), MutationsPage.Name(m)),
            Mutation.Splintering => Says(m, (r.PowerOf(m, Fields.Damage) * 100f).ToString("0"), MutationsPage.Name(m)),
            Mutation.Leeching => Says(m, N(r, m, Fields.Regen), N(r, m, Fields.RegenCap), S(r, m, Fields.CombatCooldown),
                N(r, m, Fields.Lifesteal)),
            Mutation.Warding => Says(m, N(r, m, Fields.Reflect), N(r, m, Fields.MaxReflect)),
            Mutation.Plated => Says(m, Mathf.Min(r.PowerOf(m, Fields.Armour), r.PowerOf(m, Fields.MaxReduction)).ToString("0"),
                N(r, m, Fields.Damage)),
            Mutation.Miasmic => Says(m, N(r, m, Fields.CloudRadius), S(r, m, Fields.CloudLife)),
            Mutation.Devouring => Says(m, N(r, m, Fields.AbsorbHealth), N(r, m, Fields.AbsorbDamage), N(r, m, Fields.MinMeals),
                S(r, m, Fields.DevourCooldown)),
            _ => Middle(m, r, t),
        };

        private static string Middle(Mutation m, BiomeRules r, CreatureTraits t) => m switch
        {
            Mutation.Thieving => Says(m, N(r, m, Fields.MaxItems)),
            Mutation.Gilded => Says(m, N(r, m, Fields.FleeDistance), N(r, m, Fields.Loot), N(r, m, Fields.BonusAmount),
                ItemName(r.PrefabOf(m, Fields.BonusItem))),
            Mutation.Blinking => Blinking(r),
            Mutation.Relentless => Says(m, N(r, m, Fields.ChaseDistance)),
            Mutation.Juggernaut => Says(m),
            Mutation.Screecher => Says(m, N(r, m, Fields.Threshold), N(r, m, Fields.Radius), S(r, m, Fields.MuteTime),
                S(r, m, Fields.Cooldown)),
            Mutation.Frostbound => Frostbound(r, t),
            _ => Last(m, r, t),
        };

        private static string Last(Mutation m, BiomeRules r, CreatureTraits t) => m switch
        {
            Mutation.Mudbound => Mud(r, t),
            Mutation.Corrodent => Says(m, N(r, m, Fields.Durability)),
            Mutation.Cloning => Cloning(r),
            Mutation.Piercing => Says(m, (Piercing.Share(r, t) * 100f).ToString("0")),
            Mutation.Howling => Says(m, HowlBehaviour.MaxCalled, HowlBehaviour.Reach.ToString("0"),
                PageText.Seconds(HowlBehaviour.Cooldown)),
            Mutation.Binding => Roots(r, t),
            Mutation.Flamebound => Fire(r, t),
            _ => "",
        };

        /// <summary>The mutation's own words, <c>ecr_book_mut_&lt;mutation&gt;</c>, with its values filled in.</summary>
        private static string Says(Mutation m, params object?[] values) =>
            Lang.Format("ecr_book_mut_" + m.ToString().ToLowerInvariant(), values);

        private static string Blinking(BiomeRules r)
        {
            Mutation m = Mutation.Blinking;
            string health = Pct(r, m, Fields.Health);
            return r.PowerOf(m, Fields.Every) <= 0f
                ? Lang.Format("ecr_book_mut_blinking_never", health)
                : Says(m, S(r, m, Fields.Every), N(r, m, Fields.Distance), health);
        }

        private static string Frostbound(BiomeRules r, CreatureTraits t)
        {
            Mutation m = Mutation.Frostbound;
            TrailSpec ice = TrailSpec.Of(r, t, TrailKind.Frost);
            string trail = ice.Lays
                ? Lang.Format("ecr_book_mut_frostbound_ice", (ice.Slow * 100f).ToString("0"), PageText.Seconds(ice.Life))
                : Lang.Get("ecr_book_mut_frostbound_no_ice");
            return Says(m, N(r, m, Fields.AuraRadius), Mathf.Min(r.PowerOf(m, Fields.StaminaRegen), 100f).ToString("0"), trail);
        }

        private static string Mud(BiomeRules r, CreatureTraits t)
        {
            TrailSpec mud = TrailSpec.Of(r, t, TrailKind.Mud);
            return mud.Lays
                ? Says(Mutation.Mudbound, (mud.Slow * 100f).ToString("0"), PageText.Seconds(TrailKind.Mud.Linger),
                    PageText.Seconds(mud.Life))
                : Lang.Get("ecr_book_mut_mudbound_none");
        }

        private static string Cloning(BiomeRules r)
        {
            Mutation m = Mutation.Cloning;
            float times = Mathf.Round(r.PowerOf(m, Fields.Times));
            string count = times == 1f ? Lang.Get("ecr_book_mut_cloning_once")
                : Lang.Format("ecr_book_mut_cloning_times", times.ToString("0"));
            return r.PowerOf(m, Fields.DecoyLife) > 0f
                ? Lang.Format("ecr_book_mut_cloning_late", N(r, m, Fields.Range), S(r, m, Fields.DecoyLife), count)
                : Says(m, N(r, m, Fields.Range), count);
        }

        private static string Roots(BiomeRules r, CreatureTraits t)
        {
            TrailSpec roots = TrailSpec.Of(r, t, TrailKind.Roots);
            return Says(Mutation.Binding, roots.Spacing.ToString("0.#"), PageText.Seconds(roots.Life),
                PageText.Seconds(roots.Strength), PageText.Seconds(RootFooting.Free));
        }

        private static string Fire(BiomeRules r, CreatureTraits t)
        {
            TrailSpec fire = TrailSpec.Of(r, t, TrailKind.Fire);
            return Says(Mutation.Flamebound, PageText.Seconds(fire.Life), fire.Strength.ToString("0.#"));
        }

        // An item's name in the player's language, lower case for the middle of a sentence; the prefab for an unknown item.
        private static string ItemName(string prefab)
        {
            GameObject? item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            ItemDrop? drop = item != null ? item.GetComponent<ItemDrop>() : null;
            return drop != null ? Localization.instance.Localize(drop.m_itemData.m_shared.m_name).ToLowerInvariant() : prefab;
        }

        private static string N(BiomeRules r, Mutation m, string field) => r.PowerOf(m, field).ToString("0.##");

        private static string Pct(BiomeRules r, Mutation m, string field) => PageText.Change(r.PowerOf(m, field));

        private static string S(BiomeRules r, Mutation m, string field) => PageText.Seconds(r.PowerOf(m, field));
    }
}
