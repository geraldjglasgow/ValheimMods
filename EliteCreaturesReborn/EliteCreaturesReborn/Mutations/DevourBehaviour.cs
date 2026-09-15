using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Rules;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Devouring's owner-side AI shaping. It slows itself as its accumulated health grows, and once its accumulated
    /// per-hit damage passes a share of a player's health it hunts players from then on, permanently. Until then it
    /// ignores players and eats creatures - the instant kill and its cooldown live in the hit path
    /// (<see cref="Patches.DevourHitPatch"/>), not here. The one exception to ignoring players is provocation: when a
    /// player attacks it, the hit path calls <see cref="Provoke"/>, which turns it on that player for a short memory;
    /// while that memory is live <see cref="Patches.EnemyPatch"/> treats the player as an enemy so vanilla keeps the
    /// target, and when it lapses vanilla drops the player and it goes back to feeding. Attached on every machine, it
    /// acts only while it owns the creature, so a hand-over just moves which machine drives it.
    /// </summary>
    public sealed class DevourBehaviour : MonoBehaviour
    {
        /// <summary>Seconds it keeps fighting a player after the last hit it took from them; refreshed on every hit. A
        /// judgement call - long enough to survive a brief lull mid-fight, short enough that breaking off loses it.</summary>
        private const float ProvokeWindow = 6f;

        private EliteController _controller = null!;
        private Character _character = null!;
        private MonsterAI _ai = null!;
        private float _slowPer100;
        private float _threshold = 0.333f;
        private float _provokedUntil;

        public bool HuntsPlayers { get; private set; }

        /// <summary>True while a recent player attack still has it fighting that player rather than eating creatures.</summary>
        public bool IsProvoked => Time.time < _provokedUntil;

        private void Start()
        {
            _controller = GetComponent<EliteController>();
            _character = GetComponent<Character>();
            _ai = GetComponent<MonsterAI>();
            _slowPer100 = _controller.Rules.PowerOf(Mutation.Devouring, Fields.SlowPer100Health);
            _threshold = _controller.Rules.PowerOf(Mutation.Devouring, Fields.PlayerThreshold);
        }

        private void Update() => Guard.Run("DevourBehaviour.Update", Step);

        private void Step()
        {
            if (_controller == null || _character == null || _character.IsDead() || !_controller.IsOwner())
            {
                return; // owner-only: only the machine that owns it may act on it or write its state
            }
            ApplySlow();
            UpdateThreat();
        }

        private void ApplySlow()
        {
            float eaten = TraitStore.GetDevouredHealth(_controller.View.GetZDO());
            float bonus = -(_slowPer100 / 100f) * (eaten / 100f);
            _controller.RefreshMovement(bonus);
        }

        private void UpdateThreat()
        {
            float perHit = TraitStore.GetDevouredDamage(_controller.View.GetZDO());
            HuntsPlayers = perHit > _threshold * PlayerReference.MaxHealth();
            if (HuntsPlayers && _ai != null)
            {
                _ai.SetHuntPlayer(hunt: true);
            }
        }

        /// <summary>
        /// A player attacked it: turn on that player now and remember them for a while. Runs on the devourer's owner (the
        /// hit path calls this on the victim's owner, which the devourer is). The target is forced because it may already
        /// be locked onto a prey creature, and vanilla only re-targets an attacker when it has none.
        /// </summary>
        public void Provoke(Character player)
        {
            _provokedUntil = Time.time + ProvokeWindow;
            if (_ai == null || player == null)
            {
                return;
            }
            Traverse ai = Traverse.Create(_ai);
            ai.Field("m_targetCreature").SetValue(player);
            ai.Field("m_lastKnownTargetPos").SetValue(player.transform.position);
        }
    }
}
