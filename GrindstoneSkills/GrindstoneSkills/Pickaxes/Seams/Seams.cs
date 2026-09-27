namespace GrindstoneSkills
{
    /// <summary>
    /// Seams and clean strikes (and Unbroken), on the miner's own client. A seam is personal: nobody else sees it, and
    /// nothing about it is stored or sent except the clean strike mark for the rock's owner (<see cref="CleanStrikeMarks"/>).
    /// <list type="bullet">
    /// <item>A seam is a chunk of a multi-chunk rock (MineRock5, boulders included) that glows for a window of seconds
    /// (<see cref="SeamGlow"/>). A swing that hits a rock with no open seam rolls the seam chance once; the seam opens
    /// when the swing is over, on an intact chunk near the hit that the swing did not touch and the miner can see
    /// (<see cref="SeamSwing"/>, <see cref="SeamPicker"/>).</item>
    /// <item>A hit on the seam chunk inside the window is a clean strike (<see cref="CleanStrikes"/>): more damage, a
    /// mark for the owner's extra roll, experience and a callout; the next seam opens at once, which makes a chain.</item>
    /// <item>A swing that hits the rock but not the seam chunk, or the window running out, ends the chain.</item>
    /// <item>Unbroken: from its level, each clean strike after the first in a chain hits harder
    /// (<see cref="CleanStrikes.Multiplier"/>).</item>
    /// </list>
    /// A pickaxe too weak for the rock (tool tier) neither opens seams nor strikes them. Every open seam closes when
    /// Pickaxes is turned off, the rock goes, the chunk breaks or the local player is gone (<see cref="SeamDriver"/>).
    /// </summary>
    public static class Seams
    {
        /// <summary>
        /// Called by <see cref="MineHit"/> on the miner's own client for each of the local player's pickaxe hits on a
        /// MineRock5 chunk, inside the running swing (<see cref="MineSwing.Attack"/>, <see cref="MineSwing.Serial"/>,
        /// <see cref="MineSwing.Rocks"/>), before MineRock5.Damage sends the hit to the rock's owner. A swing can hit
        /// several chunks, one call each. <paramref name="rock"/>.Chunks5 is the MineRock5 (never null here);
        /// <paramref name="area"/> is the hit chunk's area index (<see cref="RockChunks"/>). The hit is noted for the
        /// swing; a hit on the open seam's chunk is a clean strike, at most one per rock and swing, which multiplies the
        /// hit's damage before it goes out. Plain stone gets seams too; the owner gives the extra roll to ore deposits only.
        /// </summary>
        public static void OnLocalHit(Rock rock, HitData hit, int area)
        {
            if (!PickSkill.Active || Player.m_localPlayer == null || !hit.CheckToolTier(rock.Chunks5.m_minToolTier))
                return;
            SeamDriver.Ensure();
            SeamNote note = SeamSwing.Record(rock, hit.m_point, area);
            Seam seam = OpenSeams.Find(rock);
            if (seam == null || seam.Area != area || note.Link > 0)
                return;
            int link = seam.Links + 1;
            note.Strike(link, hit.m_point);
            CleanStrikes.Land(rock, hit, area, link);
        }
    }
}
