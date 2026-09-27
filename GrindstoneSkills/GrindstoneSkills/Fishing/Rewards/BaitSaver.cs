using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The bait saver: the game uses up the bait when a fish is hooked and gives it back only when nothing was. When the
    /// local player lands a fish, with the chance of Bait Saver At 100's share of their level, the bait comes back, stars
    /// and all (<see cref="StarredBait.Give"/>), and "Bait saved" shows top left. A fish lost or a snapped line never
    /// saves the bait. On the angler's client, from <see cref="CatchHook"/>.
    /// </summary>
    public static class BaitSaver
    {
        public static void OnCatch(CatchInfo info)
        {
            FishingFloat fishingFloat = info.Float;
            FloatFight fight = FloatFight.Of(fishingFloat);
            if (fight == null || Random.value >= FishSkill.Share(FishingCatchSettings.BaitSaverAt100.Value, FishSkill.Of(info.Player)))
                return;
            if (StarredBait.Give(info.Player, fishingFloat.GetBait(), fight.BaitStars))
                info.Player.Message(MessageHud.MessageType.TopLeft, "Bait saved");
        }
    }
}
