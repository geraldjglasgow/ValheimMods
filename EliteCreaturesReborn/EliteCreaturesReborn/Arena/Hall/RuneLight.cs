using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// One floor rune's light for <see cref="HallRunes"/>: its own material, its lit colour, and where it is in its
    /// fade in, hold and fade out. A struct in the runes' array, stepped in place; a rune at rest is not stepped.
    /// </summary>
    internal struct RuneLight
    {
        private enum Phase { Idle, Rising, Holding, Falling }

        public readonly Material Material;
        private readonly Color _lit;
        private Phase _phase;
        private float _level;
        private float _peak;
        private float _speed;
        private float _hold;
        private float _holdUntil;
        private float _releaseAt;

        public RuneLight(Material material, Color lit)
        {
            Material = material;
            _lit = lit;
            _phase = Phase.Idle;
            _level = _peak = _speed = _hold = _holdUntil = 0f;
            _releaseAt = float.PositiveInfinity;
        }

        public bool Idle => _phase == Phase.Idle;

        /// <summary>Lights the rune to <paramref name="peak"/> times its colour over <paramref name="fade"/> seconds, then
        /// holds it for <paramref name="hold"/> seconds (infinity until released). A fading rune rises from where it is.</summary>
        public void Start(float peak, float fade, float hold, float now)
        {
            _peak = peak;
            _speed = 1f / Mathf.Max(0.05f, fade);
            _hold = hold;
            _releaseAt = float.PositiveInfinity;
            _holdUntil = now + hold;
            _phase = Phase.Rising; // a full rune rises no further: its first step holds it, at the new peak
        }

        /// <summary>A held rune lets go at <paramref name="at"/> (a rising one as soon as it is full).</summary>
        public void Release(float at)
        {
            _releaseAt = at;
            _holdUntil = Mathf.Min(_holdUntil, at);
        }

        public void Step(float dt, float now, int emission)
        {
            switch (_phase)
            {
                case Phase.Rising:
                    _level = Mathf.MoveTowards(_level, 1f, dt * _speed);
                    if (_level >= 1f)
                    {
                        _phase = Phase.Holding;
                        _holdUntil = Mathf.Min(now + _hold, _releaseAt);
                    }
                    break;
                case Phase.Holding:
                    _phase = now >= _holdUntil ? Phase.Falling : Phase.Holding;
                    return;
                case Phase.Falling:
                    _level = Mathf.MoveTowards(_level, 0f, dt * _speed);
                    _phase = _level <= 0f ? Phase.Idle : Phase.Falling;
                    break;
            }
            Material.SetColor(emission, _lit * (_peak * Mathf.SmoothStep(0f, 1f, _level)));
        }
    }
}
