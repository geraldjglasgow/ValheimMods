using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The wand's three-move combo and the charged release (user, 2026-10-11: "1. brings wand up with slight flick motion
    /// at end ... 2. brings wand near left side of head and then points it at enemy 3. brings wand back near right side of
    /// head with wand facing behind character, then brings it forward"). The player's animator has no spare attack states,
    /// so all four motions are segments of one workshop clip on the charged staff's cast state (<see cref="WandAnimations"/>),
    /// each with its own <c>Hit</c> for the bolt. When a bolt's cast starts, the caster's machine tells every player near
    /// which segment (<see cref="Send"/>, a routed RPC, the caster too, handled at once there), and each machine cross-fades
    /// that player's upper body into the cast state at the segment's start and out again at its end (<see cref="Tick"/>),
    /// so everyone sees the same move; the game's own trigger for the state is held back (<see cref="WandAnimations"/>),
    /// since it would start the clip from its first segment. Which move is the game's own choice, its sword combo (user,
    /// 2026-10-11: "this again needs to work similarly to how clicking attacks with swords, whatever system they use"): the
    /// wand's bolt is a three-level chained attack, so the game picks the level as it picks a sword's swing (the next level
    /// for a click during a move once its <c>Chain</c> point has passed, or within 0.2 s of its end; the first again after
    /// that), and the level is the move. A charged release is the first move from the back; a cast turned into a charge sends
    /// every machine the upper body into the charged staff's raise (<see cref="ToCharge"/>).
    /// </summary>
    public static class WandMoves
    {
        public const int Charged = 3;
        public const int ToCharge = 4;
        private const string Rpc = "EliteEquipment_WandMove";
        private const float Range = 60f;
        private const float Wait = 1f;
        private const float FadeIn = 0.05f;
        private const float FadeFromCharge = 0.08f;
        private const float FadeOut = 0.15f;
        // The casts play slower than authored (user, 2026-10-11: "slow the animation down by like 20%"): the animator's speed
        // while a cast plays, which the game shares with every machine and sets back to 1 once the attack is over.
        private const float CastSpeed = 0.8f;
        private const float ToChargeFade = 0.1f;

        // The segments of ee_anim_wand_casts, clip seconds (the workshop's out/segments.json, 2026-10-11): moves 1 to 3 and
        // the charged release; their Hits at 0.42, 1.39, 2.34 and 3.27. Each ends on the ready stance, held still 0.25 s.
        private const float ClipLength = 5.2f;
        private static readonly float[] Starts = { 0f, 0.97f, 1.94f, 3.05f };
        private static readonly float[] Ends = { 0.72f, 1.69f, 2.68f, 3.57f };

        /// <summary>
        /// Where a click may start the next move, as a sword's <c>Chain</c> point: just after moves 1 and 2 throw their bolts
        /// (their Hits at 0.42 and 1.39 s). The last move has none: the game ends a sword's combo there too.
        /// </summary>
        public static readonly float[] ChainPoints = { 0.47f, 1.44f };
        private static readonly int Raise = Animator.StringToHash("staff_charge_start");

        private static readonly int Casts = Animator.StringToHash("staff_charge_attack");
        private static readonly int None = Animator.StringToHash("None");

        private sealed class Move
        {
            public Player Player;
            public int Segment;
            public float Since;
            public bool Jumped;
            public bool Seen;
        }

        private static readonly List<Move> moves = new List<Move>();

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => ZRoutedRpc.instance?.Register<ZDOID, int>(Rpc, Receive);
        }

        public static void Send(Player caster, int segment) =>
            NearbyCast.Send(caster.transform.position, Range, true, Rpc, caster.GetZDOID(), segment);

        private static void Receive(long sender, ZDOID id, int segment)
        {
            GameObject found = id.IsNone() || ZNetScene.instance == null ? null : ZNetScene.instance.FindInstance(id);
            Player player = found != null ? found.GetComponent<Player>() : null;
            if (player == null || segment < 0 || segment > ToCharge)
                return;
            moves.RemoveAll(m => m.Player == player);
            moves.Add(new Move { Player = player, Segment = segment, Since = Time.time });
        }

        /// <summary>Every frame on a machine that draws: each announced move started, and ended at its segment's end.</summary>
        public static void Tick()
        {
            for (int i = moves.Count - 1; i >= 0; i--)
            {
                if (!Step(moves[i]))
                    moves.RemoveAt(i);
            }
        }

        private static bool Step(Move move)
        {
            Animator animator = move.Player != null ? move.Player.m_animator : null;
            int layer = animator != null ? animator.GetLayerIndex("upperbody") : -1;
            if (layer < 0)
                return false;
            if (move.Segment == ToCharge)
            {
                animator.CrossFadeInFixedTime(Raise, ToChargeFade, layer);
                return false;
            }
            return move.Jumped ? Leave(move, animator, layer) : Enter(move, animator, layer);
        }

        /// <summary>The upper body into the cast state at the segment's start (from the stance, a move, or the charge's hold).</summary>
        private static bool Enter(Move move, Animator animator, int layer)
        {
            float fade = move.Segment == Charged ? FadeFromCharge : FadeIn;
            animator.CrossFadeInFixedTime(Casts, fade, layer, Starts[move.Segment]);
            if (move.Player == Player.m_localPlayer)
                animator.speed = CastSpeed;
            move.Jumped = true;
            return true;
        }

        private static bool Leave(Move move, Animator animator, int layer)
        {
            if (animator.IsInTransition(layer))
                return Time.time - move.Since < Wait;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);
            // The animator steps with the physics, not every frame: until it has taken the cross-fade the state is the old
            // one, which is not the move having ended.
            if (state.shortNameHash != Casts)
                return !move.Seen && Time.time - move.Since < Wait;
            move.Seen = true;
            if (state.normalizedTime * ClipLength < Ends[move.Segment])
                return true;
            animator.CrossFadeInFixedTime(None, FadeOut, layer);
            return false;
        }
    }
}
