using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace StatusTemplates
{
    /// <summary>
    /// A mod's status effects in the game's status effect table (<c>ObjectDB.m_StatusEffects</c>): in the live object
    /// database now and in every one the game wakes or copies later, on every peer. Only an effect that travels by name
    /// hash needs a place there: a hit's (<c>HitData.m_statusEffectHash</c>) or an item's, which the game looks up on the
    /// machine that owns the character hit. An effect only ever handed to a character as an object
    /// (<see cref="StatusTemplate{T}.Add"/>) needs none.
    /// <para>
    /// The game's lookup (<c>ObjectDB.GetStatusEffect</c>) walks the list and <c>Awake</c> builds no index of it, so a
    /// postfix is in time. An entry of the same name that is not the current object (left by an earlier load of the mod,
    /// such as a DevBridge reload) is replaced; the object itself is never added twice. Patched once per merged copy;
    /// registering the same effect again (a call made on every world load) changes nothing.
    /// </para>
    /// </summary>
    public static class StatusTable
    {
        private static readonly List<Func<StatusEffect>> sources = new List<Func<StatusEffect>>();
        private static bool patched;

        /// <summary>Each effect, asked for on every database: pass a template's <c>Get</c>.</summary>
        public static void Register(Harmony harmony, params Func<StatusEffect>[] effects)
        {
            foreach (Func<StatusEffect> effect in effects)
            {
                if (!sources.Contains(effect))
                {
                    sources.Add(effect);
                }
            }
            Install(harmony);
        }

        /// <summary>Puts one effect in a table: replaces a stale entry of its name, adds it if missing.</summary>
        public static void Put(List<StatusEffect> table, StatusEffect effect)
        {
            int hash = effect.NameHash();
            table.RemoveAll(other => other != null && !ReferenceEquals(other, effect) && other.NameHash() == hash);
            if (!table.Contains(effect))
            {
                table.Add(effect);
            }
        }

        private static void Install(Harmony harmony)
        {
            if (!patched)
            {
                patched = true;
                var after = new HarmonyMethod(typeof(StatusTable), nameof(AfterDb));
                harmony.Patch(AccessTools.Method(typeof(ObjectDB), "Awake"), postfix: after);
                harmony.Patch(AccessTools.Method(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), postfix: after);
            }
            AddTo(ObjectDB.instance);
        }

        private static void AfterDb(ObjectDB __instance) => AddTo(__instance);

        // A throw here would leave the game's database half built, so each source fails alone, logged.
        private static void AddTo(ObjectDB? db)
        {
            if (db == null)
            {
                return;
            }
            foreach (Func<StatusEffect> source in sources)
            {
                try
                {
                    Put(db.m_StatusEffects, source());
                }
                catch (Exception e)
                {
                    Debug.LogError($"[StatusTemplates] a status effect was not registered: {e}");
                }
            }
        }
    }
}
