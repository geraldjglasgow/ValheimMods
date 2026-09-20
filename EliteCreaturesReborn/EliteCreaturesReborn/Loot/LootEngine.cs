using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using UnityEngine;

namespace EliteCreaturesReborn.Loot
{
    /// <summary>
    /// Reworks a kill's drop list by the loot rules (`loot.md`): the mode decides quantities, the creature's own
    /// `creatures:` rules override and extend them, and the global (or boss) multiplier scales the result. Runs
    /// inside the GenerateDropList postfix, on the dying creature's owner, so every roll here happens exactly once
    /// and every player sees the one pile the game replicates. Trophies step outside all of it unless the trophy
    /// switch says otherwise - except a row the file names explicitly, which is the server's own words and honoured.
    /// The engine only touches rows it owns: the creature's own table and rows the rule file names. A row another
    /// mod injected into the same list (EpicLoot's materials, say) passes through untouched by mode, strip and
    /// multiplier alike, whatever order Harmony happens to run the postfixes in.
    /// </summary>
    internal static class LootEngine
    {
        private sealed class Context
        {
            public LootRules Loot = null!;
            public CreatureLootRule? Rule;
            public int Stars;
            public bool IsBoss;
            public float DropsMultiplier;
            public int ExtraRolls;
            public readonly HashSet<string> Overridden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>The prefabs this engine may touch: the creature's own table plus rows the file names.
            /// Anything else in the list was put there by another mod and passes through untouched.</summary>
            public readonly HashSet<GameObject> Ours = new HashSet<GameObject>();

            /// <summary>True when trophies follow the mode; false keeps them exactly as the game rolled them.</summary>
            public bool TrophiesFollow;
        }

        public static void Rework(CharacterDrop drop, EliteController controller, List<KeyValuePair<GameObject, int>> result)
        {
            LootRules loot = RuleState.Active.Loot;
            if (loot.Mode == LootMode.Vanilla)
            {
                return;
            }
            Context ctx = Build(loot, drop, controller);
            StripOverridden(ctx, result);
            if (loot.Mode == LootMode.Curated)
            {
                StripBase(ctx, result);
            }
            else
            {
                ApplyMode(ctx, drop, result);
                RollOverrides(ctx, result);
            }
            RollExtras(ctx, result);
            Multiply(ctx, result);
        }

        private static Context Build(LootRules loot, CharacterDrop drop, EliteController controller)
        {
            Context ctx = new Context
            {
                Loot = loot,
                Rule = FindRule(controller),
                Stars = controller.Traits.Stars,
                IsBoss = controller.Creature != null && controller.Creature.IsBoss(),
            };
            ctx.TrophiesFollow = ctx.Rule?.MultiplyTrophies ?? loot.MultiplyTrophies;
            ctx.DropsMultiplier = ctx.Rule?.Drops != null
                ? LineAt(ctx.Rule.Drops, ctx.Stars) : LiveDropsLine(controller, ctx.IsBoss, ctx.Stars);
            ctx.ExtraRolls = CountExtraRolls(loot, ctx.Stars);
            if (ctx.Rule != null)
            {
                foreach (DropRule row in ctx.Rule.Overrides)
                {
                    ctx.Overridden.Add(row.Item);
                }
            }
            ClaimTable(ctx, drop);
            return ctx;
        }

        /// <summary>The creature's own table rows are ours to rework; rows the file names join in RollRow.</summary>
        private static void ClaimTable(Context ctx, CharacterDrop drop)
        {
            foreach (CharacterDrop.Drop row in drop.m_drops)
            {
                if (row.m_prefab != null)
                {
                    ctx.Ours.Add(row.m_prefab);
                }
            }
        }

        /// <summary>
        /// The star `drops` line from the rules active right now, not the snapshot the creature resolved with. Loot
        /// is applied at death, so a rule-file edit re-tunes what an already-spawned creature pays - the same hot
        /// reload every other loot setting gets by reading RuleState at death. Falls back to the resolve-time
        /// snapshot only when the ZDO is already gone.
        /// </summary>
        private static float LiveDropsLine(EliteController controller, bool isBoss, int stars)
        {
            if (isBoss)
            {
                return RuleState.Active.Boss.Star.DropsAt(stars);
            }
            ZDO? zdo = controller.View != null && controller.View.IsValid() ? controller.View.GetZDO() : null;
            if (zdo == null)
            {
                return controller.Rules.Star.DropsAt(stars);
            }
            return RuleState.Active.For(Traits.TraitStore.GetBiome(zdo)).Star.DropsAt(stars);
        }

        private static CreatureLootRule? FindRule(EliteController controller)
        {
            string name = Utils.GetPrefabName(controller.gameObject);
            return RuleState.Active.CreatureLoot.TryGetValue(name, out CreatureLootRule rule) ? rule : null;
        }

        /// <summary>One gate per star, in order, so `max extra rolls` caps the successes and not the attempts.</summary>
        private static int CountExtraRolls(LootRules loot, int stars)
        {
            int rolls = 0;
            for (int star = 1; star <= stars; star++)
            {
                if (loot.MaxExtraRolls > 0 && rolls >= loot.MaxExtraRolls)
                {
                    break;
                }
                if (UnityEngine.Random.value * 100f <= loot.ExtraRollChanceAt(star))
                {
                    rolls++;
                }
            }
            return rolls;
        }

