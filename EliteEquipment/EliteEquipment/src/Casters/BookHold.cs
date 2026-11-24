using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One player's spellbook pose on this machine (features/casters.md, "Held open"): a weight that moves toward 1 while a
    /// book is in the left hand and nothing yields (<see cref="BookYield"/>), toward 0 otherwise, over a fifth of a second
    /// and eased, so the arm never snaps; the arm and hand posed by it (<see cref="BookHand"/>), and the left hand's copy
    /// of the book shown open while the weight is above 0 (<see cref="BookOpen"/>). Visual only: nothing is sent or saved.
    /// </summary>
    public sealed class BookHold
    {
        private const float Blend = 0.2f;

        private readonly BookHand hand;
        private readonly int[] bools;
        private float weight;
        private GameObject opened;
        private GameObject checkedCopy;
        private bool copyOpens;

        private BookHold(BookHand hand, int[] bools) => (this.hand, this.bools) = (hand, bools);

        /// <summary>A player's hold, or null when the model lacks a bone the pose needs (a warning).</summary>
        public static BookHold Of(Player player)
        {
            BookHand hand = BookHand.Of(player);
            if (hand != null)
                return new BookHold(hand, BookYield.Bools(player.m_animator));
            Plugin.Log.LogWarning($"EliteEquipment: {player.GetPlayerName()}'s model has no left arm and fingers to hold a book open");
            return null;
        }

        /// <summary>One frame after the animator; false once there is nothing left to do (no book, weight 0).</summary>
        public bool Step(Player player, bool held, float dt)
        {
            GameObject copy = Openable(held ? player.m_visEquipment.m_leftItemInstance : null);
            bool posing = copy != null && !BookYield.Now(player, bools);
            weight = Mathf.MoveTowards(weight, posing ? 1f : 0f, dt / Blend);
            Book(weight > 0f ? copy : null);
            if (weight > 0f)
                hand.Pose(player.transform, weight * weight * (3f - 2f * weight));
            return held || weight > 0f;
        }

        // The copy in the left hand, or null when it has no book to draw open (its bundle not in yet: the shield hold stays);
        // asked once per copy.
        private GameObject Openable(GameObject copy)
        {
            if (copy != checkedCopy)
                (checkedCopy, copyOpens) = (copy, copy != null && BookOpen.Opens(copy));
            return copyOpens ? copy : null;
        }

        // The open book on the copy now in the hand (or on none): the one opened before closed again, if it is still there.
        private void Book(GameObject open)
        {
            if (open == opened)
                return;
            if (opened != null)
                BookOpen.Show(opened, false, hand);
            if (open != null)
                BookOpen.Show(open, true, hand);
            opened = open;
        }
    }
}
