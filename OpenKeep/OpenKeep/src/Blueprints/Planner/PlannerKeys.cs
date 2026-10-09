using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The Site planner's keys, fixed (the feature keeps few settings): Enter queues the selection, Backspace clears
    /// it, K opens and closes the queue panel; Shift with a click picks a whole house, G with a click the joined pieces of
    /// the same type, Shift + G every piece of that type in the house (held without a click, the keys preview the pieces in
    /// the hover glow). Nothing fires while text is
    /// typed or with Alt held, nor while a game window takes the input (the planner's own panel excepted, so K closes
    /// it). Enter is also the game's chat key: while it queues a selection, the chat line stays shut that frame.
    /// </summary>
    public static class PlannerKeys
    {
        public const KeyCode QueueKey = KeyCode.Return;
        public const KeyCode QueuePadKey = KeyCode.KeypadEnter;
        public const KeyCode ClearKey = KeyCode.Backspace;
        public const KeyCode PanelKey = KeyCode.K;
        public const KeyCode SameTypeKey = KeyCode.G;

        private static int queuedFrame = -1;

        public static bool Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        /// <summary>G is held (not while text is typed): clicks and the hover glow take the joined pieces of the same type.</summary>
        public static bool SameType => Input.GetKey(SameTypeKey) && !Keys.TextInputActive;

        /// <summary>Shift and G are held: clicks and the hover glow take every piece of the same type in the house.</summary>
        public static bool HouseType => SameType && Shift;

        private static bool Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        /// <summary>Per frame while the planner is active.</summary>
        public static void Handle(Player player)
        {
            if (!Usable(player))
                return;
            if (Input.GetKeyDown(PanelKey))
                PlannerPanel.Toggle();
            if (Input.GetKeyDown(ClearKey) && PlannerSelection.Count > 0)
            {
                PlannerSelection.Clear();
                Messages.Center(PlannerWords.Cleared);
            }
            if ((Input.GetKeyDown(QueueKey) || Input.GetKeyDown(QueuePadKey)) && PlannerSelection.Count > 0)
            {
                queuedFrame = Time.frameCount;
                QueueEdits.QueueSelection();
            }
        }

        /// <summary>
        /// Enter goes to the planner this frame (it queued already, or will when its update runs after the chat's),
        /// so the chat line must not open on it.
        /// </summary>
        public static bool TakesEnter()
        {
            if (queuedFrame == Time.frameCount)
                return true;
            Player player = Player.m_localPlayer;
            return Input.GetKeyDown(QueueKey) && PlannerSelection.Count > 0 && PlannerSession.IsActive(player, menuClosed: true) && Usable(player);
        }

        private static bool Usable(Player player) => !Keys.TextInputActive && !Alt && (PlannerPanel.Showing || player.TakeInput());
    }

    /// <summary>Chat.Update prefix: skipped for the one frame in which Enter queues the planner's selection, so the chat line does not open.</summary>
    [HarmonyPatch(typeof(Chat), nameof(Chat.Update))]
    public static class PlannerChatPatch
    {
        [HarmonyPrefix]
        public static bool Prefix() => !BlueprintSafe.Call("OpenKeep site planner Enter", PlannerKeys.TakesEnter, false);
    }
}
