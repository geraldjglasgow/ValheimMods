using System.Globalization;

namespace GrindstoneSkills
{
    /// <summary>
    /// Landing a legendary fish is news: with Announce Legendary Catches on, every player on the server reads "&lt;angler&gt;
    /// landed a legendary Pike (24.1 kg)!" top left (<see cref="FishCallout.Announce"/>). Its guaranteed bonus items are
    /// <see cref="BonusItems"/>' part; its fillets are the game's own count for level 6.
    /// </summary>
    public static class LegendaryCatch
    {
        public static void OnCatch(CatchInfo info)
        {
            if (!info.Legendary || !FishingBigFishSettings.AnnounceLegendary.Value)
                return;
            string weight = info.Weight.ToString("0.0", CultureInfo.InvariantCulture);
            FishCallout.Announce($"{info.Player.GetPlayerName()} landed a legendary {info.Name} ({weight} kg)!");
        }
    }
}
