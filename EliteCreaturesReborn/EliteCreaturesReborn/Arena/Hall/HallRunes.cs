using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The floor's 24 runes lighting at random (features/arena.md section 1, "The runes"), clients only, on the floor.
    /// Each rune (<c>glow_rune_00</c>..<c>23</c>) gets its own copy of the shared rune material, dark at rest. Between
    /// trials one to three at a time fade in, hold one or two seconds and fade out; during a trial more of them at once
    /// and faster as the waves rise; on a boss wave every rune flares and stays lit until the wave ends. Only runes that
    /// are changing are touched each frame (a short list, swap-removed); nothing is allocated after setup, and nothing
    /// runs while the local player is far from the hall. The trial's state comes through <see cref="TrialView"/>.
    /// </summary>
    internal sealed class HallRunes : MonoBehaviour
    {
        private const string RunePrefix = "glow_rune_";
        private const float NearRange = 120f;
        private const float Lit = 2.5f;
        private const float Flare = 3.5f;
        private static readonly Color Fallback = new Color(0.165f, 0.592f, 0.545f);
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private RuneLight[] _runes = new RuneLight[0];
        private int[] _active = new int[0];
        private int _count;
        private float _nextStart;
        private int _quiet = 2;
        private bool _flaring;

        private void Awake()
        {
            var found = new List<RuneLight>();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial != null && IsRune(renderer.transform))
                {
                    found.Add(Instance(renderer));
                }
            }
            _runes = found.ToArray();
            _active = new int[_runes.Length];
            enabled = _runes.Length > 0;
        }

        private void OnDestroy()
        {
            foreach (RuneLight rune in _runes)
            {
                Destroy(rune.Material);
            }
        }

        private void Update()
        {
            Player? player = Player.m_localPlayer;
            if (player == null || (player.transform.position - ArenaHall.Origin).sqrMagnitude > NearRange * NearRange)
            {
                return;
            }
            TrialView.Refresh();
            if (TrialView.BossWave != _flaring)
            {
                SetFlare(TrialView.BossWave);
            }
            if (!_flaring)
            {
                StartMore();
            }
            Step(Time.deltaTime);
        }

        // A rune's inlay: the renderer on its glow_rune_NN child or under it.
        private bool IsRune(Transform part)
        {
            for (Transform? at = part; at != null && at != transform; at = at.parent)
            {
                if (at.name.StartsWith(RunePrefix, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        // One rune's own material, its emission switched on and dark; the material's own emission is its lit colour.
        private static RuneLight Instance(Renderer renderer)
        {
            var material = new Material(renderer.sharedMaterial);
            Color lit = material.HasProperty(EmissionColor) ? material.GetColor(EmissionColor) : Fallback;
            material.EnableKeyword("_EMISSION");
            material.SetColor(EmissionColor, Color.black);
            renderer.sharedMaterial = material;
            return new RuneLight(material, lit.maxColorComponent > 0.01f ? lit : Fallback);
        }

        // More runes at once as the waves rise: one to three between trials (drawn again as each one lights), then
        // 3 + a third of the wave during one (at most 12).
        private void StartMore()
        {
            int wanted = TrialView.Running ? Mathf.Min(12, 3 + TrialView.Wave / 3) : _quiet;
            if (_count >= wanted || Time.time < _nextStart)
            {
                return;
            }
            int index = IdleRune();
            if (index >= 0)
            {
                float pace = Pace();
                _runes[index].Start(Lit, 0.8f / pace, Random.Range(1f, 2f) / pace, Time.time);
                Track(index);
                _quiet = Random.Range(1, 4);
            }
            _nextStart = Time.time + Random.Range(0.2f, 0.8f) / Pace();
        }

        // Faster as the waves rise: 1 between trials, 3% quicker per wave, at most two and a half times.
        private static float Pace() => TrialView.Running ? Mathf.Min(2.5f, 1f + 0.03f * TrialView.Wave) : 1f;

        private int IdleRune()
        {
            int start = Random.Range(0, _runes.Length);
            for (int i = 0; i < _runes.Length; i++)
            {
                int index = (start + i) % _runes.Length;
                if (_runes[index].Idle)
                {
                    return index;
                }
            }
            return -1;
        }

        // A boss wave lights every rune and holds it; when the wave ends they fall away, a little apart.
        private void SetFlare(bool on)
        {
            _flaring = on;
            for (int i = 0; i < _runes.Length; i++)
            {
                if (on)
                {
                    _runes[i].Start(Flare, 0.35f, float.PositiveInfinity, Time.time);
                    Track(i);
                }
                else if (!_runes[i].Idle)
                {
                    _runes[i].Release(Time.time + Random.Range(0f, 0.8f));
                }
            }
        }

        private void Track(int index)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_active[i] == index)
                {
                    return;
                }
            }
            _active[_count++] = index;
        }

        private void Step(float dt)
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                int index = _active[i];
                _runes[index].Step(dt, Time.time, EmissionColor);
                if (_runes[index].Idle)
                {
                    _active[i] = _active[--_count];
                }
            }
        }
    }
}
