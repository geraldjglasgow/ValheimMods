using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using Hotkeys;
using PackPanel.Core;
using PackPanel.Slots;
using UnityEngine;

namespace PackPanel.Consume
{
    /// <summary>
    /// The Food and Mead bar (the user's request, 2026-09-28: the food and mead hotkeys shown in the bottom left of the
    /// screen). In the empty strip under the game's health panel, level with its left edge: the Food Key's cap, the Food
    /// slots' cells, a gap, then the Mead Key's cap and the Mead slots' cells (<see cref="ConsumeBarCell"/>), so the player
    /// sees what a press would eat or drink. A group without slots or without a key is left out; nothing shows without
    /// either, while dead, or with the module off. A child of the HUD's root, so it hides with the HUD. Written from a
    /// <c>Hud.Update</c> postfix ten times a second, built again only when the slot counts or the keys change. Per
    /// player (<c>5. Look / Food And Mead Bar</c>); local only, nothing is sent.
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    public static class ConsumeBar
    {
        public const string Name = "PackPanel_consumebar";
        private const float Gap = 4f;
        private const float GroupGap = 14f;
        private const float Every = 0.1f;

        /// <summary>The bar's bottom-left corner in HUD units: the health panel's left edge, under its bottom (58).</summary>
        private static readonly Vector2 Corner = new Vector2(50f, 6f);

        private static readonly SlotKind[] Groups = { SlotKind.Food, SlotKind.Mead };
        private static readonly List<ConsumeBarCell> cells = new List<ConsumeBarCell>();
        private static RectTransform bar;
        private static string built;
        private static float next;

        [HarmonyPostfix]
        public static void Postfix(Hud __instance)
        {
            if (Time.time < next)
                return;
            next = Time.time + Every;
            Player player = Player.m_localPlayer;
            string plan = Shown(player) ? Plan() : "";
            if (plan.Length > 0 && (bar == null || plan != built))
                Build(__instance, plan);
            if (bar == null)
                return;
            bool show = plan.Length > 0;
            if (bar.gameObject.activeSelf != show)
                bar.gameObject.SetActive(show);
            for (int i = 0; show && i < cells.Count; i++)
                cells[i].Fill(player);
        }

        private static bool Shown(Player player) =>
            player != null && !player.IsDead() && InventoryState.Active && player == InventoryState.Player && ConsumeSettings.Bar.Value;

        private static ConfigEntry<KeyboardShortcut> KeyOf(SlotKind kind) => kind == SlotKind.Food ? ConsumeSettings.FoodKey : ConsumeSettings.MeadKey;

        private static bool Drawn(SlotKind kind) => InventoryState.CellsOf(kind).Count > 0 && KeyNames.Short(KeyOf(kind).Value).Length > 0;

        /// <summary>What the bar is built from: each drawn group's slot count and key; empty when nothing is drawn.</summary>
        private static string Plan()
        {
            StringBuilder plan = new StringBuilder();
            foreach (SlotKind kind in Groups)
            {
                if (!Drawn(kind))
                    continue;
                plan.Append(kind).Append(' ').Append(InventoryState.CellsOf(kind).Count);
                plan.Append(' ').Append(KeyNames.Short(KeyOf(kind).Value)).Append('|');
            }
            return plan.ToString();
        }

        private static void Build(Hud hud, string plan)
        {
            if (bar != null)
                Object.Destroy(bar.gameObject);
            bar = null;
            cells.Clear();
            GameObject template = Template(hud);
            if (template == null)
                return;
            bar = MakeBar(hud.m_healthPanel);
            float x = 0f;
            foreach (SlotKind kind in Groups)
            {
                if (Drawn(kind))
                    x = AddGroup(template, kind, x) + GroupGap;
            }
            built = plan;
        }

        /// <summary>The key's cap and the group's cells from <paramref name="x"/>; the right edge of the last.</summary>
        private static float AddGroup(GameObject template, SlotKind kind, float x)
        {
            x += ConsumeBarCell.KeyCap(bar, template, KeyNames.Short(KeyOf(kind).Value), x) + Gap;
            int count = InventoryState.CellsOf(kind).Count;
            for (int number = 1; number <= count; number++)
            {
                cells.Add(ConsumeBarCell.Slot(bar, template, kind, number, x));
                x += ConsumeBarCell.Size + Gap;
            }
            return x - Gap;
        }

        /// <summary>The game's first food square (<c>hudroot/healthpanel/food0</c>, the parent of <c>m_foodIcons[0]</c>); null before the HUD is up.</summary>
        private static GameObject Template(Hud hud)
        {
            if (hud == null || hud.m_healthPanel == null || hud.m_foodIcons == null || hud.m_foodIcons.Length == 0 || hud.m_foodIcons[0] == null)
                return null;
            Transform square = hud.m_foodIcons[0].transform.parent;
            return square != null ? square.gameObject : null;
        }

        /// <summary>The bar, a sibling of the health panel under the HUD's root, anchored to the screen's bottom-left corner.</summary>
        private static RectTransform MakeBar(RectTransform healthPanel)
        {
            GameObject go = new GameObject(Name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(healthPanel.parent, false);
            rect.SetSiblingIndex(healthPanel.GetSiblingIndex() + 1);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, ConsumeBarCell.Size);
            rect.anchoredPosition = Corner;
            return rect;
        }
    }
}
