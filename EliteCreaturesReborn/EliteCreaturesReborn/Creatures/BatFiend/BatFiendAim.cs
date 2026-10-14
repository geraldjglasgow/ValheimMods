using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// Turns a Bat-fiend attack's hit fan to where its striking part really sweeps. The game centres a melee fan on the
    /// attacker's forward (<c>Attack.GetMeleeAttackDir</c>, then <c>DoMeleeAttack</c> sweeps half the angle each side of
    /// it); the Wing Guillotine's left wing sweeps from behind-left across the front, so its fan is turned to the left.
    /// A postfix on GetMeleeAttackDir, which runs on the attacker's owner where the hits are worked out, so every peer
    /// sees the same hits. Only the attacks named here are touched, by their item name.
    /// </summary>
    internal static class BatFiendAim
    {
        private static readonly Dictionary<string, float> turns = new Dictionary<string, float>();
        private static bool _patched;

        /// <summary>Turns the named attack's fan by <paramref name="degrees"/> (left negative); 0 leaves it on forward.</summary>
        public static void Turn(string itemName, float degrees)
        {
            if (degrees != 0f)
            {
                turns[itemName] = degrees;
            }
        }

        public static void Install(Harmony harmony)
        {
            if (_patched)
            {
                return;
            }
            _patched = true;
            harmony.Patch(AccessTools.Method(typeof(Attack), "GetMeleeAttackDir"),
                postfix: new HarmonyMethod(typeof(BatFiendAim), nameof(AfterAim)));
        }

        private static void AfterAim(Attack __instance, ref Vector3 attackDir)
        {
            ItemDrop.ItemData? weapon = __instance.m_weapon;
            if (turns.Count > 0 && weapon != null && turns.TryGetValue(weapon.m_shared.m_name, out float degrees))
            {
                attackDir = Quaternion.AngleAxis(degrees, Vector3.up) * attackDir;
            }
        }
    }
}
