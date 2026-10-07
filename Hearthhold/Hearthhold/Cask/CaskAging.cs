using System.Collections.Generic;

namespace Hearthhold
{
    /// <summary>
    /// The aging rule, run by the cask's owner on its inventory and its last records. Only meads and wines
    /// (<see cref="Kitchen.IsFermented"/>) that are star items age. A slot whose record matches (same item, same stars,
    /// stack not larger) keeps its clock; anything else (a new item, another item or stars, a topped up stack) starts a
    /// new clock now. Each full <see cref="Period"/> raises the whole stack one star, up to gold; time skipped by sleeping
    /// counts, and a cask that was not loaded catches up when it is.
    /// </summary>
    public static class CaskAging
    {
        /// <summary>In-game days per star.</summary>
        public const float DaysPerStar = 2f;

        /// <summary>Game seconds per star: two in-game days of this world's day length.</summary>
        public static double Period(EnvMan env) => DaysPerStar * (double)env.m_dayLengthSec;

        /// <summary>The new records for this inventory; <paramref name="raised"/> is true when any stack gained stars.</summary>
        public static List<CaskRecord> Age(Inventory inventory, List<CaskRecord> old, double now, double period, out bool raised)
        {
            raised = false;
            List<CaskRecord> records = new List<CaskRecord>();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (!Ages(item))
                    continue;
                CaskRecord record = Track(item, old, now);
                raised |= Raise(item, ref record, now, period);
                records.Add(record);
            }
            return records;
        }

        private static bool Ages(ItemDrop.ItemData item) =>
            item?.m_dropPrefab != null && Kitchen.IsFermented(item.m_dropPrefab.name) && Stars.IsStarItem(item);

        /// <summary>The slot's record, its clock kept when the old record still matches, else started now.</summary>
        private static CaskRecord Track(ItemDrop.ItemData item, List<CaskRecord> old, double now)
        {
            CaskRecord record = new CaskRecord
            {
                X = item.m_gridPos.x,
                Y = item.m_gridPos.y,
                Item = item.m_dropPrefab.name,
                Stars = Stars.Get(item),
                Stack = item.m_stack,
                Start = now,
            };
            foreach (CaskRecord previous in old)
            {
                if (previous.X == record.X && previous.Y == record.Y && previous.Item == record.Item
                    && previous.Stars == record.Stars && record.Stack <= previous.Stack && previous.Start <= now)
                    record.Start = previous.Start;
            }
            return record;
        }

        /// <summary>Raises the stack one star per full period passed, up to gold; true when it rose.</summary>
        private static bool Raise(ItemDrop.ItemData item, ref CaskRecord record, double now, double period)
        {
            int before = record.Stars;
            while (record.Stars < Stars.Max && now - record.Start >= period)
            {
                record.Stars++;
                record.Start += period;
            }
            if (record.Stars == before)
                return false;
            Stars.Set(item, record.Stars);
            return true;
        }
    }
}
