using System.Collections.Generic;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// Lays a Phantom boss's copies out as small health bars under the boss's own. The game draws every boss bar in the
    /// same place at the top of the screen, so without this the copies' bars sit on top of the boss's and only one of
    /// them shows. Each frame, after the game has updated its bars, every copy's bar is shrunk to a quarter of the boss
    /// bar's width and a sliver of its height and put in a slot below it: four copies side by side are as long as the
    /// boss bar, a fifth starts a second row, and a short row is centred. Its name shrinks with it. The boss's own bar is
    /// untouched. Copies are ordered by their ZDO id, so a bar keeps its place until a copy before it falls. Drawn
    /// locally on each client from the copy's replicated mark; nothing is sent.
    /// </summary>
    internal static class PhantomBars
    {
        public readonly struct Bar
        {
            public readonly ZDOID Id;
            public readonly GameObject Gui;

            public Bar(ZDOID id, GameObject gui)
            {
                Id = id;
                Gui = gui;
            }
        }

        private const int PerRow = 4;
        private const float BarHeight = 5f;
        private const float Gap = 10f;

        /// <summary>From the boss bar's place to the first row of copies: clear of the boss's star row.</summary>
        private const float FirstDrop = 46f;
        private const float RowStep = 26f;
        private const float NameScale = 0.42f;

        /// <summary>The boss bar's width when the game's template cannot be read.</summary>
        private const float FallbackWidth = 600f;

        /// <summary>True, with the copy's id, when this character is a Phantom copy - read from its ZDO, ready or not.</summary>
        public static bool IsCopy(Character character, out ZDOID id)
        {
            id = ZDOID.None;
            if (!character.IsBoss())
            {
                return false; // a copy is its boss's own prefab, so only a boss bar can be one
            }
            ZNetView nview = character.GetComponent<ZNetView>();
            ZDO? zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
            id = zdo != null ? zdo.m_uid : ZDOID.None;
            return zdo != null && AspectStore.GetPhantomOf(zdo) != ZDOID.None;
        }

        public static void Layout(List<Bar> bars)
        {
            GameObject? bossHud = EnemyHud.instance != null ? EnemyHud.instance.m_baseHudBoss : null;
            RectTransform? template = bossHud != null ? bossHud.transform as RectTransform : null;
            if (bars.Count == 0 || template == null)
            {
                return;
            }
            bars.Sort((a, b) => a.Id.UserID != b.Id.UserID ? a.Id.UserID.CompareTo(b.Id.UserID) : a.Id.ID.CompareTo(b.Id.ID));
            float slot = BossBarWidth(template) / PerRow;
            for (int i = 0; i < bars.Count; i++)
            {
                if (bars[i].Gui != null && bars[i].Gui.transform is RectTransform root)
                {
                    root.anchoredPosition = template.anchoredPosition + SlotOffset(i, bars.Count, slot);
                    Shrink(root, slot - Gap);
                }
            }
        }

        private static float BossBarWidth(RectTransform template)
        {
            RectTransform? health = template.Find("Health") as RectTransform;
            return health != null && health.sizeDelta.x > 1f ? health.sizeDelta.x : FallbackWidth;
        }

        /// <summary>Where slot <paramref name="index"/> of <paramref name="count"/> sits relative to the boss bar.</summary>
        private static Vector2 SlotOffset(int index, int count, float slot)
        {
            int row = index / PerRow;
            int inRow = Mathf.Min(PerRow, count - row * PerRow);
            float x = (index % PerRow - (inRow - 1) / 2f) * slot;
            return new Vector2(x, -(FirstDrop + row * RowStep));
        }

        // The bar is scaled rather than resized, so the game's own bar filling works in its usual units. The name is
        // scaled evenly, given the slot's width to fit in, and set down just above its bar.
        private static void Shrink(RectTransform root, float width)
        {
            RectTransform? health = root.Find("Health") as RectTransform;
            RectTransform? name = root.Find("Name") as RectTransform;
            if (health == null || name == null || health.sizeDelta.x < 1f || health.sizeDelta.y < 1f)
            {
                return;
            }
            health.localScale = new Vector3(width / health.sizeDelta.x, BarHeight / health.sizeDelta.y, 1f);
            name.localScale = new Vector3(NameScale, NameScale, 1f);
            name.sizeDelta = new Vector2(width / NameScale, name.sizeDelta.y);
            float barCentre = (health.anchorMin.y - name.anchorMin.y) * root.rect.height + health.anchoredPosition.y;
            float lift = BarHeight / 2f + name.sizeDelta.y * NameScale / 2f;
            name.anchoredPosition = new Vector2(0f, barCentre + lift);
        }
    }
}
