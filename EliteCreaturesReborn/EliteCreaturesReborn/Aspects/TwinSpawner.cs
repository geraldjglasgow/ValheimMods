using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Twin's arrival: on the boss's owner, the moment the boss is first rolled, a second copy of it appears beside it
    /// with the same stars and aspect, and each is written into the other's ZDO as its partner. The link is what
    /// <see cref="TwinLink"/> shares health through, and what stops the twin - born already linked - bringing a twin of
    /// its own.
    /// </summary>
    internal static class TwinSpawner
    {
        private const float Distance = 4f;

        public static void Spawn(EliteController boss)
        {
            ZDO zdo = boss.View.GetZDO();
            if (AspectStore.GetTwin(zdo) != ZDOID.None)
            {
                return; // already a pair
            }
            ZDOID bossId = zdo.m_uid;
            Vector3 pos = SpawnPlace.Around(boss.transform.position, Random.Range(0f, 360f), Distance);
            CreatureTraits traits = new CreatureTraits(boss.Traits.Stars, Aspect.Twin);
            Character? twin = BossCopy.Make(boss, pos, traits, copy => AspectStore.SetTwin(copy, bossId));
            if (twin != null)
            {
                AspectStore.SetTwin(zdo, twin.GetZDOID());
                Log.Diag($"{boss.name}: twin spawned, {traits.Stars} star(s)");
            }
        }
    }
}
