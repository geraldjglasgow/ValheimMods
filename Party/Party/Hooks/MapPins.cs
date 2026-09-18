using System.Collections.Generic;
using Party.Client;

namespace Party.Hooks
{
    /// <summary>Always-on map/minimap pins for online party members, from the vitals channel.</summary>
    public static class MapPins
    {
        private static readonly Dictionary<long, Minimap.PinData> pins = new Dictionary<long, Minimap.PinData>();

        public static void Tick()
        {
            if (Minimap.instance == null)
                return;
            if (!PartyClientState.InParty)
            {
                ClearAll();
                return;
            }
            HashSet<long> current = new HashSet<long>();
            foreach (PartyMemberView member in PartyClientState.Members)
            {
                if (member.Id == Identity.LocalPlayerId || !member.Online || !member.PositionValid)
                    continue;
                current.Add(member.Id);
                UpdateOrCreate(member);
            }
            RemoveStale(current);
        }

        private static void UpdateOrCreate(PartyMemberView member)
        {
            if (!pins.TryGetValue(member.Id, out Minimap.PinData pin) || pin == null)
            {
                pin = Minimap.instance.AddPin(member.Position, Minimap.PinType.Player, member.Name, save: false, isChecked: false, member.Id);
                pins[member.Id] = pin;
            }
            pin.m_pos = member.Position;
            pin.m_name = member.Name;
            pin.m_doubleSize = member.IsLeader;
            if (pin.m_iconElement != null)
                pin.m_iconElement.color = member.IsLeader ? ColorHelper.Parse(PartyConfig.LeaderColor.Value) : ColorHelper.Parse(PartyConfig.PartyColor.Value);
        }

        private static void RemoveStale(HashSet<long> current)
        {
            List<long> stale = new List<long>();
            foreach (KeyValuePair<long, Minimap.PinData> entry in pins)
            {
                if (!current.Contains(entry.Key))
                    stale.Add(entry.Key);
            }
            foreach (long id in stale)
                Remove(id);
        }

        private static void Remove(long id)
        {
            if (pins.TryGetValue(id, out Minimap.PinData pin) && pin != null)
                Minimap.instance.RemovePin(pin);
            pins.Remove(id);
        }

        private static void ClearAll()
        {
            foreach (Minimap.PinData pin in pins.Values)
            {
                if (pin != null && Minimap.instance != null)
                    Minimap.instance.RemovePin(pin);
            }
            pins.Clear();
        }
    }
}
