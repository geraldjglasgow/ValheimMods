using System.Collections.Generic;
using System.Linq;
using HaloMenu.API;
using UnityEngine;

namespace HaloMenu.Runtime
{
    /// <summary>
    /// Which entry (if any) sits in each of a ring's segments, resolved once when the ring opens: visible entries
    /// (an invisible one is skipped and does not consume a slot), sorted by Order, capped at the segment count -
    /// entries beyond that are dropped, not wrapped to a second ring. Enabled state is re-read live every frame.
    /// </summary>
    public sealed class SlotAssignment
    {
        private readonly RingEntry[] slots;

        private SlotAssignment(RingEntry[] slots) => this.slots = slots;

        public static SlotAssignment Build(IEnumerable<RingEntry> entries, int segmentCount)
        {
            RingEntry[] slots = new RingEntry[segmentCount];
            List<RingEntry> visible = entries.Where(IsVisible).OrderBy(e => e.Order).Take(segmentCount).ToList();
            for (int i = 0; i < visible.Count; i++)
                slots[i] = visible[i];
            return new SlotAssignment(slots);
        }

        public bool IsFilled(int index) => index >= 0 && index < slots.Length && slots[index] != null;

        public Sprite IconAt(int index) => IsFilled(index) ? slots[index].Icon : null;

        public string LabelAt(int index) => IsFilled(index) ? slots[index].Label : null;

        public bool IsEnabledAt(int index) => IsFilled(index) && IsEnabled(slots[index]);

        public RingEntry EntryAt(int index) => IsFilled(index) ? slots[index] : null;

        private static bool IsVisible(RingEntry e) => e.IsVisible == null || SafeInvoke(e.IsVisible, true);

        private static bool IsEnabled(RingEntry e) => e.IsEnabled == null || SafeInvoke(e.IsEnabled, true);

        private static bool SafeInvoke(System.Func<bool> predicate, bool fallback)
        {
            try
            {
                return predicate();
            }
            catch (System.Exception ex)
            {
                HaloLog.Error($"A ring entry predicate threw, treating as {fallback}: {ex}");
                return fallback;
            }
        }
    }
}
