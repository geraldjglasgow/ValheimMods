using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// Draws the icons of what a Thieving creature is carrying, on its nameplate: the star colour says what it is,
    /// these icons say what it cost you. Right-justified to the health bar's right edge, on StarRow's own line,
    /// growing leftward as more are taken - oldest leftmost, newest nearest the edge. Every item the pouch holds (one
    /// per star, up to PouchStore.HardCap) is drawn in the room the star row leaves, shrinking and then dropping the
    /// oldest when they do not all fit (<see cref="IconRow"/>). A Devouring creature's meal icons, when it carries both,
    /// sit to the left of these (<see cref="MealIcons"/>). Lives under the same nameplate GameObject EnemyHudCloakPatch
    /// already hides as a whole for a Cloaked creature, so it needs no fade logic of its own. Each poll compares the pouch
    /// as stored with the one last drawn and unpacks the items only when it changed.
    /// </summary>
    public sealed class PouchIcons : MonoBehaviour, IPlateIcons
    {
        /// <summary>The name every pouch icon starts with, so the meal row can find where this one begins.</summary>
        public const string Prefix = "ecr_pouch_";

        private const float RefreshInterval = 0.5f; // the pouch can grow mid-fight, unlike a fixed star count

        private Character _character = null!;
        private EliteController? _controller;
        private RectTransform _healthBar = null!;
        private IconRow? _row;
        private byte[]? _lastPouch;
        private bool _drawn;
        private float _lastStarEnd = -1f;
        private float _timer;

        public void Init(Character character, RectTransform healthBar)
        {
            _character = character;
            _healthBar = healthBar;
            _row = new IconRow(healthBar, Prefix);
        }

        private void Start() => Guard.Run("PouchIcons.Start", static self => self.Refresh(), this);

        private void Update() => Guard.Run("PouchIcons.Update", static self => self.Poll(), this);

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

        // Laid out again only when something it depends on moved: the pouch as stored, or the star row beside it.
        private void Refresh()
        {
            if (_controller == null && _character != null)
            {
                _controller = _character.GetComponent<EliteController>();
            }
            ZDO? zdo = _controller != null && _controller.Ready ? _controller.View.GetZDO() : null;
            if (zdo == null || _healthBar == null || _row == null)
            {
                return;
            }
            byte[]? stored = PouchStore.Raw(zdo);
            float starEnd = IconRow.StarRowEnd(_healthBar);
            if (_drawn && PouchStore.Same(stored, _lastPouch) && Mathf.Approximately(starEnd, _lastStarEnd))
            {
                return;
            }
            _drawn = true;
            _lastPouch = stored;
            _lastStarEnd = starEnd;
            float size = IconRow.VanillaSize * Configuration.StolenIconSize.Value;
            _row.Layout(Sprites(PouchStore.Load(zdo)), starEnd, _healthBar.rect.width, size);
        }

        // The newest HardCap items' own inventory icons, oldest first.
        private static List<Sprite?> Sprites(List<PouchStore.Entry> pouch)
        {
            List<Sprite?> sprites = new List<Sprite?>();
            for (int i = Mathf.Max(0, pouch.Count - PouchStore.HardCap); i < pouch.Count; i++)
            {
                sprites.Add(pouch[i].Item.GetIcon());
            }
            return sprites;
        }

        private void OnDestroy() => _row?.Clear();
    }
}
