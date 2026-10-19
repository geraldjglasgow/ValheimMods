using System;
using PatchGuard;
using Translations;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Hel's Anvil's hover text (<see cref="HelAnvil"/>), which the game asks for every frame the anvil is looked at: what
    /// it shows - the trial's phase, whether this player is one of the trial's in the hall, the repair's cost and whether
    /// their souls cover it - is packed into one number, and the text is built again only when that number or the
    /// player's language changes. A failure is reported once and the anvil shows its name alone from then on.
    /// </summary>
    internal static class AnvilText
    {
        private static long _key = long.MinValue;
        private static int _language = -1;
        private static string _text = "";
        private static bool _broken;

        public static string For()
        {
            if (_broken)
            {
                return Lang.Get(ArenaWords.Anvil);
            }
            long key = Key(out int cost, out int line);
            if (key != _key || _language != Lang.Version)
            {
                _key = key;
                _language = Lang.Version;
                _text = Build(line, cost);
            }
            return _text;
        }

        public static void Fail(Exception e)
        {
            if (!_broken)
            {
                _broken = true;
                Guard.Report(e, "Hel's Anvil hover");
            }
        }

        // Which line (0 none, 1 cold, 2 repair, 3 too few souls) and the cost, as one number.
        private static long Key(out int cost, out int line)
        {
            cost = 0;
            line = 0;
            ArenaCore? core = ArenaCore.Current;
            ZDO? zdo = core != null ? core.Zdo : null;
            if (zdo == null || !ArenaState.IsRunning(zdo) || !HallPlayers.LocalInHall())
            {
                return 0L;
            }
            long me = ZDOMan.GetSessionID();
            cost = AnvilOwner.Cost(zdo, me, out bool open);
            bool member = SoulWallets.TryGet(zdo, me, out _);
            line = !member ? 0 : !open ? 1 : SoulWallets.Souls(zdo, me) >= cost ? 2 : 3;
            return (long)line << 32 | (uint)cost;
        }

        private static string Build(int line, int cost)
        {
            string name = Lang.Get(ArenaWords.Anvil);
            switch (line)
            {
                case 1: return name + "\n" + Lang.Get(ArenaWords.AnvilCold);
                case 2:
                    return name + "\n" + Keys("[<color=yellow><b>$KEY_Use</b></color>] ")
                        + Lang.Format(ArenaWords.AnvilRepair, cost.ToString());
                case 3: return name + "\n" + Lang.Format(ArenaWords.AnvilPoor, cost.ToString());
                default: return name;
            }
        }

        // The game's key names, as the player has them bound.
        private static string Keys(string text) => Localization.instance != null ? Localization.instance.Localize(text) : text;
    }
}
