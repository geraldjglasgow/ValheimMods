using HarmonyLib;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Blood Magic experience for the caster of a shield as it absorbs hits (section 43, with the Weapon Mastery switch on;
    /// no level needed). Read from the game code 2026-10-11: a shield (SE_Shield) takes every hit its wearer gets in
    /// SE_Shield.OnDamaged, called by Character.RPC_Damage on the wearer's owner before armour and blocking, adds the hit's
    /// damage to what it has soaked up and zeroes the hit; once it has soaked up more than it can hold, SE_Shield.IsDone
    /// breaks it and raises the break skill (m_levelUpSkillOnBreak) of the wearer, not the caster. That stays as it is.
    /// <list type="bullet">
    /// <item>On the wearer's owner (any machine, a dedicated server included), a prefix on OnDamaged reads what the hit
    /// will take out of the shield, at most what the shield had left, and turns it into experience: 1 per
    /// <see cref="DamagePerExperience"/> damage (a spell that hits gives 1), at most <see cref="MaxPerHit"/> per hit. So a
    /// shield trains at most its capacity's worth.</item>
    /// <item>It counts only shields a player cast (<see cref="ShieldCaster"/>) that train Blood Magic on breaking or name
    /// no skill, and only hits from a creature that is not tamed and not the wearer: falls, fire and other hits with no
    /// attacker, and every player's hit (a friend in PvP, the caster themselves), train nothing, so nobody farms it on
    /// themselves.</item>
    /// <item>It sends <see cref="Keys.RpcShieldXp"/> by routed RPC to the owner of the caster's character, which is the
    /// caster's own machine (at once and without a send when that is this machine); nothing is sent when this machine no
    /// longer has that character (the caster left, died or went far away). The caster's client raises Blood Magic through
    /// Player.RaiseSkill, so the game's and GrindstoneSkills' experience multipliers apply. Registered in a ZNet.Awake
    /// postfix, where the game creates a fresh ZRoutedRpc for each session.</item>
    /// </list>
    /// </summary>
    public static class ShieldXp
    {
        /// <summary>Damage a shield absorbs per point of experience for its caster.</summary>
        private const float DamagePerExperience = 40f;

        /// <summary>The most experience one absorbed hit gives.</summary>
        private const float MaxPerHit = 2f;

        /// <summary>The perk's tip on the Blood Magic page.</summary>
        public static string Tip =>
            Lang.Format("gs_mastery_shieldtraining_tip", DamagePerExperience.ToString("0"), SkillPage.Number(MaxPerHit));

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => ZRoutedRpc.instance?.Register<float>(Keys.RpcShieldXp, Receive);
        }

        [HarmonyPatch(typeof(SE_Shield), nameof(SE_Shield.OnDamaged))]
        private static class Absorb
        {
            [HarmonyPrefix]
            private static void Prefix(SE_Shield __instance, HitData hit, Character attacker)
            {
                if (hit != null && GameSkillSettings.WeaponMastery.Value)
                    HookGuard.Run("shield experience", static a => Absorbed(a.shield, a.hit, a.attacker), (shield: __instance, hit, attacker));
            }
        }

        private static void Absorbed(SE_Shield shield, HitData hit, Character attacker)
        {
            if (!TrainsBloodMagic(shield) || !Fair(shield, attacker) || !ShieldCaster.TryGet(shield, out ZDOID caster))
                return;
            float absorbed = Mathf.Min(hit.GetTotalDamage(), shield.m_totalAbsorbDamage - shield.m_damage);
            if (absorbed > 0f)
                Send(caster, Mathf.Min(absorbed / DamagePerExperience, MaxPerHit));
        }

        /// <summary>A blood magic shield: it raises Blood Magic when it breaks, or names no skill to raise.</summary>
        private static bool TrainsBloodMagic(SE_Shield shield) =>
            shield.m_levelUpSkillOnBreak == Skills.SkillType.BloodMagic || shield.m_levelUpSkillOnBreak == Skills.SkillType.None;

        /// <summary>A hit from a wild creature other than the wearer.</summary>
        private static bool Fair(SE_Shield shield, Character attacker) =>
            attacker != null && !attacker.IsPlayer() && !attacker.IsTamed() && attacker != shield.m_character;

        private static void Send(ZDOID caster, float experience)
        {
            ZDO zdo = ZDOMan.instance?.GetZDO(caster);
            long peer = zdo != null ? zdo.GetOwner() : 0L;
            if (peer != 0L && ZRoutedRpc.instance != null)
                ZRoutedRpc.instance.InvokeRoutedRPC(peer, Keys.RpcShieldXp, experience);
        }

        private static void Receive(long sender, float experience) =>
            HookGuard.Run("shield experience credit", static xp => Credit(xp), experience);

        /// <summary>On the caster's client: Blood Magic experience for what their shield absorbed.</summary>
        private static void Credit(float experience)
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead() || !GameSkillSettings.WeaponMastery.Value || !(experience > 0f))
                return;
            player.RaiseSkill(Skills.SkillType.BloodMagic, Mathf.Min(experience, MaxPerHit));
        }
    }
}
