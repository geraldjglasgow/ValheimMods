using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The bolt's click and charge, on the caster's own machine, every physics step from <see cref="CasterControls"/> (user,
    /// 2026-10-11: "the player must click to cast, like the same with swords"; "how powerful the cast is, is determined by
    /// how long you hold the click down before releasing"). A press casts at once, through the game's own attack press
    /// (queued as the game queues one): the next move of a three-move combo, chosen by the game as it chooses a sword's
    /// swing (<see cref="WandMoves"/>). Still held after <see cref="ChargeAfter"/> s, before that cast's bolt has flown, the cast
    /// turns into a charge: the wand goes behind the caster (the game's charged-staff hold, an animator bool every machine
    /// sees, <see cref="WandAnimations"/>), the caster walking slowly as while casting (<see cref="ChargeStride"/>) and
    /// turning to aim (<see cref="ChargeFeet"/>); the cast it replaced already paid, so the release costs nothing more
    /// (<see cref="Prepaid"/>). Let go, the first move plays from the back and throws the charged bolt, as strong as the
    /// button was held, full at <see cref="CasterControls.FullCharge"/> s: at the trigger (<see cref="Trigger"/>) up to
    /// <see cref="ChargeBonus"/> more damage and a bigger bolt (<see cref="BoltSize"/>). The little spell at the tip
    /// (<see cref="ChargeState"/>) shows as a cast starts or <see cref="OrbAt"/> s into a charge, and goes when the bolt flies.
    /// </summary>
    public static class WindUp
    {
        private const string Hold = "staff_charging";
        private const float ChargeAfter = 0.2f;
        private const float OrbAt = 0.2f;
        private const float ChargeBonus = 0.3f;
        private const float OrbLongest = 4.5f;
        private const float QueueLife = 0.6f;

        private static float pressedAt = -1f;
        private static float chargeAt = -1f;
        private static float releasedAt = -1f;
        private static float castCharge;
        private static bool castCharged;
        private static bool pressCast;
        private static bool wasHeld;
        private static bool shown;
        private static float shownAt;
        private static Attack started;
        private static Attack spent;

        /// <summary>The local player is holding a charge now.</summary>
        public static bool Charging { get; private set; }

        /// <summary>The charged release was paid for by the cast the charge replaced.</summary>
        public static bool Prepaid { get; private set; }

        /// <summary>The charge of the last bolt thrown, 0 to 1, for its size.</summary>
        public static float Thrown { get; private set; }

        /// <summary>One physics step with left mouse <paramref name="held"/> or not: true when the game should take an attack press.</summary>
        public static bool Step(Player player, bool held)
        {
            bool pressed = held && !wasHeld;
            bool letGo = wasHeld && !held;
            wasHeld = held;
            if (pressed)
            {
                pressedAt = Time.time;
                pressCast = false;
            }
            if (letGo)
                return Release(player);
            Forget();
            if (Charging && Interrupted(player))
                Cancel(player);
            else if (!Charging && held && Time.time - pressedAt >= ChargeAfter && CanCharge(player))
                Begin(player);
            Orb(player);
            return pressed;
        }

        /// <summary>
        /// A release the game never took (its attack press queues half a second; a stagger or dodge can eat it) is
        /// forgotten, so the next click is a plain move again, not the charged release with an old charge.
        /// </summary>
        private static void Forget()
        {
            if (castCharged && Time.time - releasedAt > QueueLife)
            {
                castCharged = false;
                Prepaid = false;
            }
        }

        /// <summary>Not holding a wand any more (put away, the switch off): a charge ends without a bolt.</summary>
        public static void Stop(Player player)
        {
            wasHeld = false;
            if (Charging)
                Cancel(player);
            if (shown)
                Hide(player);
        }

        /// <summary>The bolt's cast has started (the game took the press): its move goes to every player near.</summary>
        public static void Started(Player player)
        {
            started = player.m_currentAttack;
            pressCast = true;
            if (!castCharged)
                castCharge = 0f;
            Attack attack = player.m_currentAttack;
            WandMoves.Send(player, castCharged ? WandMoves.Charged : Mathf.Clamp(attack.m_currentAttackCainLevel, 0, 2));
            castCharged = false;
            if (!shown)
                Show(player, Time.time, Time.time);
        }

        /// <summary>A charge may begin with nothing playing, or in place of the cast this press started, before its bolt flew.</summary>
        private static bool CanCharge(Player player)
        {
            Attack attack = player.m_currentAttack;
            bool ownCast = pressCast && attack != null && !attack.IsDone() && attack == started && attack != spent;
            return ownCast || Free(player);
        }

        private static void Begin(Player player)
        {
            Attack attack = player.m_currentAttack;
            Prepaid = pressCast && attack != null && !attack.IsDone() && attack == started && attack != spent;
            Charging = true;
            chargeAt = Time.time;
            player.m_queuedAttackTimer = 0f;
            player.m_zanim.SetBool(Hold, true);
            WandMoves.Send(player, WandMoves.ToCharge);
            // The press's cast showed a click's little spell; from here it grows with the charge.
            Show(player, pressedAt, -1f);
        }

        private static bool Release(Player player)
        {
            if (!Charging)
                return false;
            castCharge = Mathf.Clamp01((Time.time - pressedAt) / CasterControls.FullCharge);
            castCharged = true;
            releasedAt = Time.time;
            Charging = false;
            player.m_zanim.SetBool(Hold, false);
            if (shown)
                ChargeState.Release(player, Time.time);
            return true;
        }

        private static void Cancel(Player player)
        {
            Charging = false;
            Prepaid = false;
            castCharged = false;
            player.m_zanim.SetBool(Hold, false);
            if (shown)
                Hide(player);
        }

        private static void Orb(Player player)
        {
            if (Charging && !shown && Time.time - chargeAt >= OrbAt)
                Show(player, pressedAt, -1f);
            else if (!Charging && shown && Time.time - shownAt >= OrbLongest)
                Hide(player);
        }

        /// <summary>Nothing else playing: no cast, dodge, stagger or other action.</summary>
        private static bool Free(Player player) =>
            !player.InAttack() && !player.InDodge() && !player.IsStaggering() && !player.InMinorAction()
            && !player.IsDead() && !player.IsAttached() && !player.IsSwimming();

        private static bool Interrupted(Player player) =>
            player.InDodge() || player.IsStaggering() || player.IsDead() || player.IsAttached() || player.IsSwimming()
            || player.InMinorAction();

        private static void Show(Player player, float from, float letGoAt)
        {
            shown = true;
            shownAt = Time.time;
            ChargeState.Show(player, from, letGoAt);
        }

        private static void Hide(Player player)
        {
            shown = false;
            ChargeState.Hide(player);
        }

        /// <summary>The local player's wand bolt in progress, or null.</summary>
        private static Attack Bolt(Player player)
        {
            Attack attack = player.m_currentAttack;
            return attack != null && !attack.IsDone() && attack.m_attackType == Attack.AttackType.Projectile
                && CasterGearWorn.IsWand(attack.m_weapon) ? attack : null;
        }

        /// <summary>
        /// The bolt flies: the charge its press reached sets its damage (the attack's own multiplier, which the game puts on
        /// every projectile it fires from this trigger) and its size; the little spell at the tip goes.
        /// </summary>
        [HarmonyPatch(typeof(Attack), nameof(Attack.OnAttackTrigger))]
        private static class Trigger
        {
            /// <summary>One cast throws one bolt, whatever the animation does after (a second Hit is ignored).</summary>
            [HarmonyPrefix]
            private static bool Prefix(Attack __instance)
            {
                Player player = Player.m_localPlayer;
                if (player == null || __instance.m_character != player || __instance != Bolt(player))
                    return true;
                if (__instance == spent)
                    return false;
                spent = __instance;
                Prepaid = false;
                Thrown = castCharge;
                __instance.m_damageMultiplier *= 1f + ChargeBonus * Thrown;
                if (shown)
                    Hide(player);
                return true;
            }
        }
    }
}
