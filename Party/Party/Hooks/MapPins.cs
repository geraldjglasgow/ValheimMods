using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Party.Client;

namespace Party.Hooks
{
    /// <summary>
    /// Always-on map/minimap pins for online party members, from the vitals channel. The pins are the local
    /// player's own (owner 0), not shared-map pins, so hiding shared map data never hides them and the game
    /// draws them full size and white; the party colour goes on after the game's own pin pass.
    /// </summary>
    public static class MapPins
    {
        private static readonly Dictionary<long, Minimap.PinData> pins = new Dictionary<long, Minimap.PinData>();
        private static readonly HashSet<long> current = new HashSet<long>();
        private static readonly List<long> stale = new List<long>();

        public static void Tick()
        {
            if (Minimap.instance == null)
                return;
            if (!PartyClientState.InParty)
            {
                ClearAll();
                return;
            }
            current.Clear();
            long selfId = Identity.LocalPlayerId;
            foreach (PartyMemberView member in PartyClientState.Members)
            {
                if (member.Id == selfId || !member.Online || !member.PositionValid)
                    continue;
                current.Add(member.Id);
                UpdateOrCreate(member);
            }
            RemoveStale();
        }

        private static void UpdateOrCreate(PartyMemberView member)
        {
            Minimap map = Minimap.instance;
            if (!pins.TryGetValue(member.Id, out Minimap.PinData pin) || pin == null)
            {
                pin = map.AddPin(member.Position, Minimap.PinType.Player, member.Name, save: false, isChecked: false);
                pins[member.Id] = pin;
            }
            if (pin.m_pos != member.Position)
            {
                pin.m_pos = member.Position;
                map.m_pinUpdateRequired = true;   // as the game does for its own player pins
            }
            pin.m_name = member.Name;
            bool leader = member.IsLeader;
            if (pin.m_doubleSize != leader)
            {
                pin.m_doubleSize = leader;
                map.DestroyPinMarker(pin);   // the size is read when the marker is made: the next pin pass remakes it
                map.m_pinUpdateRequired = true;
            }
        }

        /// <summary>After <c>Minimap.UpdatePins</c>, which sets every pin icon white on each pass.</summary>
        public static void Recolor()
        {
            foreach (KeyValuePair<long, Minimap.PinData> entry in pins)
            {
                Image icon = entry.Value?.m_iconElement;
                if (icon == null)
                    continue;
                PartyMemberView member = PartyClientState.Find(entry.Key);
                Color color = ColorHelper.MemberColor(member != null && member.IsLeader);
                if (icon.color != color)
                    icon.color = color;
            }
        }

        private static void RemoveStale()
        {
            stale.Clear();
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
            if (pins.Count == 0)
                return;
            foreach (Minimap.PinData pin in pins.Values)
            {
                if (pin != null && Minimap.instance != null)
                    Minimap.instance.RemovePin(pin);
            }
            pins.Clear();
        }
    }

    /// <summary>The party colours on our pins, put back after each of the game's pin passes.</summary>
    [HarmonyPatch(typeof(Minimap), "UpdatePins")]
    public static class MapPinsColorPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            MapPins.Recolor();
            TempPartyPins.Recolor();
        }
    }
}