        /// <summary>Rows the file overrides lose their game-rolled contribution; the file's version replaces it.</summary>
        private static void StripOverridden(Context ctx, List<KeyValuePair<GameObject, int>> result)
        {
            if (ctx.Overridden.Count > 0)
            {
                result.RemoveAll(pair => ctx.Overridden.Contains(pair.Key.name));
            }
        }

        /// <summary>Curated: the creature's own table is ignored entirely; protected trophies survive, and so
        /// does anything another mod put in the list - Curated replaces our table, not theirs.</summary>
        private static void StripBase(Context ctx, List<KeyValuePair<GameObject, int>> result)
        {
            result.RemoveAll(pair => ctx.Ours.Contains(pair.Key)
                && (ctx.TrophiesFollow || !DropRoller.IsTrophy(pair.Key)));
        }

        private static void ApplyMode(Context ctx, CharacterDrop drop, List<KeyValuePair<GameObject, int>> result)
        {
            if (ctx.Loot.Mode == LootMode.Scaled)
            {
                ScaleRows(ctx, result);
            }
            else if (ctx.Loot.Mode == LootMode.Rolled)
            {
                RerollTable(ctx, drop, result);
            }
        }

        private static void ScaleRows(Context ctx, List<KeyValuePair<GameObject, int>> result)
        {
            if (Mathf.Approximately(ctx.DropsMultiplier, 1f))
            {
                return;
            }
            for (int i = 0; i < result.Count; i++)
            {
                if (ctx.Ours.Contains(result[i].Key) && (ctx.TrophiesFollow || !DropRoller.IsTrophy(result[i].Key)))
                {
                    result[i] = new KeyValuePair<GameObject, int>(
                        result[i].Key, DropRoller.Scaled(result[i].Value, ctx.DropsMultiplier));
                }
            }
        }

        /// <summary>Rolled: the whole table again per earned extra roll, each row independent, exactly like the
        /// game's own base roll - which is what makes a rare drop's chance genuinely repeat.</summary>
        private static void RerollTable(Context ctx, CharacterDrop drop, List<KeyValuePair<GameObject, int>> result)
        {
            for (int roll = 0; roll < ctx.ExtraRolls; roll++)
            {
                foreach (CharacterDrop.Drop row in drop.m_drops)
                {
                    if (row.m_prefab == null || ctx.Overridden.Contains(row.m_prefab.name)
                        || (!ctx.TrophiesFollow && DropRoller.IsTrophy(row.m_prefab)))
                    {
                        continue;
                    }
                    DropRoller.Add(result, row.m_prefab, DropRoller.RollGameRow(row));
                }
            }
        }

        private static void RollOverrides(Context ctx, List<KeyValuePair<GameObject, int>> result)
        {
            if (ctx.Rule == null)
            {
                return;
            }
            foreach (DropRule row in ctx.Rule.Overrides)
            {
                if (!row.Remove)
                {
                    RollRow(ctx, row, followsMode: true, result);
                }
            }
        }

        private static void RollExtras(Context ctx, List<KeyValuePair<GameObject, int>> result)
        {
            if (ctx.Rule == null)
            {
                return;
            }
            foreach (DropRule row in ctx.Rule.Extras)
            {
                RollRow(ctx, row, row.PerStar, result);
            }
        }

        /// <summary>A file row: one base roll, plus the mode's treatment when it follows the mode - extra rolls
        /// under Rolled and Curated, the quantity line under Scaled.</summary>
        private static void RollRow(Context ctx, DropRule row, bool followsMode, List<KeyValuePair<GameObject, int>> result)
        {
            GameObject? prefab = DropRoller.FindPrefab(row.Item);
            if (prefab == null)
            {
                return;
            }
            ctx.Ours.Add(prefab);
            int amount = DropRoller.RollRuleRow(row);
            if (followsMode && ctx.Loot.Mode != LootMode.Scaled)
            {
                for (int roll = 0; roll < ctx.ExtraRolls; roll++)
                {
                    amount += DropRoller.RollRuleRow(row);
                }
            }
            else if (followsMode && amount > 0)
            {
                amount = DropRoller.Scaled(amount, ctx.DropsMultiplier);
            }
            DropRoller.Add(result, prefab, amount);
        }

        /// <summary>The world-wide multiplier, after everything else; a boss takes the boss factor on top.</summary>
        private static void Multiply(Context ctx, List<KeyValuePair<GameObject, int>> result)
        {
            float factor = ctx.Loot.GlobalMultiplier * (ctx.IsBoss ? ctx.Loot.BossMultiplier : 1f);
            if (Mathf.Approximately(factor, 1f))
            {
                return;
            }
            for (int i = 0; i < result.Count; i++)
            {
                if (ctx.Ours.Contains(result[i].Key) && (ctx.TrophiesFollow || !DropRoller.IsTrophy(result[i].Key)))
                {
                    result[i] = new KeyValuePair<GameObject, int>(
                        result[i].Key, DropRoller.Scaled(result[i].Value, factor));
                }
            }
        }

        private static float LineAt(float[] line, int stars)
        {
            if (line.Length == 0)
            {
                return 1f;
            }
            return line[Mathf.Clamp(stars, 0, line.Length - 1)];
        }
    }
}
