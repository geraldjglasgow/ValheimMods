using System.Collections.Generic;

namespace EliteCreaturesReborn.Tally
{
    /// <summary>
    /// One boss damage board - the fight it closes, the boss's name and every player's damage - and the latest one this
    /// machine has received. Every machine keeps the latest, the server included (the board's broadcast reaches it too),
    /// so <c>damage</c> can show it again, and the server can answer a player who joined after the kill. Kept in memory
    /// only, for the session: a new world or server starts with none.
    /// </summary>
    internal sealed class BossBoard
    {
        public string Fight = "";
        public string BossName = "";
        public List<DamageTally.Entry> Entries = new List<DamageTally.Entry>();

        public static BossBoard? Latest { get; private set; }

        /// <summary>
        /// Keeps <paramref name="board"/> as the latest. False, keeping nothing, when it is the fight already kept: a
        /// Twin's partner falling a moment later sends the same fight again, with only what is left of the pair's tally.
        /// </summary>
        public static bool Remember(BossBoard board)
        {
            if (Latest != null && Latest.Fight == board.Fight)
            {
                return false;
            }
            Latest = board;
            return true;
        }

        public static void Forget() => Latest = null;

        public void Write(ZPackage pkg)
        {
            pkg.Write(Fight);
            pkg.Write(BossName);
            DamageTally.Write(pkg, Entries);
        }

        public static BossBoard Read(ZPackage pkg)
        {
            BossBoard board = new BossBoard { Fight = pkg.ReadString(), BossName = pkg.ReadString() };
            DamageTally.Read(pkg, board.Entries);
            return board;
        }
    }
}
