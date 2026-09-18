using HarmonyLib;
using UnityEngine;
using Party.Client;

namespace Party.Hooks
{
    /// <summary>Holding the configured modifier key while pinging the map sends a party-only ping instead of a shout.</summary>
    [HarmonyPatch(typeof(global::Chat), "SendPing")]
    public static class PingPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Vector3 position)
        {
            if (!PartyClientState.InParty || !IsModifierHeld())
                return true;
            PartyPing.Send(position);
            return false;
        }

        private static bool IsModifierHeld()
        {
            if (!System.Enum.TryParse(PartyConfig.PingModifierKey.Value, true, out KeyCode key))
                key = KeyCode.LeftAlt;
            return Input.GetKey(key);
        }
    }
}
