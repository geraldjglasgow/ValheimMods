using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Leeching's passive half: regenerates a share of maximum health every second, the rate taken from the creature's
    /// biome rules. The other half - healing off damage dealt - is applied in the damage patch, where the amount dealt
    /// is known. Attached on every machine but heals only while it owns the creature (a heal is an owner-only write),
    /// so ownership changing hands moves the regen to the new owner with it.
    /// </summary>
    public sealed class LeechBehaviour : MonoBehaviour
    {
        private Character _character = null!;
        private EliteController _controller = null!;
        private float _regenPercent;
        private float _timer;

        private void Start()
        {
            _character = GetComponent<Character>();
            _controller = GetComponent<EliteController>();
            _regenPercent = _controller != null
                ? Scaling.Enhance.Magnitude(_controller.Rules, _controller.Traits, Mutation.Leeching, Fields.Regen)
                : 0f;
        }

        private void Update() => Guard.Run("LeechBehaviour.Update", Step);

        private void Step()
        {
            if (_character == null || _character.IsDead() || _controller == null || !_controller.IsOwner())
            {
                return; // owner-only: a Heal is a write and only sticks on the owner
            }
            _timer += Time.deltaTime;
            if (_timer < 1f)
            {
                return;
            }
            _timer -= 1f;
            float amount = _character.GetMaxHealth() * (_regenPercent / 100f);
            if (amount > 0f && _character.GetHealth() < _character.GetMaxHealth())
            {
                _character.Heal(amount, showText: false);
            }
        }
    }
}
