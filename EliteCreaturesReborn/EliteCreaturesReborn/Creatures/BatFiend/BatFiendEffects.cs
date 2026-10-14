using BundlePrefabs;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// Draws the Bat-fiend's attacks (<see cref="BatFiendFx"/>) on every machine with a screen, from what that machine
    /// already has: the animator plays the attack clip on every peer (the game's animation sync sends the attack trigger),
    /// so this watches the current state and its normalised time and plays each look once as the clip passes its moment,
    /// placed on the model's own bones and turned by the creature. The bite's blood wisp waits for the creature's health,
    /// which every peer reads from its ZDO, to rise after the bite lands: it shows only when the bite really healed it.
    /// Times are clip time, so a faster attack (Mad) plays them sooner; an attack cut short (staggered) plays no more.
    /// Nothing is sent over the network; a dedicated server, which has no screen, does not watch at all.
    /// </summary>
    public sealed class BatFiendEffects : MonoBehaviour
    {
        private enum Look { Kick, Land, Push, Plant, Swipe, Gust, Downdraft, Ripple, Wave }

        private readonly struct Beat
        {
            public readonly int State;
            public readonly float At;
            public readonly Look Look;

            /// <summary>A look at <paramref name="seconds"/> into a clip <paramref name="clip"/> seconds long.</summary>
            public Beat(string state, float clip, float seconds, Look look) =>
                (State, At, Look) = (Animator.StringToHash(state), seconds / clip, look);
        }

        private const string Pounce = "RakingPounce", Bite = "BloodlettingBite", Wing = "WingGuillotine", Wind = "GravewindBlast", Screech = "ShatteringScreech";

        /// <summary>Each attack's moments, in the clips' own seconds (ValheimAssets BatFiend animation timing).</summary>
        private static readonly Beat[] Beats =
        {
            new Beat(Pounce, 1.5f, 0.37f, Look.Kick),     // the leap leaves the ground
            new Beat(Pounce, 1.5f, 0.56f, Look.Land),     // claws down, the leap ends
            new Beat(Bite, 1.3f, 0.24f, Look.Push),       // the lunge pushes off the hind feet
            new Beat(Bite, 1.3f, 0.46f, Look.Plant),      // the hands plant as the bite lands
            new Beat(Wing, 1.4f, 0.42f, Look.Swipe),      // the wing sweeps across (hit at 0.467)
            new Beat(Wind, 1.5f, 0.50f, Look.Gust),       // the downbeat (hit at 0.567)
            new Beat(Wind, 1.5f, 0.53f, Look.Downdraft),
            new Beat(Screech, 2f, 0.70f, Look.Ripple),    // the scream, held to 1.4 s
            new Beat(Screech, 2f, 0.72f, Look.Wave),
            new Beat(Screech, 2f, 0.94f, Look.Wave),
            new Beat(Screech, 2f, 1.16f, Look.Wave),
        };

        private static readonly int BiteState = Animator.StringToHash(Bite);
        private const float BiteClip = 1.3f, BiteLands = 0.38f;   // health before this is the baseline (the hit is at 0.433 s)
        private const float HealSeen = 2f;                        // a rise smaller than this is not the bite's
        private const float Late = 0.1f;                          // a moment passed by more than this share of the clip is skipped
        private const float SeenFrom = 60f;                       // metres from the camera beyond which nothing is drawn
        private const float TunedAt = 1.2f;                       // the body's scale the looks are sized for (one star)
        private const float WingHeight = 1.5f, WingTurn = 5f, GustHeight = 1f, GustAhead = 0.4f, BeamHeight = 1.1f;

        private Animator? _animator;
        private Character? _character;
        private Transform? _mouth, _chest, _clawL, _clawR, _footL, _footR, _handL, _handR;
        private int _state, _played;
        private float _time, _lowest;
        private bool _drained;

        private void Awake()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                enabled = false;
                return;
            }
            _character = GetComponent<Character>();
            _animator = GetComponentInChildren<Animator>();
            Transform model = _animator != null ? _animator.transform : transform;
            (_mouth, _chest) = (Bone(model, "MouthOrigin", "Head"), Bone(model, "Chest"));
            (_clawL, _clawR) = (Bone(model, "ClawOrigin.L", "Hand.L"), Bone(model, "ClawOrigin.R", "Hand.R"));
            (_footL, _footR, _handL, _handR) = (Bone(model, "Foot.L"), Bone(model, "Foot.R"), Bone(model, "Hand.L"), Bone(model, "Hand.R"));
        }

        private static Transform? Bone(Transform model, string name, string? fallback = null) =>
            GameMaterials.Find(model, name) ?? (fallback != null ? GameMaterials.Find(model, fallback) : null);

        private void Update()
        {
            if (_animator == null)
            {
                return;
            }
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            float time = state.normalizedTime;
            if (state.shortNameHash != _state || time < _time)
            {
                Begin(state.shortNameHash, time);
            }
            _time = time;
            Play(time);
            WatchBite(time);
        }

        /// <summary>A new state, or the same attack again: nothing played yet, except moments already well past.</summary>
        private void Begin(int state, float time)
        {
            (_state, _played, _drained) = (state, 0, false);
            _lowest = _character != null ? _character.GetHealth() : 0f;
            for (int i = 0; i < Beats.Length; i++)
            {
                if (Beats[i].State == state && time > Beats[i].At + Late)
                {
                    _played |= 1 << i;
                }
            }
        }

        private void Play(float time)
        {
            for (int i = 0; i < Beats.Length; i++)
            {
                if (Beats[i].State != _state || (_played & (1 << i)) != 0 || time < Beats[i].At)
                {
                    continue;
                }
                _played |= 1 << i;
                if (Near())
                {
                    Show(Beats[i].Look);
                }
            }
        }

        private void Show(Look look)
        {
            float size = Size;
            Vector3 root = transform.position;
            switch (look)
            {
                case Look.Kick: BatFiendFx.Kick(Ground(Between(_footL, _footR, 0f)), size); break;
                case Look.Land: BatFiendFx.Land(Ground(Between(_clawL, _clawR, 1f)), size); break;
                case Look.Push: BatFiendFx.Step(Ground(Between(_footL, _footR, -0.5f)), size); break;
                case Look.Plant: BatFiendFx.Step(Ground(Between(_handL, _handR, 0.8f)), size); break;
                case Look.Swipe: BatFiendFx.Swipe(transform, root + Vector3.up * (WingHeight * size), Facing(WingTurn), size); break;
                case Look.Gust: BatFiendFx.Gust(transform, root + Vector3.up * (GustHeight * size) + Flat * (GustAhead * size), Facing(0f), size); break;
                case Look.Downdraft: BatFiendFx.Downdraft(root + Flat * (GustAhead * size), size); break;
                case Look.Ripple: BatFiendFx.Ripple(transform, Mouth, size); break;
                case Look.Wave: BatFiendFx.Wave(transform, Beam(size), Facing(0f), size); break;
            }
        }

        /// <summary>The bite's wisp, once, when the health every peer reads has risen since the bite landed.</summary>
        private void WatchBite(float time)
        {
            if (_state != BiteState || _drained || _character == null)
            {
                return;
            }
            float health = _character.GetHealth();
            if (time * BiteClip < BiteLands || health < _lowest)
            {
                _lowest = health;
                return;
            }
            if (health > _lowest + HealSeen)
            {
                _drained = true;
                Vector3 mouth = Mouth;
                Vector3 body = _chest != null ? _chest.position : transform.position + Vector3.up * (0.8f * Size);
                if (Near())
                {
                    BatFiendFx.Drain(transform, mouth, Quaternion.LookRotation(body - mouth + Vector3.up * 0.01f), Size);
                }
            }
        }

        /// <summary>The body's size against one star's, from the model's scale in the world (stars make it bigger).</summary>
        private float Size => _animator != null ? _animator.transform.lossyScale.y / TunedAt : 1f;

        /// <summary>The creature's forward, level.</summary>
        private Vector3 Flat
        {
            get
            {
                Vector3 forward = transform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            }
        }

        /// <summary>Level and forward, turned <paramref name="yaw"/> degrees to its right.</summary>
        private Quaternion Facing(float yaw) => Quaternion.LookRotation(Flat) * Quaternion.Euler(0f, yaw, 0f);

        private Vector3 Mouth => _mouth != null ? _mouth.position : transform.position + Vector3.up * (1.1f * Size) + Flat * (1.2f * Size);

        /// <summary>Where the screech's beam starts: under the jaws, at the beam's height.</summary>
        private Vector3 Beam(float size)
        {
            Vector3 mouth = Mouth;
            return new Vector3(mouth.x, transform.position.y + BeamHeight * size, mouth.z);
        }

        /// <summary>Halfway between two bones, or, without them, this far ahead of the root (in body sizes).</summary>
        private Vector3 Between(Transform? left, Transform? right, float ahead) =>
            left != null && right != null ? (left.position + right.position) * 0.5f : transform.position + Flat * (ahead * Size);

        /// <summary>The point brought down to the creature's feet: the leaps are drawn on the spot, the body stays grounded.</summary>
        private Vector3 Ground(Vector3 point) => new Vector3(point.x, transform.position.y, point.z);

        private bool Near()
        {
            Camera camera = Utils.GetMainCamera();
            return camera != null && (camera.transform.position - transform.position).sqrMagnitude < SeenFrom * SeenFrom;
        }
    }
}
