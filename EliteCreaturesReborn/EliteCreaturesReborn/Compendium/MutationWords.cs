using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// What each mutation does, in a player's words, with the numbers of the rules it is read from - the compendium's
    /// counterpart of <c>MutationReport</c>, which writes the same numbers for an admin. Read for a creature with no stars,
    /// so a value that grows with stars says so in words rather than in a number the player will rarely meet.
    /// </summary>
    internal static class MutationWords
    {
        public static string Describe(Mutation m, BiomeRules r, CreatureTraits t) => m switch
        {
            Mutation.Mad => $"Moves {Pct(r, m, Fields.Move)} faster and attacks {Pct(r, m, Fields.AttackSpeed)} faster, "
                + $"with {Pct(r, m, Fields.Health)} less health. Never on bats.",
            Mutation.Bloated => $"{N(r, m, Fields.Health)} times its usual health. {S(r, m, Fields.Delay)} after it dies it "
                + $"explodes: {N(r, m, Fields.Damage)} damage within {N(r, m, Fields.Radius)} m, more for each star.",
            Mutation.Cloaked => $"Invisible until you are within {N(r, m, Fields.RevealDistance)} m. Drakes, bats and "
                + "deathsquitos are never Cloaked.",
            Mutation.Splintering => "Splits into weaker copies when killed, each dealing "
                + $"{r.PowerOf(m, Fields.Damage) * 100f:0}% of its damage, and they can split again.",
            Mutation.Leeching => $"Regenerates {N(r, m, Fields.Regen)}% of its health a second (at most "
                + $"{N(r, m, Fields.RegenCap)} a second) once {S(r, m, Fields.CombatCooldown)} pass without a hit, "
                + $"and heals {N(r, m, Fields.Lifesteal)}% of the damage it deals.",
            Mutation.Warding => $"Reflects {N(r, m, Fields.Reflect)}% of each hit back at you and knocks you back, never "
                + $"more than {N(r, m, Fields.MaxReflect)}% of your maximum health in any second.",
            Mutation.Plated => $"Shrugs off {Mathf.Min(r.PowerOf(m, Fields.Armour), r.PowerOf(m, Fields.MaxReduction)):0}% "
                + "of the damage it takes at full health, less as it is hurt, and hits up to "
                + $"{N(r, m, Fields.Damage)}% harder as that armour goes.",
            Mutation.Miasmic => $"Leaves poison clouds behind it, {N(r, m, Fields.CloudRadius)} m wide, each lasting "
                + $"{S(r, m, Fields.CloudLife)}. They poison players, never creatures.",
            Mutation.Devouring => Devouring(r),
            _ => Middle(m, r, t),
        };

        private static string Middle(Mutation m, BiomeRules r, CreatureTraits t) => m switch
        {
            Mutation.Thieving => "Steals an item with each melee hit that lands, up to one per star (at least "
                + $"{N(r, m, Fields.MaxItems)}), and carries them on its nameplate. Kill it to get everything back.",
            Mutation.Gilded => Gilded(r),
            Mutation.Blinking => Blinking(r),
            Mutation.Relentless => "Once it picks you it keeps coming, seen or not, until you are "
                + $"{N(r, m, Fields.ChaseDistance)} m away; sneaking does not hide you. Never faster than its usual speed, "
                + "never on large creatures.",
            Mutation.Juggernaut => "Never staggers or is knocked back: hits, parries, blasts and traps don't stop it. "
                + "\"Unstoppable\" shows over it instead.",
            Mutation.Screecher => $"When one hit takes {N(r, m, Fields.Threshold)}% of its health it shrieks: players "
                + $"within {N(r, m, Fields.Radius)} m are deafened for {S(r, m, Fields.MuteTime)}, and Elemental "
                + $"and Blood Magic weapons will not cast. At most once every {S(r, m, Fields.Cooldown)}; a "
                + "killing blow makes no shriek.",
            Mutation.Frostbound => Frostbound(r, t),
            _ => Last(m, r, t),
        };

        private static string Last(Mutation m, BiomeRules r, CreatureTraits t) => m switch
        {
            Mutation.Mudbound => Mud(r, t),
            Mutation.Corrodent => $"Your armour loses durability {N(r, m, Fields.Durability)} times as fast under its "
                + "hits. Shields wear as usual.",
            Mutation.Cloning => Cloning(r),
            Mutation.Piercing => $"Its hits ignore {Piercing.Share(r, t) * 100f:0}% of your armour.",
            Mutation.Howling => $"When it turns on you it howls, and the {HowlBehaviour.MaxCalled} nearest creatures on "
                + $"its side within {HowlBehaviour.Reach:0} m come for you; again every "
                + $"{PageText.Seconds(HowlBehaviour.Cooldown)} while it fights you. Never calls a boss.",
            Mutation.Binding => Roots(r, t),
            Mutation.Flamebound => Fire(r, t),
            _ => "",
        };

        private static string Devouring(BiomeRules r)
        {
            Mutation m = Mutation.Devouring;
            return $"Kills smaller creatures in one bite and keeps {N(r, m, Fields.AbsorbHealth)}% of their health and "
                + $"{N(r, m, Fields.AbsorbDamage)}% of their damage, slowing as it grows: one meal per star in its life (at "
                + $"least {N(r, m, Fields.MinMeals)}), one every {S(r, m, Fields.DevourCooldown)}, never a boss or a "
                + "large creature. Once it is big enough it hunts players too. Its nameplate shows what it ate.";
        }

        private static string Gilded(BiomeRules r)
        {
            Mutation m = Mutation.Gilded;
            return $"Glitters gold, never attacks a player and runs from any within {N(r, m, Fields.FleeDistance)} m. "
                + $"Drops {N(r, m, Fields.Loot)} times its loot plus {N(r, m, Fields.BonusAmount)} "
                + $"{ItemName(r.PrefabOf(m, Fields.BonusItem))}, more for each star. Never on large creatures.";
        }

        private static string Blinking(BiomeRules r)
        {
            Mutation m = Mutation.Blinking;
            string health = $"{Pct(r, m, Fields.Health)} less health.";
            return r.PowerOf(m, Fields.Every) <= 0f
                ? $"{health} It never blinks on this server."
                : $"Every {S(r, m, Fields.Every)} of a fight it reappears {N(r, m, Fields.Distance)} m behind its "
                    + $"target, after a flash and a chime at the spot. {health}";
        }

        private static string Frostbound(BiomeRules r, CreatureTraits t)
        {
            Mutation m = Mutation.Frostbound;
            TrailSpec ice = TrailSpec.Of(r, t, TrailKind.Frost);
            string trail = ice.Lays
                ? $"Leaves slick ice where it walks: on it you slide and move {ice.Slow * 100f:0}% slower; each patch lasts "
                    + $"{PageText.Seconds(ice.Life)}."
                : "It lays no ice on this server.";
            return $"Players within {N(r, m, Fields.AuraRadius)} m recover stamina "
                + $"{Mathf.Min(r.PowerOf(m, Fields.StaminaRegen), 100f):0}% slower (\"Chilled\"). {trail} Frost heals it "
                + "instead of hurting it.";
        }

        private static string Mud(BiomeRules r, CreatureTraits t)
        {
            TrailSpec mud = TrailSpec.Of(r, t, TrailKind.Mud);
            return mud.Lays
                ? $"Leaves thick mud where it walks: in it you move {mud.Slow * 100f:0}% slower, and for "
                    + $"{PageText.Seconds(TrailKind.Mud.Linger)} after stepping out. Each patch lasts "
                    + $"{PageText.Seconds(mud.Life)}."
                : "It lays no mud on this server.";
        }

        private static string Cloning(BiomeRules r)
        {
            Mutation m = Mutation.Cloning;
            float times = Mathf.Round(r.PowerOf(m, Fields.Times));
            string late = r.PowerOf(m, Fields.DecoyLife) > 0f
                ? $", and after {S(r, m, Fields.DecoyLife)} it shows itself anyway" : "";
            return $"When it squares up to you within {N(r, m, Fields.Range)} m it leaves a harmless decoy in its place "
                + "and fights on unseen. Its first blow that lands shows it, so does killing the decoy" + late + ". "
                + (times == 1f ? "Once in its life." : $"{times:0} times in its life.");
        }

        private static string Roots(BiomeRules r, CreatureTraits t)
        {
            TrailSpec roots = TrailSpec.Of(r, t, TrailKind.Roots);
            return $"Leaves patches of tangled roots where it walks, one every {roots.Spacing:0.#} m, each lasting "
                + $"{PageText.Seconds(roots.Life)}. Step into one and the roots hold you for "
                + $"{PageText.Seconds(roots.Strength)} (\"Rooted\": you can still turn, block and strike), then let you go "
                + $"for {PageText.Seconds(RootFooting.Free)}.";
        }

        private static string Fire(BiomeRules r, CreatureTraits t)
        {
            TrailSpec fire = TrailSpec.Of(r, t, TrailKind.Fire);
            return $"Leaves burning ground where it walks, each patch lasting {PageText.Seconds(fire.Life)}. Standing in "
                + $"it sets you on fire: {fire.Strength:0.#} fire damage a second, more with stars. Fire resistance, armour "
                + "and being wet help.";
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
