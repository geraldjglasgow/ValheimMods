using Translations;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The words players read about a raid: the message as it is sounded, the one as it ends, and the line under the
    /// minimap; one place, so they read alike (keys in <see cref="RaidWords"/>). A raid is named after its creature
    /// (<see cref="RaidState.Face"/>), a word every machine reads in its own language. The start and the end are made on
    /// the host's owner and shown on other machines, so they leave here as <c>LangWire</c> messages that
    /// <see cref="RaidNet"/> reads on arrival; the line is drawn by each machine for itself.
    /// </summary>
    internal static class RaidText
    {
        /// <summary>"Raiders are coming for your gold!" with the raid's name and its difficulty, for the wire.</summary>
        public static string Started(string face, RaidBand band) =>
            LangWire.Message(RaidWords.Started, face, Lang.Token(band.Word));

        /// <summary>A raid staked with trophies won, for the wire: the win, and how many trophies are back in the chest.</summary>
        public static string WonTrophies(string face, int back) =>
            LangWire.Message(RaidWords.WonTrophies, face, back.ToString(System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>How the raid ended, for the wire; empty for none.</summary>
        public static string Ended(string face, RaidEnd how)
        {
            switch (how)
            {
                case RaidEnd.Won: return LangWire.Message(RaidWords.Won, face);
                case RaidEnd.Robbed: return LangWire.Message(RaidWords.Robbed);
                case RaidEnd.Stopped: return LangWire.Message(RaidWords.Stopped, face);
                case RaidEnd.Abandoned: return LangWire.Message(RaidWords.Abandoned);
                case RaidEnd.TimedOut: return LangWire.Message(RaidWords.TimedOut);
                default: return "";
            }
        }

        /// <summary>The line under the minimap, in this machine's language: "Fuling raid (Brutal) - wave 2 of 3 - 5 left - 8:40".</summary>
        public static string Line(string face, RaidBand band, RaidPhase phase, int wave, int left, int secondsLeft,
            int secondsToNext)
        {
            string name = Name(face);
            string clock = Clock(secondsLeft);
            switch (phase)
            {
                case RaidPhase.Countdown: return Lang.Format(RaidWords.LineCountdown, name, band.Title, Clock(secondsToNext));
                case RaidPhase.Break:
                    return Lang.Format(RaidWords.LineBreak, name, band.Title, wave + 1, RaidTable.Waves, Clock(secondsToNext), clock);
                default: return Lang.Format(RaidWords.LineWave, name, band.Title, wave, RaidTable.Waves, left, clock);
            }
        }

        /// <summary>The raid's creature in this machine's language: "Fuling".</summary>
        public static string Name(string face) =>
            Localization.instance != null && face.IndexOf('$') >= 0 ? Localization.instance.Localize(face) : face;

        /// <summary>The raid's name for the log and the console, which stay English: "Fuling raid".</summary>
        public static string LogName(string face) => Name(face) + " raid";

        /// <summary>Whole seconds as m:ss.</summary>
        public static string Clock(int seconds)
        {
            int s = seconds < 0 ? 0 : seconds;
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
