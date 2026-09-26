using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Summoner: each time the boss has lost another `every` percent of its maximum health, it calls a wave. The waves
    /// already called are counted in the boss's ZDO, so an ownership hand-over neither repeats nor skips one, and healing
    /// back above a threshold never re-arms it. A hit that crosses two thresholds calls two waves. Attached on every
    /// machine, it acts only on the boss's owner and never once the boss is at zero - no wave arrives over its body.
    /// </summary>
    public sealed class SummonerBehaviour : MonoBehaviour
    {
        private const float Interval = 0.25f;

        private Character _character = null!;
        private EliteController _controller = null!;
        private float _timer;

        private void Start()
        {
            _character = GetComponent<Character>();
            _controller = GetComponent<EliteController>();
        }

        private void Update() => Guard.Run("SummonerBehaviour.Update", Step);

        private void Step()
        {
            if (_character == null || _controller == null || !_controller.IsOwner() || _character.IsDead())
            {
                return;
            }
            _timer += Time.deltaTime;
            if (_timer < Interval)
            {
                return;
            }
            _timer = 0f;
            CallDueWaves();
        }

        private void CallDueWaves()
        {
            float fraction = _character.GetHealthPercentage();
            ZDO zdo = _controller.View.GetZDO();
            if (fraction <= 0f || zdo == null)
            {
                return;
            }
            int done = AspectStore.GetWaves(zdo);
            int due = WavesEarned(fraction);
            if (due <= done)
            {
                return;
            }
            AspectStore.SetWaves(zdo, due); // counted before calling, so a throw mid-wave can never repeat it
            for (int wave = done; wave < due; wave++)
            {
                SummonWave.Call(_character, _controller);
            }
        }

        /// <summary>How many whole `every` steps of health have been lost; the epsilon keeps 67% of 100 from reading 0.9999.</summary>
        private static int WavesEarned(float fraction)
        {
            float step = Mathf.Clamp(AspectMath.Power(Aspect.Summoner, Fields.Every), 1f, 100f) / 100f;
            return Mathf.FloorToInt((1f - fraction) / step + 0.0001f);
        }
    }
}
