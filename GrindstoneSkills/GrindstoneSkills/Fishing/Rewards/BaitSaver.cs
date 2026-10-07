using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The bait saver: the game uses up the bait when a fish is hooked and gives it back only when nothing was. When the
    /// local player lands a fish, with the chance of Bait Saver At 100's share of their level, the bait comes back (into
    /// the inventory, or at their feet when it has no room), and "Bait saved" shows top left. A fish lost or a snapped
    /// line never saves the bait. On the angler's client, from <see cref="CatchHook"/>.
    /// </summary>
    public static class BaitSaver
    {
        public static void OnCatch(CatchInfo info)
        {
            FishingFloat fishingFloat = info.Float;
            if (FloatFight.Of(fishingFloat) == null || Random.value >= FishSkill.Share(FishingCatchSettings.BaitSaverAt100.Value, FishSkill.Of(info.Player)))
                return;
            if (Give(info.Player, fishingFloat.GetBait()))
                info.Player.Message(MessageHud.MessageType.TopLeft, "Bait saved");
        }

        /// <summary>
        /// Gives the player one bait of the named prefab: into the inventory, or at their feet when it has no room (the
        /// game would lose it). False only when there is no such bait.
        /// </summary>
        private static bool Give(Player player, string bait)
        {
            GameObject prefab = string.IsNullOrEmpty(bait) || ZNetScene.instance == null ? null : ZNetScene.instance.GetPrefab(bait);
            if (player == null || prefab == null || prefab.GetComponent<ItemDrop>() == null)
                return false;
            if (player.GetInventory().CanAddItem(prefab, 1))
                player.GetInventory().AddItem(prefab, 1);
            else
                ItemDrop.OnCreateNew(Object.Instantiate(prefab, player.transform.position + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>());
            return true;
        }
    }
}
