using System;

namespace GrindstoneSkills
{
    /// <summary>
    /// A status effect that only shows something: an icon in the HUD's status column while <see cref="Showing"/> says
    /// so, with <see cref="Label"/> as its text (a time, a stack count). SEMan updates it every frame on the local
    /// player and removes it once IsDone is true; the state it shows lives in the Defense feature it belongs to, so
    /// the icon can never outlast it. Status effects stay on the player's own client (only their attribute bits reach
    /// the ZDO, and these have none), so nothing is sent. Clone copies the delegates with the rest.
    /// </summary>
    public class DefenseStatus : StatusEffect
    {
        public Func<bool> Showing;
        public Func<string> Label;

        public override bool IsDone() => Showing == null || !HookGuard.Run("defense status", Showing, false);

        public override string GetIconText() => Label != null ? HookGuard.Run("defense status text", Label, "") : "";
    }
}
