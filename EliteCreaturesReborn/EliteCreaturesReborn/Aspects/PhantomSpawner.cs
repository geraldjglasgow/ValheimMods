using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Phantom's arrival: on the boss's owner, the moment the boss is first rolled, its copies appear in an even ring
    /// around it - same prefab, stars, size and name - each marked in its own ZDO with the boss it belongs to. The mark
    /// is what makes a copy a copy on every machine (<see cref="PhantomBody"/>), and what lets the boss's death find and
    /// dismiss them (<see cref="PhantomReaper"/>).
    /// </summary>
    internal static class PhantomSpawner
    {
        private const float Radius = 5f;

        /// <summary>A sanity cap on the copy count, whatever the rule file says.</summary>
        private const int MaxCopies = 16;

        public static void Spawn(EliteController boss)
        {
            int copies = Mathf.Clamp(Mathf.RoundToInt(AspectMath.Power(Aspect.Phantom, Fields.Copies)), 0, MaxCopies);
            ZDOID bossId = boss.View.GetZDO().m_uid;
            float start = Random.Range(0f, 360f);
            CreatureTraits traits = new CreatureTraits(boss.Traits.Stars, Aspect.Phantom);
            for (int i = 0; i < copies; i++)
            {
                Vector3 pos = SpawnPlace.Around(boss.transform.position, start + 360f * i / copies, Radius);
                BossCopy.Make(boss, pos, traits, copy => AspectStore.SetPhantomOf(copy, bossId));
            }
            Log.Diag($"{boss.name}: {copies} phantom copies spawned");
        }
    }
}
