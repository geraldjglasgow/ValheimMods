using System.Collections.Generic;
using HarmonyLib;
using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Casting, on the caster's own machine. When the local player starts a wand's secondary attack (a spell picked on
    /// the spell ring, a right mouse tap or middle mouse, <see cref="SpellRing"/>; the primary, left mouse, fires the bolt:
    /// user, 2026-10-11) the wand's last picked spell is fixed for that cast (<see cref="Casting"/>, <see cref="LastSpell"/>).
    /// The cast does not start without a spellbook in the left hand, or when that spell's page is not in the book (or none
    /// was picked yet); with the switch off neither the cast nor the bolt does.
    /// When the cast lands (the game's <c>Attack.DoNonAttack</c>, the trigger of an attack that hits nothing) the spell
    /// takes effect: Mend flies to the friend highlighted as the cast started (<see cref="HealTarget"/>, the lock) and
    /// heals them where it lands, or heals the caster when no friend is highlighted (<c>Character.Heal</c>, which the game
    /// sends to the owner of a character on another machine); Cleanse lifts the same target's harmful effects at once, with
    /// a small heal (<see cref="Cleanse"/>); Ward shields the same target (<see cref="Ward"/>) and Sanctuary raises its dome
    /// at the caster's feet, not while its cooldown is on (<see cref="Sanctuary"/>); a chant puts its status effect on every friend within
    /// <see cref="Spell.ChantRange"/> (<c>SEMan.AddStatusEffect</c>, sent to each owner the same way); a strike fires at
    /// the aim (<see cref="VolleyCast"/>, its hits raising Elemental Magic). Each heal and chant raises Blood Magic;
    /// every cast shows its effects to the players near (<see cref="CasterFx"/>). A spell with its own motion
    /// (<see cref="SpellMotion"/>) lands at its clip's <c>Hit</c> (<see cref="SpellHits"/>), else at the game cast's.
    /// </summary>
    public static class SpellCast
    {
        private const float MessageEvery = 2f;
        private static readonly List<Character> near = new List<Character>();
        private static float lastMessage;
        private static Character locked;

        /// <summary>The spell of the local player's current or last wand cast.</summary>
        public static Spell Casting { get; private set; }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
        private static class StartPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(Humanoid __instance, bool secondaryAttack, ref bool __result)
            {
                if (__instance != Player.m_localPlayer || CasterGearWorn.Wand(__instance) == null)
                    return true;
                if (!CastersSettings.On)
                {
                    __result = false;
                    return false;
                }
                if (!secondaryAttack)
                    return true;
                if (CasterGearWorn.Book(__instance) == null)
                    return Refuse(Lang.Get("ee_spell_no_book"), ref __result);
                Casting = LastSpell.In(Player.m_localPlayer, CasterGearWorn.Wand(__instance));
                if (Casting == null)
                    return Refuse(Lang.Get("ee_spell_none_ready"), ref __result);
                if (Sanctuary.Waits(Player.m_localPlayer, Casting))
                    return Refuse(Lang.Get("ee_spell_sanctuary_cooling"), ref __result);
                if (!HealSpells.Ready(Player.m_localPlayer, Casting, out string why))
                    return Refuse(why, ref __result);
                locked = (Casting.Kind == SpellKind.Heal && !HealSpells.Unmarked(Casting)) || Casting == Spells.Ward ? HealTarget.Current : null;
                SpellMotion.Choose(Player.m_localPlayer, CasterGearWorn.Wand(__instance), Casting);
                return true;
            }

            /// <summary>
            /// The turn to the locked friend begins only once the game has started the cast (it refuses one it cannot pay
            /// for before any animation): a refused cast neither plays nor turns the caster (user, 2026-10-10). The spell's own
            /// motion goes to every player near only then too (<see cref="SpellMotion"/>).
            /// </summary>
            [HarmonyPostfix]
            private static void Postfix(Humanoid __instance, bool secondaryAttack, bool __result)
            {
                if (!__result || __instance != Player.m_localPlayer || CasterGearWorn.Wand(__instance) == null)
                    return;
                if (!secondaryAttack)
                {
                    WindUp.Started(Player.m_localPlayer);
                    return;
                }
                SpellMotion.Started(Player.m_localPlayer);
                if (locked != null)
                    CastTurn.Begin(locked);
            }
        }

        [HarmonyPatch(typeof(Attack), "DoNonAttack")]
        private static class TriggerPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Attack __instance)
            {
                Player player = __instance.m_character as Player;
                if (player == null || player != Player.m_localPlayer || !CasterGearWorn.IsWand(__instance.m_weapon))
                    return;
                Spell spell = Casting;
                if (spell != null && CastersSettings.On)
                    CasterSafe.Run("EliteEquipment cast", () => Cast(player, __instance.m_weapon, spell, Origin(__instance)));
            }
        }

        private static void Cast(Player caster, ItemDrop.ItemData wand, Spell spell, Vector3 tip)
        {
            CasterFx.Send(CasterFx.Cast, tip, ZDOID.None, spell);
            if (spell == Spells.Cleanse)
                Cleanse.Cast(caster, wand, spell, locked);
            else if (spell == Spells.Ward)
                Ward.Cast(caster, wand, spell, locked);
            else if (spell == Spells.Sanctuary)
                Sanctuary.Cast(caster, spell);
            else if (HealSpells.Casts(spell))
                HealSpells.Cast(caster, wand, spell, locked, tip);
            else if (spell.Kind == SpellKind.Heal)
                Mend(caster, wand, spell, tip);
            else if (spell == Spells.Breath)
                BreathCast.Fire(caster, wand, spell);
            else if (spell.Kind == SpellKind.Strike)
                VolleyCast.Fire(caster, wand, spell);
            else
                Chant(caster, spell);
            if (spell.Kind != SpellKind.Strike)
                caster.RaiseSkill(Skills.SkillType.BloodMagic, 1f);
        }

        /// <summary>
        /// The friend locked on to as the cast started gets the heal when it lands (<see cref="HealFlight"/>, its flight
        /// shown to the players near). With no friend locked on, or one who died or went out of reach during the cast, the
        /// caster heals themselves (user, 2026-10-11: "allow spell to heal yourself if its not targeting an ally").
        /// </summary>
        private static void Mend(Player caster, ItemDrop.ItemData wand, Spell spell, Vector3 tip)
        {
            float amount = HealPower.Of(caster, wand);
            bool stamina = caster.GetSEMan().HaveStatusEffect(RobeSets.Plains.BonusHash);
            Character friend = FriendTarget.StillValid(locked, caster) && locked != caster ? locked : null;
            if (friend == null)
            {
                Heal(caster, amount, stamina);
                CasterFx.Send(CasterFx.Mend, caster.transform.position, caster.GetZDOID(), spell);
                return;
            }
            CasterFx.Send(CasterFx.MendBolt, tip, friend.GetZDOID(), spell, self: false);
            HealFlight.Launch(tip, friend, landed => Land(landed, amount, stamina));
        }

        private static void Land(Character friend, float amount, bool stamina)
        {
            if (friend == null)
                return;
            Heal(friend, amount, stamina);
            CasterFx.MendOn(friend);
        }

        /// <summary>The heal, and with a Sunweave wearer's heal its stamina too (sent to the healed one's owner by the game).</summary>
        private static void Heal(Character healed, float amount, bool stamina)
        {
            healed.Heal(amount, true);
            if (stamina)
                healed.GetSEMan().AddStatusEffect(CasterStatus.HealStaminaHash, resetTime: true);
        }

        private static void Chant(Player caster, Spell spell)
        {
            bool longer = caster.GetSEMan().HaveStatusEffect(RobeSets.BlackForest.BonusHash);
            int effect = (longer ? spell.Effect + CasterStatus.LongSuffix : spell.Effect).GetStableHashCode();
            near.Clear();
            Character.GetCharactersInRange(caster.transform.position, Spell.ChantRange, near);
            foreach (Character character in near)
            {
                if (FriendTarget.Friendly(character, caster))
                    character.GetSEMan().AddStatusEffect(effect, resetTime: true);
            }
            near.Clear();
            // The chant's own release (its ring and more, turned to the caster's facing: the caster's id goes with it), else the shared ring.
            CasterFx.Send(spell.ReleaseEffect ?? CasterFx.Chant, caster.transform.position, caster.GetZDOID(), spell);
        }

        /// <summary>The wand's tip in the hand (its <c>tip</c> point), else where the game starts the attack.</summary>
        private static Vector3 Origin(Attack attack)
        {
            VisEquipment vis = attack.m_character.GetComponent<VisEquipment>();
            Transform tip = vis != null && vis.m_rightItemInstance != null ? HeldModel.Tip(vis.m_rightItemInstance.transform) : null;
            return tip != null ? tip.position : attack.GetAttackOrigin().position;
        }

        /// <summary>The cast does not start; why, in the middle of the screen.</summary>
        private static bool Refuse(string word, ref bool result)
        {
            result = false;
            Tell(word);
            return false;
        }

        /// <summary>A caster's message in the middle of the screen, not more often than every two seconds.</summary>
        public static void Tell(string word)
        {
            if (Time.time - lastMessage < MessageEvery)
                return;
            lastMessage = Time.time;
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, word);
        }
    }
}
