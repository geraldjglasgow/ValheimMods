using TMPro;
using Translations;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The spell bar: while a wand is drawn, one slot under the hotbar, drawn with the game's own hotbar slot
    /// (<c>HotkeyBar.m_elementPrefab</c>: frame, icon, the key label), labelled RMB, with the spell last picked from the
    /// spell ring (<see cref="LastSpell"/>, what a right mouse tap casts) and its eitr cost where the hotbar shows a stack;
    /// greyed, without a cost, while its page is not in the held spellbook; empty before any pick. Made under the hotbar
    /// on the local machine the first time a wand is drawn; refreshed only when the wand, its spell, the held book's
    /// pages or the language change.
    /// </summary>
    public static class SpellBar
    {
        private const float Below = -78f;
        private static readonly Color Unavailable = new Color(0.35f, 0.35f, 0.35f, 0.7f);

        private static RectTransform bar;
        private static GameObject slot;
        private static ItemDrop.ItemData shownWand;
        private static string shownSpell;
        private static string shownPages;
        private static int shownWords = -1;

        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            ItemDrop.ItemData wand = player != null && !player.IsDead() && CastersSettings.On ? CasterGearWorn.Wand(player) : null;
            if (wand == null || !Ensure())
            {
                if (bar != null && bar.gameObject.activeSelf)
                    bar.gameObject.SetActive(false);
                shownWand = null;
                return;
            }
            if (!bar.gameObject.activeSelf)
                bar.gameObject.SetActive(true);
            if (Changed(player, wand))
                Show(player, LastSpell.Saved(wand), LastSpell.In(player, wand) != null);
        }

        private static bool Changed(Player player, ItemDrop.ItemData wand)
        {
            string spell = LastSpell.KeyOf(wand);
            ItemDrop.ItemData book = CasterGearWorn.Book(player);
            string pages = book != null ? BookPages.Text(book) : null;
            if (wand == shownWand && spell == shownSpell && pages == shownPages && Lang.Version == shownWords)
                return false;
            (shownWand, shownSpell, shownPages, shownWords) = (wand, spell, pages, Lang.Version);
            return true;
        }

        private static void Show(Player player, Spell saved, bool ready)
        {
            Icon(slot.transform.Find("icon")?.GetComponent<Image>(), saved, ready);
            TMP_Text amount = slot.transform.Find("amount")?.GetComponent<TMP_Text>();
            if (amount != null)
            {
                amount.gameObject.SetActive(ready);
                amount.text = ready ? Mathf.CeilToInt(SpellCost.Of(player, saved)).ToString() : "";
            }
            Label(slot.transform.Find("binding")?.GetComponent<TMP_Text>());
        }

        /// <summary>The slot's spell icon (none before a pick), greyed when its page is not in the held book.</summary>
        private static void Icon(Image icon, Spell saved, bool ready)
        {
            if (icon == null)
                return;
            icon.gameObject.SetActive(saved != null);
            icon.sprite = saved != null ? CasterBundles.Sprite(CasterBundles.Spells, saved.Icon) : null;
            icon.color = ready ? Color.white : Unavailable;
        }

        private static bool Ensure()
        {
            if (bar != null)
                return true;
            HotkeyBar hotbar = Hud.instance != null ? Hud.instance.GetComponentInChildren<HotkeyBar>(true) : null;
            if (hotbar == null || hotbar.m_elementPrefab == null)
                return false;
            bar = new GameObject("EE_SpellBar", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(hotbar.transform, false);
            bar.localPosition = new Vector3(0f, Below, 0f);
            slot = Slot(hotbar);
            return true;
        }

        private static GameObject Slot(HotkeyBar hotbar)
        {
            GameObject made = Object.Instantiate(hotbar.m_elementPrefab, bar, false);
            made.name = "EE_Spell0";
            made.transform.localPosition = Vector3.zero;
            Button button = made.GetComponent<Button>();
            if (button != null)
                button.interactable = false;
            foreach (string part in new[] { "durability", "equiped", "queued", "selected" })
                made.transform.Find(part)?.gameObject.SetActive(false);
            Label(made.transform.Find("binding")?.GetComponent<TMP_Text>());
            return made;
        }

        /// <summary>The slot's button (RMB) where the hotbar shows its number, shrunk to fit.</summary>
        private static void Label(TMP_Text binding)
        {
            if (binding == null)
                return;
            binding.text = Lang.Get("ee_spell_key");
            binding.enableAutoSizing = true;
            binding.fontSizeMin = 8f;
            binding.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
