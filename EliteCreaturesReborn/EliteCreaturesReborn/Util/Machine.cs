namespace EliteCreaturesReborn.Util
{
    /// <summary>
    /// What kind of machine this is. A dedicated server has no screen, no speakers and no local player, so nothing
    /// cosmetic it made would ever be seen or heard: effects make nothing there, while what they stand for - a cloud's
    /// poison, a blast's damage - still runs wherever it runs.
    /// </summary>
    internal static class Machine
    {
        /// <summary>True on a dedicated server.</summary>
        public static bool Headless => ZNet.instance != null && ZNet.instance.IsDedicated();
    }
}
