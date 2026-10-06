using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// Draws what a Devouring creature has eaten on its nameplate, one icon per creature, the way
    /// <see cref="PouchIcons"/> draws what a thief carries: the eaten creature's trophy, or the game's horned monster head
    /// for one with no trophy (<see cref="MealSprites"/>). On StarRow's own line, right-justified - to the health bar's
    /// right edge, or just left of the pouch icons of a creature that is Thieving too - oldest leftmost, shrinking and
    /// then dropping the oldest when they do not all fit (<see cref="IconRow"/>). Read from the meal list on the
    /// creature's ZDO, so every client draws the same icons whoever owns it. Lives under the nameplate GameObject
    /// EnemyHudCloakPatch hides as a whole for a Cloaked creature, so it hides and fades with the rest of the plate.
    /// </summary>
    public sealed class MealIcons : MonoBehaviour, IPlateIcons
    {
        private const string Prefix = "ecr_meal_";
        private const float RefreshInterval = 0.5f; // a meal can come at any moment

        private Character _character = null!;
        private EliteController? _controller;
        private RectTransform _healthBar = null!;
        private IconRow? _row;
        private int _lastCount = -1;
        private float _lastStarEnd = -1f;
        private float _lastRight = -1f;
        private float _timer;

        public void Init(Character character, RectTransform healthBar)
        {
            _character = character;
            _healthBar = healthBar;
            _row = new IconRow(healthBar, Prefix);
        }

        private void Start() => Guard.Run("MealIcons.Start", static self => self.Refresh(), this);

        private void Update() => Guard.Run("MealIcons.Update", static self => self.Poll(), this);

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

        // Re-laid out only when something it depends on moved: a new meal, the star row, or the pouch icons beside it.
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
            float starEnd = IconRow.StarRowEnd(_healthBar);
            float right = IconRow.LeftOf(_healthBar, PouchIcons.Prefix);
            if (!Moved(MealStore.Count(zdo), starEnd, right))
            {
                return;
            }
            List<Sprite?> sprites = MealSprites.For(MealStore.Load(zdo));
            _row.Layout(sprites, starEnd, right, IconRow.VanillaSize * Configuration.DevouredIconSize.Value);
            if (sprites.Contains(null))
            {
                _lastCount = -1; // a picture not found yet (the map not built yet): look again next poll
            }
        }

        private bool Moved(int count, float starEnd, float right)
        {
            if (count == _lastCount && Mathf.Approximately(starEnd, _lastStarEnd) && Mathf.Approximately(right, _lastRight))
            {
                return false;
            }
            _lastCount = count;
            _lastStarEnd = starEnd;
            _lastRight = right;
            return true;
        }

        private void OnDestroy() => _row?.Clear();
    }
}
