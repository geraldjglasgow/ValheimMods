using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// The Copy tool's keys, fixed like the Site planner's: Enter saves the selection (asking for a name), Backspace
    /// clears it; a click takes one piece, Shift + click the whole building, G + click the joined pieces
    /// of one type (held alone, Shift and G preview what a click would take). Nothing fires while text is typed, with Alt held, or while a game window
    /// takes the input. Enter is also the game's chat key: while it saves, the chat line stays shut (<see cref="ChatEnter"/>).
    /// </summary>
    public static class CopyKeys
    {
        public const KeyCode ClearKey = KeyCode.Backspace;
        public const KeyCode SameTypeKey = KeyCode.G;

        /// <summary>Shift is held: a click takes the whole building.</summary>
        public static bool Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        /// <summary>G is held (not while text is typed).</summary>
        public static bool SameType => Input.GetKey(SameTypeKey) && !Keys.TextInputActive;

        /// <summary>What a click takes now: G its joined pieces of one type, Shift its building, otherwise the piece (no Ctrl: it lowers the build camera).</summary>
        public static CopyMode Mode => SameType ? CopyMode.SameType : Shift ? CopyMode.Building : CopyMode.Piece;

        private static bool Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        /// <summary>Per frame while the tool is shown.</summary>
        public static void Handle(Player player)
        {
            if (CopySave.Busy || !Usable(player))
                return;
            if (Input.GetKeyDown(ClearKey) && CopySelection.Count > 0)
            {
                CopySelection.Clear();
                Messages.Center(CopyWords.Cleared);
            }
            if (ChatEnter.Pressed && CopySelection.Count > 0)
            {
                ChatEnter.Take();
                CopySave.Ask();
            }
        }

        /// <summary>
        /// Enter goes to the tool this frame (it saved already, or will when its update runs after the chat's), or it
        /// confirmed the tool's name box: either way the chat line stays shut.
        /// </summary>
        public static bool TakesEnter()
        {
            if (!ChatEnter.Pressed)
                return false;
            return CopySave.Busy || (CopySelection.Count > 0 && CopySession.Active && Usable(Player.m_localPlayer));
        }

        private static bool Usable(Player player) => player != null && !Keys.TextInputActive && !Alt && player.TakeInput();
    }
}
