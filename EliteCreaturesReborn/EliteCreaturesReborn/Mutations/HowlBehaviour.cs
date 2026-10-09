using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using EliteCreaturesReborn.Visuals;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Howling: when it turns on a player it howls, and the <see cref="MaxCalled"/> nearest creatures within
    /// <see cref="Reach"/> metres on its side (ones it does not count as enemies) that are hostile to that player take up
    /// the fight against them - again every <see cref="Cooldown"/> seconds while it still fights a player. It calls
    /// creatures already there, never new ones. Decided on its owner, which watches its target: it stamps the howl in
    /// the ZDO on the shared clock, so a new owner keeps the cooldown, and asks each called creature's own owner through
    /// the game's own "a shot landed near you" message, the one the game sends when an arrow lands by a creature: that
    /// owner alerts it and sets the player as its target unless it already has one, the game's way, peaceful worlds
    /// included. The howl itself is broadcast over the creature's own ZNetView, so every
    /// client holding it hears it. A tamed one never turns on a player, so it never howls; bosses are never called.
    /// Attached on every machine: each one needs the tell's handler, and only the live owner's gate opens.
    /// </summary>
    public sealed class HowlBehaviour : MonoBehaviour
    {
        /// <summary>Metres around it from which it calls creatures.</summary>
        public const float Reach = 30f;

        /// <summary>The most creatures one howl calls, the nearest first.</summary>
        public const int MaxCalled = 4;

        /// <summary>Seconds between two howls.</summary>
        public const float Cooldown = 45f;

        private const float CheckEvery = 0.5f;
        private const string Rpc = "ecr_howl";
        private const string HowlSound = "sfx_wolf_haul";
        private const string CallRpc = "OnNearProjectileHit";
        private static readonly string[] Keywords = { "howl", "haul", "wolf", "alert" };
        private static readonly int HowlAtHash = TraitKeys.HowlAt.GetStableHashCode();

        private Character _character = null!;
        private BaseAI _ai = null!;
        private EliteController? _controller;
        private float _nextCheck;
        private readonly List<BaseAI> _callable = new List<BaseAI>();
        private Comparison<BaseAI> _nearer = null!;

        private void Start() => Guard.Run("HowlBehaviour.Start", Setup);

        private void Setup()
        {
            Character character = GetComponent<Character>();
            EliteController controller = GetComponent<EliteController>();
            BaseAI ai = GetComponent<BaseAI>();
            if (character == null || controller == null || ai == null || controller.View == null || !controller.View.IsValid())
            {
                return;
            }
            _character = character;
            _ai = ai;
            _controller = controller;
            _nearer = (a, b) => Away(a).CompareTo(Away(b));
            controller.View.Register<Vector3>(Rpc, OnTell); // every machine: each one hears the broadcast
        }

        private void Update()
        {
            if (_controller != null && Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + CheckEvery;
                Guard.Run("HowlBehaviour.Update", static self => self.Check(), this);
            }
        }

        private void Check()
        {
            if (!_controller!.IsOwner() || _character.IsDead() || _character.IsTamed() || !_ai.IsAlerted())
            {
                return;
            }
            Character? target = _ai.GetTargetCreature();
            if (target != null && target.IsPlayer() && !target.IsDead() && Rested())
            {
                Howl(target);
            }
        }

        /// <summary>The cooldown, read from the shared-clock stamp in the ZDO, so a new owner honours the old one's.</summary>
        private bool Rested()
        {
            long last = _controller!.View.GetZDO().GetLong(HowlAtHash);
            return last <= 0L || NetTime.SecondsSince(last) >= Cooldown;
        }

        private void Howl(Character player)
        {
            _controller!.View.GetZDO().Set(TraitKeys.HowlAt, NetTime.NowMs());
            _controller.View.InvokeRPC(ZRoutedRpc.Everybody, Rpc, _character.GetCenterPoint());
            int called = Call(player);
            if (Log.Diagnostics)
            {
                Log.Diag($"{name} howled at {player.GetHoverName()}: {called} creature(s) within {Reach:0}m called");
            }
        }

        private int Call(Character player)
        {
            _callable.Clear();
            foreach (BaseAI ai in BaseAI.BaseAIInstances)
            {
                if (Answers(ai, player))
                {
                    _callable.Add(ai);
                }
            }
            _callable.Sort(_nearer);
            int called = Mathf.Min(_callable.Count, MaxCalled);
            for (int i = 0; i < called; i++)
            {
                _callable[i].m_nview.InvokeRPC(CallRpc, transform.position, Reach, player.GetZDOID());
            }
            _callable.Clear();
            return called;
        }

        private float Away(BaseAI ai) => (ai.transform.position - transform.position).sqrMagnitude;

        // A monster, alive, wild and no boss, in reach, on its side and hostile to the player.
        private bool Answers(BaseAI ai, Character player)
        {
            if (!(ai is MonsterAI) || ai.m_nview == null || !ai.m_nview.IsValid())
            {
                return false;
            }
            Character other = ai.m_character;
            return other != null && other != _character
                && !other.IsDead() && !other.IsBoss() && !other.IsTamed()
                && Vector3.Distance(other.transform.position, transform.position) <= Reach
                && !BaseAI.IsEnemy(_character, other) && BaseAI.IsEnemy(other, player);
        }

        private void OnTell(long sender, Vector3 at) => Guard.Run("HowlBehaviour.Tell", () =>
            CosmeticClone.Sound(EffectResolver.ResolveSound(HowlSound, Keywords, "Howling howl sound"), at));
    }
}
