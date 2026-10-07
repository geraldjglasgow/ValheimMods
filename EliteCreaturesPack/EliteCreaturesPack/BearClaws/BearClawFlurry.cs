using UnityEngine;

namespace EliteCreaturesPack.BearClaws
{
    /// <summary>
    /// A bear claw punch played as three swipes in the time of one: the game's punch starts as usual, the player's
    /// animator runs at <see cref="Speed"/>, and each next swipe starts the moment the game would let a combo go on (or
    /// the swipe ends), so the swipes alternate hands (left, right, left or right, left, right) the game's own way.
    /// Each swipe hits with its share of the punch (<see cref="BearClawSwipe.Shape"/>); hits freeze a third as long. Runs on the
    /// player's own machine, where attacks run; the animator's speed reaches every other peer through the game's own
    /// animation sync. Added to a player at its first claw punch.
    /// </summary>
    public sealed class BearClawFlurry : MonoBehaviour
    {
        /// <summary>
        /// The animator's speed through a flurry, so the three swipes take about 0.75 s from the click to the end (the
        /// user). The punches (Punchstep 1, 1.17 s, hit at 0.84; Punchstep 2, 1.2 s, hit at 0.90; the game plays them at
        /// 2x through their Speed events) make about 3.55 clip seconds for three: 0.57 s at 6.2x, plus three shortened
        /// freezes (0.05 s each when the swipes hit) and the frames each next swipe waits for.
        /// </summary>
        public const float Speed = 6.2f;

        /// <summary>
        /// The share of the punches' forward step a flurry keeps (the user: less lunge): three punches step about as far
        /// as the game's one.
        /// </summary>
        public const float Step = 1f / 3f;

        /// <summary>Longest a flurry may run before it stops, in case a swipe never ends.</summary>
        private const float Timeout = 3f;

        /// <summary>True while the flurry starts its own next swipe: the game's attack start lets that one through.</summary>
        public static bool StartingSwipe { get; private set; }

        /// <summary>The player whose flurry is swiping, on this machine (only its own player attacks here), else null.</summary>
        public static Character? Swiper { get; private set; }

        private Player player = null!;
        private Attack? swipe;
        private int index, punchLevel;
        private float started;

        public bool Active => swipe != null;

        /// <summary>Whether this attack is a flurry swipe after the first: it wears no durability, misses add no adrenaline.</summary>
        public bool IsExtra(Attack attack) => attack == swipe && index > 0;

        public static void Begin(Player player)
        {
            BearClawFlurry flurry = player.GetComponent<BearClawFlurry>() ?? player.gameObject.AddComponent<BearClawFlurry>();
            flurry.player = player;
            flurry.swipe = player.m_currentAttack;
            flurry.index = 0;
            flurry.punchLevel = flurry.swipe.m_currentAttackCainLevel;
            flurry.started = Time.time;
            Swiper = player;
            BearClawSwipe.Shape(flurry.swipe, flurry.punchLevel, 0);
            flurry.Hasten();
        }

        private void Update()
        {
            if (swipe == null)
            {
                return;
            }
            if (player == null || player.IsDead() || player.IsStaggering() || player.m_currentAttack != swipe
                || Time.time - started > Timeout)
            {
                End();
                return;
            }
            Hasten();
            if (swipe.CanStartChainAttack() || swipe.IsDone())
            {
                Next();
            }
        }

        /// <summary>The next swipe, or the end after the last; a swipe the game refuses (a dodge, no claws) ends it.</summary>
        private void Next()
        {
            if (index + 1 >= BearClawSwipe.Swipes)
            {
                if (swipe!.IsDone())
                {
                    End();
                }
                return;
            }
            if (!StartSwipe())
            {
                End();
                return;
            }
            swipe = player.m_currentAttack;
            index++;
            BearClawSwipe.Shape(swipe, punchLevel, index);
        }

        /// <summary>The game's own attack start, let through its block on attacks during a flurry.</summary>
        private bool StartSwipe()
        {
            StartingSwipe = true;
            try
            {
                return player.StartAttack(null, false);
            }
            finally
            {
                StartingSwipe = false;
            }
        }

        /// <summary>The flurry's speed, kept through the game's resets; a frozen frame (speed near 0) is left alone.</summary>
        private void Hasten()
        {
            if (player.m_animator.speed >= 0.01f && player.m_animator.speed != Speed)
            {
                player.m_zanim.SetSpeed(Speed);
            }
        }

        private void End()
        {
            swipe = null;
            if (Swiper == player)
            {
                Swiper = null;
            }
            if (player != null && player.m_animator.speed >= 0.01f)
            {
                player.m_zanim.SetSpeed(1f);
            }
        }
    }
}
