using System;
using System.Collections.Generic;
using HarmonyLib;
using PackPanel.Core;
using PackPanel.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PackPanel.Panels
{
    /// <summary>
    /// With no rows (Inventory Rows 0) the HUD's hotbar shows a box for every hotbar cell the player has, empty or not:
    /// the two hands and the top-row cells a worn backpack opens after them, up to key 8 (the user, 2026-10-05: "if I
    /// have +4 inventory I should see hotkeys 1-6"). The game draws boxes only up to its last item and makes them all
    /// again whenever that count changes, so the empty boxes after it are PackPanel's own: taken out of the game's list
    /// before it runs, put back after it, made again only when the count they follow changes. With rows the bar is the game's.
    /// </summary>
    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
    public static class HotbarCells
    {
        private static readonly List<HotkeyBar.ElementData> pads = new List<HotkeyBar.ElementData>();
        private static HotkeyBar owner;
        private static int padsFrom = -1;

        [HarmonyPrefix]
        private static void Prefix(HotkeyBar __instance)
        {
            if (__instance == owner)
                __instance.m_elements.RemoveAll(pads.Contains);
        }

        [HarmonyPostfix]
        private static void Postfix(HotkeyBar __instance, Player player)
        {
            int from = __instance.m_elements.Count;
            int wanted = player != null && !player.IsDead() ? Wanted() : 0;
            if (__instance != owner || from != padsFrom || pads.Count != Math.Max(0, wanted - from))
                Rebuild(__instance, player, from, wanted);
            bool gamepad = ZInput.IsGamepadActive();
            for (int i = 0; i < pads.Count; i++)
                pads[i].m_selection.SetActive(gamepad && from + i == __instance.m_selected);
            __instance.m_elements.AddRange(pads);
        }

        /// <summary>The open cells of the top row up to key 8 while the main grid is only the hands, else 0.</summary>
        private static int Wanted()
        {
            InventoryLayout layout = InventoryState.Active ? InventoryState.Layout : null;
            if (layout == null || layout.BaseCells >= layout.Width)
                return 0;
            int count = 0;
            int last = Math.Min(layout.Width, InventorySettings.GameWidth);
            while (count < last && layout.IsMain(new Vector2i(count, 0)))
                count++;
            return count;
        }

        private static void Rebuild(HotkeyBar bar, Player player, int from, int wanted)
        {
            foreach (HotkeyBar.ElementData pad in pads)
            {
                if (pad.m_go != null)
                    Object.Destroy(pad.m_go);
            }
            pads.Clear();
            owner = bar;
            padsFrom = from;
            for (int index = from; index < wanted; index++)
                pads.Add(Pad(bar, player, index));
        }

        /// <summary>An empty box at a column, made as the game makes its own: its key number, a tap uses its key.</summary>
        private static HotkeyBar.ElementData Pad(HotkeyBar bar, Player player, int index)
        {
            GameObject go = Object.Instantiate(bar.m_elementPrefab, bar.transform);
            go.transform.localPosition = new Vector3(index * bar.m_elementSpace, 0f, 0f);
            go.GetComponent<Button>().onClick.AddListener(() => bar.ElementClicked(player, index));
            go.transform.Find("binding").GetComponent<TMP_Text>().text = ZInput.IsGamepadActive() ? string.Empty : (index + 1).ToString();
            HotkeyBar.ElementData pad = new HotkeyBar.ElementData
            {
                m_go = go,
                m_icon = go.transform.Find("icon").GetComponent<Image>(),
                m_durability = go.transform.Find("durability").GetComponent<GuiBar>(),
                m_amount = go.transform.Find("amount").GetComponent<TMP_Text>(),
                m_equiped = go.transform.Find("equiped").gameObject,
                m_queued = go.transform.Find("queued").gameObject,
                m_selection = go.transform.Find("selected").gameObject,
            };
            foreach (GameObject part in new[] { pad.m_icon.gameObject, pad.m_durability.gameObject, pad.m_amount.gameObject, pad.m_equiped, pad.m_queued })
                part.SetActive(false);
            return pad;
        }
    }
}
