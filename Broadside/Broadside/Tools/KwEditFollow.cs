using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// What else an edit moves besides its own parts (<see cref="KwEditKind.Follow"/>: a cabin's stack, the cabins on its roof
    /// and what stands on them): a plan for each other piece moved, built again or taken away (<see cref="KwEditPlan.Follows"/>),
    /// built in the edit's undo step; what the window's note says of them and what the message after OK says. Planned again at
    /// every change of the window; empty for a kind that moves nothing else.
    /// </summary>
    public sealed class KwEditFollow
    {
        public List<KwEditPlan> Moved { get; } = new List<KwEditPlan>();

        /// <summary>The window's note on them ("" for nothing).</summary>
        public string Note { get; set; } = "";

        /// <summary>The message's line on them after OK ("" for nothing).</summary>
        public string Told { get; set; } = "";

        /// <summary>The pieces moved or taken, still loaded, for the hiding while the window is open.</summary>
        public List<Piece> Pieces => Moved.ConvertAll(plan => plan.Target.Piece).FindAll(piece => piece != null);

        public void Clear()
        {
            Moved.Clear();
            Note = "";
            Told = "";
        }
    }
}
