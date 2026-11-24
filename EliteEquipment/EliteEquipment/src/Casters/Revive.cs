using HarmonyLib;
using LocalEffects;
using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Revive (features/casters.md, "Nine more spells"): cast at a friend's grave (<see cref="ReviveGrave"/>: another
    /// online player's tombstone, dead within 3 minutes), it brings that player back to the grave with half their most
    /// health. The cast is checked as it starts (<see cref="Refusal"/>, before any eitr is spent; refused with a message
    /// when no such grave is aimed at or the caster's <see cref="ReviveCooldown"/> runs) and again where it lands
    /// (<see cref="Cast"/>). The player is moved on their own machine, where the game moves them: one routed message to
    /// everybody (<see cref="Rpc"/>; only the machine whose profile is the grave's owner acts on it, a dedicated server
    /// never does) and that machine teleports them the game's way and heals them on arrival (<see cref="ReviveArrival"/>).
    /// The golden pillar (<c>ee_fx_cast_revive</c>) shows at the grave as the cast lands and on the player as they
    /// arrive, to every player near (<see cref="CasterFx"/>'s message).
    /// </summary>
    public static class Revive
    {
        public const float HealthShare = 0.5f;

        private const string Rpc = "EliteEquipment_Revive";
        private const string Sound = "sfx_Potion_health_Start";
        // The pillar's flash radius at which LocalEffect draws an effect as authored.
        private const float AsAuthored = 4f;

        private static TombStone locked;

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => ZRoutedRpc.instance?.Register<long, Vector3, ZDOID, string>(Rpc, Receive);
        }

        /// <summary>Why the cast cannot start (a word), or null; the grave aimed at is kept for the landing.</summary>
        public static string Refusal(Player caster)
        {
            locked = null;
            if (caster.GetSEMan().HaveStatusEffect(ReviveCooldown.Hash))
                return "ee_spell_revive_wait";
            TombStone grave = ReviveGrave.Aimed(caster);
            string why = grave == null ? "ee_spell_revive_no_grave" : ReviveGrave.Refusal(grave, caster);
            if (why == null)
                locked = grave;
            return why;
        }

        /// <summary>At the motion's <c>Hit</c>: the grave checked again, the call sent, the cooldown begun, the pillar shown.</summary>
        public static void Cast(Player caster, Spell spell)
        {
            TombStone grave = locked;
            locked = null;
            string why = grave == null ? "ee_spell_revive_no_grave" : ReviveGrave.Refusal(grave, caster);
            if (why != null)
            {
                SpellCast.Tell(Lang.Get(why));
                return;
            }
            ZDO zdo = ReviveGrave.Zdo(grave);
            ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpc, zdo.GetLong(ZDOVars.s_owner, 0L), grave.transform.position, zdo.m_uid, caster.GetPlayerName());
            caster.GetSEMan().AddStatusEffect(ReviveCooldown.Hash, resetTime: true);
            CasterFx.Send(HealReleases.Revive, GroundPoint.Below(grave.transform.position), ZDOID.None, spell);
            caster.Message(MessageHud.MessageType.Center, Lang.Format("ee_spell_revive_sent", zdo.GetString(ZDOVars.s_ownerName, "")));
        }

        private static void Receive(long sender, long owner, Vector3 grave, ZDOID id, string caster)
        {
            PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            if (profile != null && profile.GetPlayerID() == owner && CastersSettings.On)
                CasterSafe.Run("revive", () => ReviveArrival.Begin(grave, id, caster));
        }

        /// <summary>The pillar on this machine: on the one revived, following them, else at the grave; Mend's motes without it.</summary>
        public static void Show(Vector3 at, Character on)
        {
            GameObject prefab = CasterFx.Prepared(HealReleases.Revive);
            if (prefab == null)
            {
                if (on != null)
                    CasterFx.MendOn(on);
                else
                    CasterFx.Here(CasterFx.Mend, at);
                return;
            }
            if (on != null)
                LocalEffect.Attach(prefab, on.transform, on.transform.position, endless: false);
            else
                LocalEffect.Flash(prefab, at, AsAuthored);
            LocalEffect.Sound(ZNetScene.instance?.GetPrefab(Sound), at);
        }
    }
}
