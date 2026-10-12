using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A caster's buttons, while the local player holds a wand (user, 2026-10-11), read where the game takes the player's
    /// controls (<c>Player.SetControls</c>, every physics step, from the player's own input):
    /// <list type="bullet">
    /// <item>Left mouse: let go, the bolt's cast starts, as strong as the button was held long, full at
    /// <see cref="FullCharge"/> s ("+30% more damage"); a click is the next move of the combo, a hold charges with the wand
    /// behind the caster, walking slowly (<see cref="WindUp"/>: the press is passed on to the game only when let go).</item>
    /// <item>A caster does not block: right mouse casts the spell in slot 1 when let go, right mouse held and left mouse
    /// clicked casts the spell in slot 2 at once (and then nothing on letting go); middle mouse, the game's secondary
    /// attack, casts slot 1 (<see cref="Slot"/>). Right mouse held and jump pressed dodges (a blink, <see cref="CasterBlink"/>),
    /// and then casts nothing.</item>
    /// </list>
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    public static class CasterControls
    {
        /// <summary>Seconds to full charge (user's reference, 2026-10-11: "3 second charge"; 2.5 s before).</summary>
        public const float FullCharge = 3f;

        private static bool held;
        private static bool used;
        private static bool dodgeNow;

        /// <summary>The slot (0 or 1) whose spell the next cast takes.</summary>
        public static int Slot { get; private set; }

        /// <summary>Left mouse is held for the bolt now.</summary>
        public static bool BoltHeld { get; private set; }

        [HarmonyPrefix]
        private static void Prefix(Player __instance, ref bool attack, ref bool attackHold,
            ref bool secondaryAttack, ref bool block, ref bool blockHold, ref bool jump)
        {
            if (__instance != Player.m_localPlayer)
                return;
            if (!CastersSettings.On || CasterGearWorn.Wand(__instance) == null)
            {
                NoWand(__instance);
                return;
            }
            if (secondaryAttack)
                Slot = 0;
            bool right = blockHold;
            Spells(right, ref attack, ref secondaryAttack, ref jump);
            block = false;
            blockHold = false;
            BoltHeld = !right && attackHold;
            attackHold = false;
            attack = WindUp.Step(__instance, BoltHeld);
            if (WindUp.Charging)
                jump = false;
        }

        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            if (!dodgeNow || __instance != Player.m_localPlayer)
                return;
            dodgeNow = false;
            Vector3 direction = __instance.m_moveDir;
            if (direction.magnitude < 0.1f)
            {
                direction = -__instance.m_lookDir;
                direction.y = 0f;
                direction.Normalize();
            }
            __instance.Dodge(direction);
        }

        private static void NoWand(Player player)
        {
            held = used = false;
            BoltHeld = false;
            WindUp.Stop(player);
        }

        /// <summary>Right mouse: held with jump dodges, held with left mouse casts slot 2, let go casts slot 1 if neither did.</summary>
        private static void Spells(bool right, ref bool attack, ref bool secondaryAttack, ref bool jump)
        {
            if (right && jump)
            {
                dodgeNow = used = true;
                jump = false;
            }
            if (right && attack && !used)
            {
                used = true;
                Slot = 1;
                secondaryAttack = true;
            }
            if (held && !right && !used)
            {
                Slot = 0;
                secondaryAttack = true;
            }
            if (!right)
                used = false;
            held = right;
        }
    }
}
