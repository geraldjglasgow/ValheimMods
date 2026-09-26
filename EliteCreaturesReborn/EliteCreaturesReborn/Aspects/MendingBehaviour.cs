using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Mending: heals a share of maximum health every second, in combat and out - unlike Leeching's regeneration it never
    /// pauses when hit, because the aspect is a damage check, not a top-up. The game's own slow regeneration still runs
    /// underneath. Attached on every machine, heals only while it owns the boss (a heal is an owner-only write), so the
    /// healing moves with ownership. The rate is read live, so a rule-file edit retunes a fight in progress.
    /// </summary>
    public sealed class MendingBehaviour : MonoBehaviour
    {
        private Character _character = null!;
        private EliteController _controller = null!;
        private float _timer;

        private void Start()
        {
            _character = GetComponent<Character>();
            _controller = GetComponent<EliteController>();
        }

        private void Update() => Guard.Run("MendingBehaviour.Update", Step);

        private void Step()
        {
            if (_character == null || _controller == null || !_controller.IsOwner() || _character.IsDead())
            {
                return;
            }
            _timer += Time.deltaTime;
            if (_timer < 1f)
            {
                return;
            }
            _timer -= 1f;
            float amount = _character.GetMaxHealth() * AspectMath.Power(Aspect.Mending, Fields.Regen) / 100f;
            if (amount > 0f && _character.GetHealth() < _character.GetMaxHealth())
            {
                _character.Heal(amount, showText: false);
            }
        }
    }
}
