using System;
using EliteCrafting.Affixes;
using EliteCrafting.Epic;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// The runes on Epic Loot items (the user's decision 2026-10-03: with Epic Loot installed the runes work its magic,
    /// not ours). Promote: Awakening rolls Epic Loot's own Magic item; Ascension makes Magic Rare, keeps every effect and
    /// adds up to Epic Loot's Rare minimum (at least <c>promote_adds_at_least</c>, never past its maximum), renamed as
    /// Epic Loot names it. Add: Shaping and Consecrated add one effect up to Epic Loot's maximum for the rarity. Strip:
    /// the Cleansing Rune does not work on Epic Loot items. Corrupt: <see cref="EpicSerpent"/>. Every roll is a dry run
    /// on a copy; <see cref="StoneCommit"/> writes it.
    /// </summary>
    internal static class EpicVerbs
    {
        public static StoneResult Run(StoneJob job)
        {
            switch (job.Def!.Verb)
            {
                case StoneVerb.Promote:
                    return Promote(job);
                case StoneVerb.Add:
                    return Add(job);
                case StoneVerb.Corrupt:
                    return EpicSerpent.Run(job);
                default:
                    return StoneResult.Refuse("epic_no_strip", job.StoneName);
            }
        }

        public static StoneResult WrongRarity(StoneJob job) =>
            StoneResult.Refuse("wrong_rarity", job.StoneName, StoneNames.Rarity(job.Rarity!));

        private static StoneResult Promote(StoneJob job)
        {
            RarityDef? next = job.Rules.Economy.Next(job.Rarity!);
            if (next == null)
            {
                return WrongRarity(job);
            }
            EpicItem? made = job.Epic == null ? Awaken(job, next) : Ascend(job, next);
            if (made == null)
            {
                return StoneResult.Refuse("epic_no_effect");
            }
            return StoneResult.Epic(made.Json, null, "promoted", job.ItemName, StoneNames.Rarity(next));
        }

        // A plain item: Epic Loot's own roll of the new rarity (its effect count, sockets and name).
        private static EpicItem? Awaken(StoneJob job, RarityDef next)
        {
            EpicItem? made = EpicItem.Parse(EpicApi.RollJson(EpicRarity.EpicIndex(next), job.Target));
            return made != null && made.EffectCount > 0 ? made : null;
        }

        private static EpicItem? Ascend(StoneJob job, RarityDef next)
        {
            EpicItem item = job.Epic!.Copy();
            item.Rarity = EpicRarity.EpicIndex(next);
            (int min, int max) = EpicExtras.Counts(item.Rarity);
            int wanted = Math.Min(Math.Max(min - item.EffectCount, job.Rules.Economy.Rolling.PromoteAddsAtLeast), max - item.EffectCount);
            if (wanted > 0 && EpicEffects.Add(job.Target, item, wanted, RollRandom.Create()) == 0)
            {
                return null;
            }
            EpicExtras.Rename(job.Target, item);
            return item;
        }

        private static StoneResult Add(StoneJob job)
        {
            if (job.Epic == null)
            {
                return WrongRarity(job);
            }
            if (job.Epic.EffectCount >= EpicExtras.Counts(job.Epic.Rarity).Max)
            {
                return StoneResult.Refuse("epic_full");
            }
            EpicItem item = job.Epic.Copy();
            if (EpicEffects.Add(job.Target, item, 1, RollRandom.Create()) == 0)
            {
                return StoneResult.Refuse("epic_no_effect");
            }
            return StoneResult.Epic(item.Json, null, "affix_added", job.ItemName, EpicEffects.LastText(item));
        }
    }

    /// <summary>
    /// The Serpent Rune on an Epic Loot item: one outcome drawn from the rune's table, then sealed for good (our own
    /// <c>ecf_sealed</c>, which our runes honour; Epic Loot's enchanting table does not read it). Seal only; one effect
    /// past Epic Loot's maximum by the rune's <c>overflow</c>; or a chaotic reroll, every effect replaced by a fresh Epic
    /// Loot roll of the same rarity, sockets kept. An outcome that cannot be carried out seals only.
    /// </summary>
    internal static class EpicSerpent
    {
        public static StoneResult Run(StoneJob job)
        {
            if (job.Epic == null)
            {
                return EpicVerbs.WrongRarity(job);
            }
            Random random = RollRandom.Create();
            CorruptOutcome outcome = CorruptVerb.Draw(job.Def!.Outcomes, new RollContext { Random = random });
            StoneResult? result = outcome == CorruptOutcome.AddInscription ? AddPast(job, job.Epic, random)
                : outcome == CorruptOutcome.ChaoticReroll ? Reroll(job, job.Epic) : null;
            return result ?? StoneResult.Epic(null, Sealed(job), "corrupt_seal", job.ItemName);
        }

        private static StoneResult? AddPast(StoneJob job, EpicItem epic, Random random)
        {
            if (epic.EffectCount >= EpicExtras.Counts(epic.Rarity).Max + job.Def!.Overflow)
            {
                return null;
            }
            EpicItem item = epic.Copy();
            if (EpicEffects.Add(job.Target, item, 1, random) == 0)
            {
                return null;
            }
            return StoneResult.Epic(item.Json, Sealed(job), "corrupt_add", job.ItemName, EpicEffects.LastText(item));
        }

        private static StoneResult? Reroll(StoneJob job, EpicItem epic)
        {
            EpicItem? fresh = EpicItem.Parse(EpicApi.RollJson(epic.Rarity, job.Target));
            if (fresh == null || fresh.EffectCount == 0)
            {
                return null;
            }
            EpicItem item = epic.Copy();
            item.TakeEffects(fresh);
            EpicExtras.Rename(job.Target, item);
            return StoneResult.Epic(item.Json, Sealed(job), "epic_corrupt_chaos", job.ItemName);
        }

        private static ItemState Sealed(StoneJob job) => job.State.ToBuilder().Seal(ItemKeys.SealedSerpent).Build();
    }
}
