using HarmonyLib;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// The Bat-fiend's status effects (<see cref="BatFiendScreech"/>, <see cref="BatFiendChill"/>) in every object
    /// database, on every peer: an attack item's status effect reaches its victim as a name hash the victim's owner looks
    /// up there, so each must be found on whichever machine owns the victim.
    /// </summary>
    internal static class BatFiendStatuses
    {
        private static bool _patched;

        /// <summary>Puts them in this object database and every one made from now on.</summary>
        public static void Register(Harmony harmony)
        {
            Add(ObjectDB.instance);
            if (_patched)
            {
                return;
            }
            _patched = true;
            var after = new HarmonyMethod(typeof(BatFiendStatuses), nameof(AfterDb));
            harmony.Patch(AccessTools.Method(typeof(ObjectDB), "Awake"), postfix: after);
            harmony.Patch(AccessTools.Method(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), postfix: after);
        }

        private static void AfterDb(ObjectDB __instance) => Add(__instance);

        private static void Add(ObjectDB? db)
        {
            if (db == null)
            {
                return;
            }
            foreach (StatusEffect effect in new[] { BatFiendScreech.Template(), BatFiendChill.Template() })
            {
                if (db.GetStatusEffect(effect.NameHash()) == null)
                {
                    db.m_StatusEffects.Add(effect);
                }
            }
        }
    }
}
