using EliteCrafting.Affixes;
using EliteCrafting.Rolling;

namespace EliteCrafting.Commands
{
    /// <summary>
    /// <c>ecraft reroll [cursor|hover|&lt;slot&gt;]</c> (console-commands.md section 3): rerolls every affix of an item in
    /// the caller's own inventory, keeping its rarity. A test tool: it ignores seals, costs, <c>Modify equipped
    /// items</c> and the rarity's affix count, but never writes an item of a newer format.
    /// <para>
    /// Spec gap, decided here: "ignores the rarity's affix count" is read as <b>keep the item's affix count</b>. Every
    /// unbound affix (dormant ones included) is removed and the same number is drawn again with
    /// <see cref="ItemRoller.AddAffixes"/>; the bound affix and unreadable segments stay. An item with no rerollable
    /// affix is rolled fresh at its rarity instead (<see cref="ItemRoller.RollFresh"/>).
    /// </para>
    /// </summary>
    internal static class RerollCommand
    {
        public const string Grammar = "ecraft reroll [" + ItemTargets.InventoryWords + "]";

        public static void Run(CommandCall call)
        {
            string word = call.Lower(0);
            if (word.Length > 0 && !ItemTargets.IsWord(word, allowGround: false))
            {
                call.Fail($"unknown target '{word}'.", Grammar);
                return;
            }
            ItemDrop.ItemData? item = ItemTargets.OwnMagicBase(call, word, Grammar);
            ItemState state = ItemState.Read(item);
            if (item == null || !Rerollable(call, state))
            {
                return;
            }
            ItemState stripped = WithoutUnbound(state, out int count);
            RollContext context = RollContext.For(item);
            context.Random = RollRandom.Create();
            ItemState? rolled = count > 0
                ? RollerCall.Roll(call, () => ItemRoller.AddAffixes(stripped, count, context))
                : RollerCall.Roll(call, () => ItemRoller.RollFresh(state, state.Rarity!, context));
            if (rolled != null && RollerCall.Commit(call, item, rolled))
            {
                ItemReport.Write(call, item);
            }
        }

        private static bool Rerollable(CommandCall call, ItemState state)
        {
            string? problem = !state.IsMagic ? "the item is Common: it has no rarity to keep."
                : state.Rarity == null ? $"the item's rarity '{state.RarityId}' is not in the configuration."
                : null;
            if (problem != null)
            {
                call.Reply(problem);
            }
            return problem == null && !ItemTargets.IsNewer(call, state);
        }

        private static ItemState WithoutUnbound(ItemState state, out int removed)
        {
            ItemStateBuilder builder = state.ToBuilder();
            removed = 0;
            for (int i = 0; i < state.AffixCount; i++)
            {
                if (!state.IsBoundAt(i) && builder.RemoveAffix(state.Affixes[i].Id))
                {
                    removed++;
                }
            }
            return builder.Build();
        }
    }
}
