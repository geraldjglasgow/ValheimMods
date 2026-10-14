using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// Carries the Bat-fiend forward through its Raking Pounce and its Bloodletting Bite. The clips leap and lunge on the
    /// spot (the workshop's clips carry no root motion); this moves the creature along each clip's own travel curve
    /// (<see cref="BatFiendNumbers.PounceTravel"/>: 2.4 m between 0.37 and 0.60 s; <see cref="BatFiendNumbers.BiteTravel"/>:
    /// 1.2 m between 0.22 and 0.47 s; at scale 1, times the body's scale) through the game's root-motion hook, the crypt
    /// mimic's way, so the move runs on the owner and replicates like any creature's. It follows the clip's own time, so
    /// a faster attack (Mad) moves faster, and a leap cut short (staggered) stops where it was.
    /// </summary>
    public sealed class BatFiendLeap : MonoBehaviour
    {
        private const float GameRootMotionScale = 55f;   // Character.ApplyRootMotion: velocity = accumulated motion * 55

        private sealed class Move
        {
            public readonly int State;
            public readonly float[] Travel;
            public readonly float Frames;

            public Move(string clip, float[] travel, float frames) =>
                (State, Travel, Frames) = (Animator.StringToHash(clip), travel, frames);
        }

        private static readonly Move[] Moves =
        {
            new Move("RakingPounce", BatFiendNumbers.PounceTravel, BatFiendNumbers.PounceFrames),
            new Move("BloodlettingBite", BatFiendNumbers.BiteTravel, BatFiendNumbers.BiteFrames),
        };

        private Character _character = null!;
        private Animator? _animator;
        private ZNetView _nview = null!;
        private Move? _moving;
        private float _travelled, _lastTime;

        private void Awake()
        {
            _character = GetComponent<Character>();
            _animator = GetComponentInChildren<Animator>();
            _nview = GetComponent<ZNetView>();
        }

        private void FixedUpdate()
        {
            if (_animator == null || _nview == null || !_nview.IsValid() || !_nview.IsOwner())
            {
                return;
            }
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            Move? move = Of(state.shortNameHash);
            float time = Mathf.Clamp01(state.normalizedTime);
            if (move != _moving || time < _lastTime)
            {
                (_moving, _travelled) = (move, 0f); // another clip, or the same one started again
            }
            _lastTime = time;
            if (move != null)
            {
                Carry(Distance(move.Travel, time * move.Frames) * _animator.transform.lossyScale.z);
            }
        }

        private static Move? Of(int state)
        {
            foreach (Move move in Moves)
            {
                if (move.State == state)
                {
                    return move;
                }
            }
            return null;
        }

        /// <summary>Moves on to <paramref name="target"/> metres from the clip's start, forward only.</summary>
        private void Carry(float target)
        {
            float step = target - _travelled;
            if (step > 0f)
            {
                // What the game turns into velocity for one physics step, so the creature covers exactly `step`.
                _character.AddRootMotion(transform.forward * (step / (GameRootMotionScale * Time.fixedDeltaTime)));
                _travelled = target;
            }
        }

        /// <summary>Metres travelled at a (fractional) frame of the clip, at scale 1.</summary>
        private static float Distance(float[] travel, float frame)
        {
            int at = Mathf.FloorToInt(frame);
            if (at >= travel.Length - 1)
            {
                return travel[travel.Length - 1];
            }
            return Mathf.Lerp(travel[at], travel[at + 1], frame - at);
        }
    }
}
