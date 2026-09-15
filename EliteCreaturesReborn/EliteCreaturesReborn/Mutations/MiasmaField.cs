using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Miasmic's trail, split the way the spec's multiplayer rule demands: the owner DECIDES where clouds are (it emits
    /// a drop once per configured second of movement, into the creature's replicated ZDO), and EVERY machine DRAWS them
    /// (each reads the trail and reconstructs the same clouds locally). A cloud built on the owner also carries damage;
    /// one built on a remote client is visual only. This component therefore runs on every machine, and gates only the
    /// emit on live ownership, so a creature handed to a new owner simply keeps being fed drops by whoever owns it now.
    /// </summary>
    public sealed class MiasmaField : MonoBehaviour
    {
        private EliteController _controller = null!;
        private Character _character = null!;
        private Vector3 _lastPosition;
        private float _movingTime;
        private long _lastSeenId;
        private float _interval = 1f;
        private float _life = 6f;
        private float _damage = 5f;
        private float _radius = 4f;
        private string _effect = "";
        private readonly List<MiasmaTrail.Drop> _scratch = new List<MiasmaTrail.Drop>();

        private void Start()
        {
            _controller = GetComponent<EliteController>();
            _character = GetComponent<Character>();
            _lastPosition = transform.position;
            Read();
        }

        private void Read()
        {
            if (_controller == null)
            {
                return;
            }
            CreatureTraits traits = _controller.Traits;
            float perSecond = Scaling.Enhance.Magnitude(_controller.Rules, traits, Mutation.Miasmic, Fields.CloudsPerSecond);
            _interval = perSecond > 0f ? 1f / perSecond : float.MaxValue;
            _life = _controller.Rules.PowerOf(Mutation.Miasmic, Fields.CloudLife);
            _damage = Scaling.Enhance.Magnitude(_controller.Rules, traits, Mutation.Miasmic, Fields.CloudDamage);
            _radius = _controller.Rules.PowerOf(Mutation.Miasmic, Fields.CloudRadius);
            _effect = _controller.Rules.PrefabOf(Mutation.Miasmic, Fields.CloudEffect);
        }

        private void Update() => Guard.Run("MiasmaField.Update", Step);

        private void Step()
        {
            if (_controller == null || _character == null || !_controller.View.IsValid())
            {
                return;
            }
            if (_controller.IsOwner()) // OWNER decides the trail
            {
                Emit();
            }
            Reconstruct(); // EVERY machine draws it
        }

        /// <summary>Owner only: measure actual movement and, once per interval, append a drop to the replicated trail.</summary>
        private void Emit()
        {
            if (_character.IsDead())
            {
                return;
            }
            float moved = Vector3.Distance(transform.position, _lastPosition);
            _lastPosition = transform.position;
            if (moved > 0.05f)
            {
                _movingTime += Time.deltaTime;
            }
            if (_movingTime >= _interval)
            {
                _movingTime = 0f;
                Append(DropPoint());
            }
        }

        /// <summary>
        /// The drop falls at the creature's BACK - offset behind its facing by roughly its own body width, never at its
        /// centre - so fighting a Miasmic creature head-on does not poison you; you are poisoned because you chased it,
        /// circled it, or backed into ground it has already crossed. Body width is taken as the collider's diameter.
        /// </summary>
        private Vector3 DropPoint()
        {
            float bodyWidth = Mathf.Max(0.5f, _character.GetRadius() * 2f);
            return transform.position - transform.forward * bodyWidth;
        }

        private void Append(Vector3 pos)
        {
            ZDO zdo = _controller.View.GetZDO();
            List<MiasmaTrail.Drop> drops = MiasmaTrail.Read(zdo);
            MiasmaTrail.TrimExpired(drops, _life);
            drops.Add(new MiasmaTrail.Drop { Id = MiasmaTrail.NextId(zdo), Pos = pos, TimeMs = NetTime.NowMs() });
            MiasmaTrail.Write(zdo, drops);
        }

        /// <summary>Every machine: spawn each not-yet-seen drop once, damaging only where this machine owns the creature.</summary>
        private void Reconstruct()
        {
            _scratch.Clear();
            _scratch.AddRange(MiasmaTrail.Read(_controller.View.GetZDO()));
            bool owner = _controller.IsOwner();
            foreach (MiasmaTrail.Drop drop in _scratch)
            {
                if (drop.Id <= _lastSeenId)
                {
                    continue;
                }
                _lastSeenId = drop.Id;
                float remaining = _life - NetTime.SecondsSince(drop.TimeMs);
                if (remaining > 0.1f)
                {
                    PoisonCloud.Spawn(drop.Pos, remaining, _damage, _radius, _effect, owner);
                }
            }
        }
    }
}
