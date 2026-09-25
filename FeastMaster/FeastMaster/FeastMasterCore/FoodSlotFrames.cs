using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace FeastMaster
{
    /// <summary>
    /// Shows as many food slots as Food Slots, or as the foods the player still holds after the count was lowered.
    /// The game hides only a slot's bar, icon and timer when it is empty; the slot's frame (its slot root, when the
    /// bar, icon and timer sit inside one) always shows, because the game always has all its slots in use. So the
    /// frames beyond the count are hidden after every Hud.UpdateFood. Roots that are the bar, icon or timer
    /// themselves are left to the game, which already hides them when empty.
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateFood))]
    public static class FoodSlotFrames
    {
        private static Transform[][] frames = new Transform[0][];

        /// <summary>Finds each slot's frames once the HUD's slot lists are complete.</summary>
        public static void Capture(Hud hud)
        {
            int count = hud.m_foodBars.Length;
            frames = new Transform[count][];
            for (int i = 0; i < count; i++)
                frames[i] = FramesOf(hud, i, i == 0 ? 1 : i - 1);
        }

        private static Transform[] FramesOf(Hud hud, int slot, int other)
        {
            Component[] parts = { hud.m_foodBars[slot], hud.m_foodIcons[slot], hud.m_foodTime[slot] };
            Component[] others = { hud.m_foodBars[other], hud.m_foodIcons[other], hud.m_foodTime[other] };
            List<Transform> roots = new List<Transform>();
            for (int i = 0; i < parts.Length; i++)
            {
                Transform root = FoodSlotsHud.SlotRoot(parts[i].transform, others[i].transform);
                if (!roots.Contains(root) && !IsPart(root, parts))
                    roots.Add(root);
            }
            return roots.ToArray();
        }

        private static bool IsPart(Transform root, Component[] parts)
        {
            foreach (Component part in parts)
            {
                if (part.transform == root)
                    return true;
            }
            return false;
        }

        [HarmonyPostfix]
        public static void Postfix(Player player)
        {
            int visible = Mathf.Max(Settings.FoodSlots.Value, player.GetFoods().Count);
            for (int i = 0; i < frames.Length; i++)
            {
                foreach (Transform frame in frames[i])
                {
                    if (frame != null && frame.gameObject.activeSelf != i < visible)
                        frame.gameObject.SetActive(i < visible);
                }
            }
        }
    }
}
