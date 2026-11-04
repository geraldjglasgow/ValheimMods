using BundlePrefabs;
using PatchGuard;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The signal bell, rung with E: its <c>bell</c> part (yoke, bell, clapper and rope, pivoting on the axle at its origin,
    /// 2.3 m up) swings about its X axis, first toward the front where the ringer pulls the rope (a negative turn: a positive
    /// one swings the mouth and rope toward -Z, the HANDOFF's sign), eased in from where it hung and dying away over a few
    /// seconds; and the game's own bell sounds at it (the Fader's bell, <c>sfx_fader_bell</c>, pitched up by half for a
    /// smaller bell, and quieter; the game's metal clang if that is missing; never a made-up sound). A ring is
    /// one routed RPC to every peer (<see cref="ZNetView.Everybody"/>: the server passes it on, a peer without the bell
    /// loaded drops it), so every player near sees the same swing and hears the same bell; nothing is kept. A bell still
    /// swinging is not rung again (<see cref="Cooldown"/>), and a ring arriving within a second of the last is ignored. The
    /// rope and bell are aimed at through a box nothing collides with (<see cref="KwDockModel.HitBox"/>), as the posts are.
    /// A dedicated server only registers the RPC; a placement ghost does nothing.
    /// </summary>
    public sealed class KwDockBell : MonoBehaviour, Hoverable, Interactable
    {
        public const string PartName = "bell";
        private const string Rpc = "BS_dock_bell_ring", Name = "Signal bell";
        private const float Cooldown = 3f, Gap = 1f, Swing = 32f, Period = 1.8f, Decay = 1.6f, Ease = 0.3f, Length = 7f;
        private const float Pitch = 1.5f, Loudness = 0.45f, SoundLife = 20f;
        private static readonly string[] Sounds = { "sfx_fader_bell", "sfx_metal_blocked" };

        private ZNetView? view;
        private Transform? bell;
        private Quaternion rest;
        private float rungAt = -100f, from, angle;
        private bool broken;

        /// <summary>Put on the bell's piece (<see cref="KwDockDef.Fit"/>), with the box round the bell and rope as they hang.</summary>
        public static void Fit(GameObject piece, GameObject look)
        {
            piece.AddComponent<KwDockBell>();
            // the part reaches from the axle (2.3 m) down to the tassel (1.075 m), 0.21 m each way
            KwDockModel.HitBox(look.transform, "hit_bell", new Vector3(0f, 1.715f, 0f), new Vector3(0.42f, 1.28f, 0.42f));
        }

        private void Awake()
        {
            view = GetComponent<ZNetView>();
            enabled = false;
            if (view == null || view.GetZDO() == null)
                return;
            view.Register(Rpc, sender => Guard.Run("Broadside bell", Rung));
            bell = KwIcons.CanRender ? GameMaterials.Find(transform, PartName) : null;
            if (bell != null)
                rest = bell.localRotation;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || view == null || !view.IsValid())
                return false;
            if (Time.time - rungAt >= Cooldown)
                view.InvokeRPC(ZNetView.Everybody, Rpc);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText() => Localization.instance.Localize($"{Name}\n[<color=yellow><b>$KEY_Use</b></color>] Ring");

        public string GetHoverName() => Name;

        public float GetHoverOffset() => 0f;

        // On every peer the bell is loaded on, the ringer's too: the swing starts from where it hangs, the bell sounds.
        private void Rung()
        {
            if (!KwIcons.CanRender || Time.time - rungAt < Gap)
                return;
            (from, rungAt) = (angle, Time.time);
            enabled = bell != null;
            Clang(bell != null ? bell.position : transform.position + Vector3.up * 2f);
        }

        // A throw is reported once and stops this bell's swing, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside bell", static b => b.Step(), this);
            broken = false;
        }

        private void Step()
        {
            float t = Time.time - rungAt;
            angle = t >= Length ? 0f : Angle(t);
            bell!.localRotation = rest * Quaternion.Euler(angle, 0f, 0f);
            if (t >= Length)
                enabled = false;
        }

        // The turn t seconds after a ring: toward the front first, dying away; eased in, and out of where the last ring left it.
        private float Angle(float t)
        {
            float ease = Mathf.SmoothStep(0f, 1f, t / Ease);
            float swing = -Swing * Mathf.Exp(-t / Decay) * Mathf.Sin(2f * Mathf.PI * t / Period);
            return from * (1f - ease) + swing * ease;
        }

        /// <summary>
        /// The game's bell as the game plays an effect's sound (a plain copy of its sound prefab, which plays itself as it
        /// wakes and removes itself), pitched up and quieter: never networked, made on each peer the ring reaches.
        /// </summary>
        private static void Clang(Vector3 at)
        {
            GameObject? sound = Sound();
            if (sound == null)
                return;
            GameObject clang = Object.Instantiate(sound, at, Quaternion.identity);
            ZSFX? sfx = clang.GetComponent<ZSFX>();
            if (sfx != null)
            {
                // it rolls its pitch between these as it plays, a frame from now
                (sfx.m_minPitch, sfx.m_maxPitch) = (sfx.m_minPitch * Pitch, sfx.m_maxPitch * Pitch);
                sfx.SetVolumeModifier(Loudness);
            }
            Object.Destroy(clang, SoundLife);
        }

        private static GameObject? Sound()
        {
            ZNetScene? scene = ZNetScene.instance;
            foreach (string name in Sounds)
            {
                GameObject? found = scene != null ? scene.GetPrefab(name) : null;
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
