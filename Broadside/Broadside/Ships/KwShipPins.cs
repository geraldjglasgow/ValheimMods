using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;
using UnityEngine.UI;

namespace Broadside
{
    /// <summary>
    /// A map pin on each Broadside ship the player launched or steered last, named for its blueprint, so a lost ship can
    /// be found. A ship far away is not loaded on the player's game, so every few seconds the game asks the server where
    /// the player's ships are (<see cref="KwShipFinder"/>, which knows every ship's ZDO); its answer moves the pins, adds
    /// one for a new ship and takes away the pin of a ship that is gone. The pins are the map's own (the ship marker's
    /// picture, the player pins' kind, which has no filter to hide it), never saved, made only on this game. Made with the
    /// network (<see cref="KwShipPinsWake"/>) on every peer; a dedicated server only answers.
    /// </summary>
    public sealed class KwShipPins : MonoBehaviour
    {
        private const float Every = 5f, Moved = 0.25f;
        private static KwShipPins? live;
        private readonly Dictionary<ZDOID, Minimap.PinData> pins = new Dictionary<ZDOID, Minimap.PinData>();
        private float next = 2f;

        public static void Install(ZNet net)
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            KwShipFinder.Register(rpc);
            rpc.Register<ZPackage>(KwShipFinder.AnswerCall, (_, package) => Guard.Run("Broadside ship pins", () => live?.Take(package)));
            GameObject holder = new GameObject("KW_ship_pins");
            holder.transform.SetParent(net.transform, false);
            live = holder.AddComponent<KwShipPins>();
        }

        // The player's game asks the server, every few seconds while in the world.
        private void Update()
        {
            if (Time.time < next)
                return;
            next = Time.time + Every;
            Player? me = Player.m_localPlayer;
            if (me != null && Minimap.instance != null && ZRoutedRpc.instance != null)
                ZRoutedRpc.instance.InvokeRoutedRPC(KwShipFinder.AskCall, me.GetPlayerID());
        }

        // The server's answer: every ship's pin placed, the pins of ships no longer in it taken away.
        private void Take(ZPackage package)
        {
            Minimap? map = Minimap.instance;
            if (map == null)
                return;
            HashSet<ZDOID> seen = new HashSet<ZDOID>();
            int count = package.ReadInt();
            for (int i = 0; i < count; i++)
            {
                ZDOID id = package.ReadZDOID();
                Vector3 at = package.ReadVector3();
                string name = package.ReadString();
                seen.Add(id);
                Place(map, id, at, name);
            }
            Drop(map, seen);
        }

        private void Place(Minimap map, ZDOID id, Vector3 at, string name)
        {
            if (pins.TryGetValue(id, out Minimap.PinData pin) && map.m_pins.Contains(pin))
            {
                if ((pin.m_pos - at).sqrMagnitude > Moved * Moved)
                    (pin.m_pos, map.m_pinUpdateRequired) = (at, true);
                return;
            }
            pin = map.AddPin(at, Minimap.PinType.Player, name, false, false);
            Sprite? icon = Icon(map);
            if (icon != null)
                pin.m_icon = icon;
            pins[id] = pin;
        }

        private void Drop(Minimap map, HashSet<ZDOID> seen)
        {
            List<ZDOID> gone = new List<ZDOID>();
            foreach (KeyValuePair<ZDOID, Minimap.PinData> pin in pins)
            {
                if (seen.Contains(pin.Key))
                    continue;
                gone.Add(pin.Key);
                if (map.m_pins.Contains(pin.Value))
                    map.RemovePin(pin.Value);
            }
            gone.ForEach(id => pins.Remove(id));
        }

        // The map's own ship marker's picture (shown while steering), else the player pins' own.
        private static Sprite? Icon(Minimap map)
        {
            Image? image = map.m_largeShipMarker != null ? map.m_largeShipMarker.GetComponentInChildren<Image>(true) : null;
            return image != null ? image.sprite : null;
        }
    }

    /// <summary>The ship pins' calls and their maker, with the network (the server's answers on a dedicated server too).</summary>
    [HarmonyPatch(typeof(ZNet), "Awake")]
    public static class KwShipPinsWake
    {
        [HarmonyPostfix]
        private static void Postfix(ZNet __instance)
        {
            try
            {
                KwShipPins.Install(__instance);
            }
            catch (Exception e)
            {
                // Never out of ZNet.Awake: a throw there stops the network from starting.
                Log.Error($"Broadside ship pins could not be made: {e}");
            }
        }
    }
}
