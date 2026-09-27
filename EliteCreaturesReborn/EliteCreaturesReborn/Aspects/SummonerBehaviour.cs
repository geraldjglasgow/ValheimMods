using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Summoner: each time the boss has lost another `every` percent of its maximum health, it calls a wave. The waves
    /// already called are counted in the boss's ZDO (see <see cref="HealthStepBehaviour"/> for the rules every mark
    /// follows).
    /// </summary>
    public sealed class SummonerBehaviour : HealthStepBehaviour
    {
        /// <summary>How many whole `every` steps of health have been lost; the epsilon keeps 67% of 100 from reading 0.9999.</summary>
        protected override int Passed(float fraction)
        {
            float step = Mathf.Clamp(AspectMath.Power(Aspect.Summoner, Fields.Every), 1f, 100f) / 100f;
            return Mathf.FloorToInt((1f - fraction) / step + 0.0001f);
        }

        protected override int Counted(ZDO zdo) => AspectStore.GetWaves(zdo);

        protected override void Count(ZDO zdo, int passed) => AspectStore.SetWaves(zdo, passed);

        protected override void Fire() => SummonWave.Call(Character, Controller);
    }
}
