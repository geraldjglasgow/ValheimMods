namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The ground of this player's last blueprint build or ground fix of the session, kept so it can be put back:
    /// every vertex that moved goes back to the height it had (sent the same way, through each compiler's owner).
    /// Paint stays as the work left it.
    /// </summary>
    public static class GroundUndo
    {
        private static GroundWork last;

        /// <summary>The work a ground fix (not a build) did last, so Alt+Z on the Fix ground entry knows it may undo.</summary>
        public static bool LastWasFix { get; private set; }

        public static bool Has => last != null;

        public static void Record(GroundWork work, bool fix)
        {
            last = work;
            LastWasFix = fix;
        }

        /// <summary>Sends the heights back; returns how many vertices were sent (0 when there is nothing to put back).</summary>
        public static int Restore()
        {
            if (last == null)
                return 0;
            GroundWork back = new GroundWork();
            foreach (GroundPoint p in last.Points)
            {
                if (p.Moves)
                    back.Points.Add(new GroundPoint { X = p.X, Z = p.Z, Height = p.Before, Before = p.Height, Moves = true });
            }
            last = null;
            if (back.Points.Count > 0)
                GroundWriter.Send(back);
            return back.Points.Count;
        }
    }
}
