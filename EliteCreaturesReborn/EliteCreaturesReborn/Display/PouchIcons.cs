using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using PatchGuard;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// Draws the icons of what a Thieving creature is carrying, on its nameplate: the star colour says what it is,
    /// these icons say what it cost you. Right-justified to the health bar's right edge, on StarRow's own line,
    /// growing leftward as more are taken - oldest leftmost, newest nearest the edge. Every item the pouch holds (one
    /// per star, up to PouchStore.HardCap) is drawn in the room the star row leaves; when they do not all fit, the
    /// icons first shrink toward the size vanilla draws a star at, and only then do the oldest drop off the left,
    /// since the star row is never clipped to make room but these icons are allowed to be. Lives under the same
    /// nameplate GameObject EnemyHudCloakPatch already hides as a whole for a Cloaked creature, so it needs no fade
    /// logic of its own.
    /// </summary>
    public sealed class PouchIcons : MonoBehaviour
    {
        private const float VanillaSize = 10f; // matches StarRow's own reference size, and the smallest an icon shrinks to
        private const float Gap = 3f;
        private const float TopEdge = -3f; // the same shared line StarRow's glyphs hang from
        private const float RefreshInterval = 0.5f; // the pouch can grow mid-fight, unlike a fixed star count
        private const string StarGlyphPrefix = "ecr_star_"; // the name StarRow.CreateGlyph gives each of its glyphs

        private Character _character = null!;
        private RectTransform _healthBar = null!;
        private readonly List<GameObject> _icons = new List<GameObject>();
        private int _lastCount = -1;
        private float _lastStarEnd = -1f;
        private float _timer;

        public void Init(Character character, RectTransform healthBar)
        {
            _character = character;
            _healthBar = healthBar;
        }

        private void Start() => Guard.Run("PouchIcons.Start", Refresh);

        private void Update() => Guard.Run("PouchIcons.Update", Poll);

        private void Poll()
        {
            _timer += Time.deltaTime;
            if (_timer < RefreshInterval)
            {
                return;
            }
            _timer = 0f;
            Refresh();
        }

        private void Refresh()
        {
            EliteController? controller = _character != null ? _character.GetComponent<EliteController>() : null;
            ZDO? zdo = controller != null && controller.Ready ? controller.View.GetZDO() : null;
            if (zdo == null || _healthBar == null)
            {
                return;
            }
            List<PouchStore.Entry> pouch = PouchStore.Load(zdo);
            float starEnd = StarRowEnd();
            if (pouch.Count == _lastCount && Mathf.Approximately(starEnd, _lastStarEnd))
            {
                return;
            }
            _lastCount = pouch.Count;
            _lastStarEnd = starEnd;
            Layout(pouch, _healthBar.rect.width - starEnd);
        }

        /// <summary>Where the shared line is free from: just past the rightmost glyph StarRow drew on this health bar,
        /// or 0 when it drew none (no stars, or coloured stars off). Read from the glyphs themselves rather than
        /// recomputed, so a star size setting can never put icons on the stars, and re-read on every poll because
        /// StarRow may build after this component's first layout.</summary>
        private float StarRowEnd()
        {
            float end = 0f;
            foreach (Transform child in _healthBar)
            {
                if (child is RectTransform glyph && child.name.StartsWith(StarGlyphPrefix, System.StringComparison.Ordinal))
                {
                    end = Mathf.Max(end, glyph.anchoredPosition.x + glyph.sizeDelta.x + Gap);
                }
            }
            return end;
        }

        private void Layout(List<PouchStore.Entry> pouch, float room)
        {
            Clear();
            int wanted = Mathf.Min(pouch.Count, PouchStore.HardCap);
            float size = IconSize(wanted, room);
            int shown = Mathf.Min(wanted, Fits(size, room));
            float cursor = _healthBar.rect.width - (shown * size + Mathf.Max(0, shown - 1) * Gap);
            for (int i = pouch.Count - shown; i < pouch.Count; i++)
            {
                cursor += CreateIcon(pouch[i], cursor, size) + Gap;
            }
        }

        /// <summary>The configured icon size, shrunk just enough for <paramref name="count"/> icons to fit the room the
        /// star row leaves, but never below the size vanilla draws a star at: smaller than that an icon is a smudge,
        /// so past it the oldest are dropped instead.</summary>
        private static float IconSize(int count, float room)
        {
            float configured = VanillaSize * Configuration.StolenIconSize.Value;
            if (count <= 0)
            {
                return configured;
            }
            float squeezed = (room - (count - 1) * Gap) / count;
            return Mathf.Min(configured, Mathf.Max(squeezed, Mathf.Min(configured, VanillaSize)));
        }

        // How many icons of this size fit in the room, gaps between them included; the epsilon keeps an exact
        // squeezed fit from rounding down by one.
        private static int Fits(float size, float room) =>
            Mathf.Max(0, Mathf.FloorToInt((room + Gap) / (size + Gap) + 0.001f));

        private float CreateIcon(PouchStore.Entry entry, float x, float size)
        {
            Sprite? sprite = entry.Item.GetIcon();
            GameObject icon = new GameObject("ecr_pouch_" + _icons.Count, typeof(RectTransform));
            RectTransform rect = icon.GetComponent<RectTransform>();
            rect.SetParent(_healthBar, worldPositionStays: false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 1f); // top-left pivot: hangs down from the shared top edge, like StarRow
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(x, TopEdge);
            Image image = icon.AddComponent<Image>();
            image.sprite = sprite;
            image.enabled = sprite != null;
            _icons.Add(icon);
            return size;
        }

        private void Clear()
        {
            foreach (GameObject icon in _icons)
            {
                if (icon != null)
                {
                    Destroy(icon);
                }
            }
            _icons.Clear();
        }

        private void OnDestroy() => Clear();
    }
}
