using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The death-triggered mutations, all on the dying creature's owner: Bloated leaves a fuse-and-blast behind,
    /// Splintering breaks into copies, and a Devouring killer keeps its victim's health and damage.
    /// <para>
    /// The fault this fixes: vanilla <c>Character.OnDeath</c> ends with <c>ZNetScene.Destroy</c>, which calls
    /// <c>ResetZDO</c> and nulls the creature's ZDO. A postfix therefore sees <c>IsValid()</c>/<c>IsOwner()</c> false and
    /// <c>GetZDO()</c> null, so everything read from the ZDO (the split's prefab/generation/root, the devour marker) was
    /// silently gone - which is why Splintering never split. So a PREFIX snapshots everything the death behaviours need
    /// while the ZDO is still alive; the POSTFIX (after the ragdoll exists, so Bloated's warning can ride it) acts on the
    /// snapshot. The prefix is exception-safe and never rethrows: a throw there would abort the creature's own death.
    /// </para>
    /// </summary>
    [HarmonyPatch(typeof(Character), "OnDeath")]
    public static class DeathPatch
    {
        private sealed class Snapshot
        {
            public Character Victim = null!;
            public CreatureTraits Traits = null!;
            public BiomeRules Rules = null!;
            public int PrefabHash;
            public int Generation;
            public string CascadeRoot = "";
            public string ResolvedRoot = "";
            public Heightmap.Biome Biome;
            public float MaxHealth;
            public Vector3 Pos;
            public Quaternion Rot;
            public ZDOID Devourer;
            public List<PouchStore.Entry> Pouch = new List<PouchStore.Entry>();
        }

        private static Snapshot? _pending;

        // Runs before vanilla resets the ZDO. Must not rethrow, or the creature would fail to die - so it reports and
        // swallows via Guard.Report (never Guard.Run), the pattern for a prefix that must never break the method it guards.
        private static void Prefix(Character __instance)
        {
            try
            {
                _pending = Capture(__instance);
            }
            catch (Exception e)
            {
                _pending = null;
                Guard.Report(e, "Character.OnDeath capture");
            }
        }

        private static Snapshot? Capture(Character victim)
        {
            ZNetView nview = victim.GetComponent<ZNetView>();
            bool owner = nview != null && nview.IsValid() && nview.IsOwner();
            EliteController controller = victim.GetComponent<EliteController>();
            Log.Diag($"OnDeath {victim.name}: owner={owner} ready={(controller != null && controller.Ready)}");
            if (!owner || controller == null || !controller.Ready)
            {
                return null; // all death work is the owner's, and only on a resolved creature
            }
            return Build(victim, controller, nview!.GetZDO());
        }

        private static Snapshot Build(Character victim, EliteController controller, ZDO zdo)
        {
            string stored = zdo.GetString(TraitKeys.CascadeRoot);
            // A gen-0 parent has no stored root, so its cascade is rooted at its own id - resolved here while the ZDOID
            // is still readable, because after the ZDO reset the postfix cannot ask the creature for its id any more.
            string resolved = string.IsNullOrEmpty(stored) ? victim.GetZDOID().ToString() : stored;
            return new Snapshot
            {
                Victim = victim, Traits = controller.Traits, Rules = controller.Rules,
                PrefabHash = zdo.GetPrefab(), Generation = zdo.GetInt(TraitKeys.Generation),
                CascadeRoot = stored, ResolvedRoot = resolved, Biome = TraitStore.GetBiome(zdo),
                MaxHealth = victim.GetMaxHealth(), Pos = victim.transform.position, Rot = victim.transform.rotation,
                Devourer = TraitStore.GetDevouredBy(zdo), Pouch = PouchStore.Load(zdo),
            };
        }

        private static void Postfix(Character __instance) =>
            Guard.Run("Character.OnDeath", () => Handle(__instance));

        private static void Handle(Character victim)
        {
            Snapshot? snap = _pending;
            _pending = null;
            if (snap == null || snap.Victim != victim)
            {
                return; // no valid snapshot for this creature (not owner, not resolved, or capture failed)
            }
            RunTraitDeaths(snap);
            Feed(snap);
            DescendantRegistry.Unregister(snap.CascadeRoot);
        }

        private static void RunTraitDeaths(Snapshot snap)
        {
            if (snap.Traits.Has(Mutation.Bloated))
            {
                Bloat(snap);
            }
            if (snap.Traits.Has(Mutation.Splintering))
            {
                Log.Diag($"{snap.Victim.name}: splintering stars={snap.Traits.Stars} gen={snap.Generation}");
                Splitter.Split(snap.Pos, snap.Rot, snap.PrefabHash, snap.Traits, snap.Rules,
                    snap.Generation, snap.ResolvedRoot, snap.Biome);
            }
            if (snap.Traits.Has(Mutation.Thieving))
            {
                // Splintering copies never inherit the pouch (Splitter.Configure never touches TraitKeys.Pouch), so a
                // Thieving+Splintering death both empties this parent's pouch here AND is born-empty as two copies.
                PouchDrop.DropAll(snap.Pouch, snap.Pos);
            }
        }

        // Broadcasts the Bloated death so every client wears a warning that rides its own local corpse for the whole
        // fuse; the owner, when its fuse ends, broadcasts the blast at its corpse's resting place (see EliteRpc).
        private static void Bloat(Snapshot snap)
        {
            BiomeRules rules = snap.Rules;
            CreatureTraits traits = snap.Traits;
            Log.Diag($"{snap.Victim.name}: bloated death, fuse={rules.PowerOf(Mutation.Bloated, Fields.Delay)}s");
            EliteRpc.FireBloat(snap.Pos,
                Enhance.Magnitude(rules, traits, Mutation.Bloated, Fields.Damage),
                Enhance.Magnitude(rules, traits, Mutation.Bloated, Fields.Radius),
                rules.PowerOf(Mutation.Bloated, Fields.Delay), traits.Stars,
                rules.PrefabOf(Mutation.Bloated, Fields.WarningEffect),
                rules.PrefabOf(Mutation.Bloated, Fields.BlastEffect));
        }

        // Runs on the VICTIM's owner. The devourer is read from the prey's own ZDO marker (set at the bite, survives a
        // hand-over), so a prey devoured across owners still feeds the right devourer; the grant is routed to its owner.
        private static void Feed(Snapshot snap)
        {
            if (snap.Devourer == ZDOID.None)
            {
                return; // this creature was not devoured (a plain death, or a kill during the devourer's cooldown)
            }
            GameObject? go = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(snap.Devourer) : null;
            Character? devourer = go != null ? go.GetComponent<Character>() : null;
            EliteController? kc = devourer != null ? devourer.GetComponent<EliteController>() : null;
            if (devourer == null || kc == null || !kc.Ready || !kc.Traits.Has(Mutation.Devouring))
            {
                Log.Diag($"{snap.Victim.name}: devourer not resolvable here; cross-owner feed skipped");
                return;
            }
            Grant(snap, devourer, kc);
        }

        // The instant kill always lands the killing blow, so the devourer keeps the whole meal: a share of the prey's max
        // health and of its damage, both permanent. The cooldown is started where this is banked (CreatureRpc.Bank).
        private static void Grant(Snapshot snap, Character devourer, EliteController kc)
        {
            float health = snap.MaxHealth
                * Enhance.Magnitude(kc.Rules, kc.Traits, Mutation.Devouring, Fields.AbsorbHealth) / 100f;
            float damage = EstimateDamage(snap.Victim, snap.MaxHealth)
                * Enhance.Magnitude(kc.Rules, kc.Traits, Mutation.Devouring, Fields.AbsorbDamage) / 100f;
            Log.Diag($"{devourer.name} devoured {snap.Victim.name}: +hp={health:0.#} +dmg={damage:0.#}");
            CreatureRpc.Absorb(devourer, health, damage);
        }

        private static float EstimateDamage(Character victim, float maxHealth)
        {
            Humanoid? humanoid = victim as Humanoid;
            ItemDrop.ItemData? weapon = humanoid != null ? humanoid.GetCurrentWeapon() : null;
            float damage = weapon != null ? weapon.GetDamage().GetTotalDamage() : 0f;
            return damage > 0f ? damage : maxHealth * 0.05f;
        }
    }
}
