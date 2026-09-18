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
    /// growing leftward as more are taken - oldest leftmost, newest nearest the edge. At most 4 are shown; beyond
    /// that the oldest drop off the left first, since the star row is never clipped to make room but these icons
    /// are allowed to be. Lives under the same nameplate GameObject EnemyHudCloakPatch already hides as a whole for
    /// a Cloaked creature, so it needs no fade logic of its own.
    /// </summary>
    public sealed class PouchIcons : MonoBehaviour
    {
        private const int MaxIcons = 4;
        private const float VanillaSize = 10f; // matches StarRow's own reference size
        private const float Gap = 3f;
        private const float TopEdge = -3f; // the same shared line StarRow's glyphs hang from
        private const float RefreshInterval = 0.5f; // the pouch can grow mid-fight, unlike a fixed star count

        private Character _character = null!;
        private RectTransform _healthBar = null!;
        private readonly List<GameObject> _icons = new List<GameObject>();
        private int _lastCount = -1;
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
            if (pouch.Count == _lastCount)
            {
                return;
            }
            _lastCount = pouch.Count;
            Layout(pouch);
        }

        private void Layout(List<PouchStore.Entry> pouch)
        {
            Clear();
            List<PouchStore.Entry> shown = pouch.Count > MaxIcons
                ? pouch.GetRange(pouch.Count - MaxIcons, MaxIcons)
                : pouch;
            float size = VanillaSize * Configuration.StolenIconSize.Value;
            float totalWidth = shown.Count * size + Mathf.Max(0, shown.Count - 1) * Gap;
            float cursor = _healthBar.rect.width - totalWidth;
            foreach (PouchStore.Entry entry in shown)
            {
                cursor += CreateIcon(entry, cursor, size) + Gap;
            }
        }

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
