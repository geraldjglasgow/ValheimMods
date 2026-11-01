using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// One creature's animators as the server sets them (<see cref="AlwaysAnimate"/>): found once, when the server first
    /// meets the creature (its wake, or the scan for one that woke before HighSeat ran), each with the culling it shipped
    /// with, and what the wake rule (<see cref="AnimRule"/>) reads that never changes: its saddle, how far from the camera
    /// the game plays its footsteps, whether it is a boss. The modes: Full is AlwaysAnimate. Quiet is CullUpdateTransforms
    /// (state machine, parameters, events and root motion run; IK and bone writes stop while no renderer is seen), except
    /// for an animator shipped to always animate, which stays so, as its maker wanted it animated unseen. Rest, for a copy
    /// the server does not run, is no animation at all on a dedicated server and the shipped culling on a hosted game.
    /// Shipped gives every animator its own culling back. Animators destroyed since (a model a mod replaced) are passed
    /// over. Kept by the creature's instance id; destroyed creatures are forgotten once every scan (<see cref="Prune"/>).
    /// </summary>
    internal sealed class CreatureAnim
    {
        public enum Mode : byte
        {
            Unset,
            Full,
            Quiet,
            Rest,
            Shipped,
        }

        private static readonly Dictionary<int, CreatureAnim> All = new Dictionary<int, CreatureAnim>();
        private static readonly List<Animator> Found = new List<Animator>();
        private static readonly List<int> Dead = new List<int>();

        public readonly BaseAI Ai;
        public readonly Character Body;
        public readonly Sadle Saddle;
        public readonly float StepRange;
        public readonly bool Boss;

        /// <summary>When something last needed its bones (<see cref="AnimRule"/>, <see cref="AlwaysAnimate.Wake"/>).</summary>
        public float LastNeed = float.NegativeInfinity;

        private readonly Animator[] _animators;
        private readonly AnimatorCullingMode[] _shipped;

        public Mode Current { get; private set; }

        private CreatureAnim(BaseAI ai)
        {
            Ai = ai;
            Body = ai.m_character;
            Saddle = ai.m_tamable != null ? ai.m_tamable.m_saddle : null;
            FootStep steps = ai.GetComponent<FootStep>();
            StepRange = steps != null ? steps.m_footstepCullDistance : 0f;
            Boss = Body != null && Body.IsBoss();
            ai.GetComponentsInChildren(true, Found);
            _animators = Found.ToArray();
            Found.Clear();
            _shipped = new AnimatorCullingMode[_animators.Length];
            for (int i = 0; i < _animators.Length; i++)
            {
                _shipped[i] = _animators[i].cullingMode;
            }
        }

        /// <summary>The creature's record, made the first time it is asked for.</summary>
        public static CreatureAnim Of(BaseAI ai)
        {
            int id = ai.GetInstanceID();
            if (!All.TryGetValue(id, out CreatureAnim anim))
            {
                anim = new CreatureAnim(ai);
                All[id] = anim;
            }
            return anim;
        }

        /// <summary>The record when there is one; null for a creature HighSeat never set.</summary>
        public static CreatureAnim Find(BaseAI ai) => All.TryGetValue(ai.GetInstanceID(), out CreatureAnim anim) ? anim : null;

        /// <summary>Sets every animator for <paramref name="mode"/>; nothing when it is set so already.</summary>
        public void Set(Mode mode)
        {
            if (mode == Current)
            {
                return;
            }
            Current = mode;
            bool dedicated = ZNet.instance != null && ZNet.instance.IsDedicated();
            for (int i = 0; i < _animators.Length; i++)
            {
                Animator animator = _animators[i];
                if (animator != null)
                {
                    animator.cullingMode = Culling(mode, _shipped[i], dedicated);
                }
            }
            AnimStats.Switched();
        }

        /// <summary>Forgets destroyed creatures (once a scan of them all).</summary>
        public static void Prune()
        {
            foreach (KeyValuePair<int, CreatureAnim> entry in All)
            {
                if (entry.Value.Ai == null)
                {
                    Dead.Add(entry.Key);
                }
            }
            foreach (int id in Dead)
            {
                All.Remove(id);
            }
            Dead.Clear();
        }

        private static AnimatorCullingMode Culling(Mode mode, AnimatorCullingMode shipped, bool dedicated)
        {
            switch (mode)
            {
                case Mode.Full:
                    return AnimatorCullingMode.AlwaysAnimate;
                case Mode.Quiet:
                    return shipped == AnimatorCullingMode.AlwaysAnimate ? shipped : AnimatorCullingMode.CullUpdateTransforms;
                case Mode.Rest:
                    return dedicated ? AnimatorCullingMode.CullCompletely : shipped;
                default:
                    return shipped;
            }
        }
    }
}
