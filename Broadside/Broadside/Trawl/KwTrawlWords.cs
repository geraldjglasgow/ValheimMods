namespace Broadside
{
    /// <summary>
    /// Every word the trawl shows: the winch's hover (its name, what E does, how full the net is and, while it is out, how
    /// it fares at the ship's speed as this screen sees it) and the words on the screen when E is pressed, the net snags or
    /// fills. English, as Broadside's other words.
    /// </summary>
    public static class KwTrawlWords
    {
        public const string Cast = "You cast the trawl net", Hauled = "You haul in the trawl net", Busy = "The net winch is busy";
        public const string NotAboard = "Build the net winch on a launched ship to trawl";
        public const string ArmInboard = "The winch's arm must reach out over the water: build it at the stern or a side rail, facing out";
        public const string Shallow = "Too shallow to trawl here";
        public const string SnaggedNow = "The trawl net is snagged on the sea floor", FullNow = "The trawl net is full";
        private const string Key = "[<color=yellow><b>$KEY_Use</b></color>] ";

        public static string Hover(KwTrawlWinch winch, ZDO zdo)
        {
            KwTrawlStep step = KwTrawlState.Step(zdo);
            string text = $"{KwTrawl.Label}\n{Doing(step)}\nNet: {KwTrawlState.Count(zdo)} of {KwTrawlTune.Capacity} fish";
            if (step == KwTrawlStep.Out)
                text += "\n" + Fares(KwTrawlState.Fare(zdo), winch.Speed);
            else if (step == KwTrawlStep.Stowed && winch.Ship == null)
                text += "\n" + NotAboard;
            return Localization.instance.Localize(text);
        }

        private static string Doing(KwTrawlStep step) => step switch
        {
            KwTrawlStep.Stowed => Key + "Cast the net",
            KwTrawlStep.Casting => "Paying out the net",
            KwTrawlStep.Out => Key + "Haul in the net",
            KwTrawlStep.Hauling => "Hauling in the net",
            _ => "Emptying the net on the deck",
        };

        private static string Fares(KwTrawlFare fare, float speed) => fare switch
        {
            KwTrawlFare.Trawling => $"Trawling at {speed:0.0} m/s",
            KwTrawlFare.Slack => $"Too slow ({speed:0.0} m/s): the net hangs slack; trawl at {KwTrawlTune.Trawling:0.0} to {KwTrawlTune.Lifting:0.0} m/s",
            KwTrawlFare.Lifting => $"Too fast ({speed:0.0} m/s): the net lifts and catches less",
            KwTrawlFare.Snagged => "Snagged: the sea floor is too near",
            KwTrawlFare.Full => "The net is full: haul it in",
            KwTrawlFare.NoFish => "No fish in these waters",
            _ => "The net is going out",
        };
    }
}
