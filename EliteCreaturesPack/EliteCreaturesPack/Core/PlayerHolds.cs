using EliteCreaturesPack.Arsenal;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Crossbow;
using EliteCreaturesPack.Headsman;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCreaturesPack
{
    /// <summary>
    /// Every player carries the bone weapons' holds (<see cref="ArsenalAtgeirHold"/>, <see cref="XbowHold"/> with
    /// <see cref="XbowPlayerRig"/>, <see cref="GreataxeHold"/>) on every peer that draws, from one patch. They only pose
    /// and animate what is seen (a player's attacks run on that player's own machine), so a dedicated server gets none.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    public static class PlayerHolds
    {
        private static void Postfix(Player __instance)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;
            }
            SafeCall.Run("Player.Awake bone weapon holds", static player => Add(player.gameObject), __instance);
        }

        private static void Add(GameObject player)
        {
            if (player.GetComponent<ArsenalAtgeirHold>() == null)
            {
                player.AddComponent<ArsenalAtgeirHold>();
            }
            if (player.GetComponent<XbowHold>() == null)
            {
                player.AddComponent<XbowHold>();
                player.AddComponent<XbowPlayerRig>();
            }
            if (player.GetComponent<GreataxeHold>() == null)
            {
                player.AddComponent<GreataxeHold>();
            }
        }
    }
}
