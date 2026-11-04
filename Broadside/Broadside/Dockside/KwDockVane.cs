using BundlePrefabs;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The weather vane's plate turning to the wind: the model's <c>vane</c> part (its pivot on the post's axis, its plate
    /// trailing toward the post's -Z at rest) turns about the post's axis so the plate trails downwind, the way the game's
    /// wind blows (<see cref="EnvMan.GetWindDir"/>, the same for everyone), eased like a vane on its spindle: slower in a
    /// light wind, quicker in a strong one, swinging a few degrees in gusts. Everyone sees it from the game's wind alone:
    /// nothing is kept or sent. Every frame within <see cref="Near"/>, ten times a second beyond, not at all beyond
    /// <see cref="Range"/> or out of view; switched off on a dedicated server, which draws nothing.
    /// </summary>
    public sealed class KwDockVane : MonoBehaviour
    {
        public const string PartName = "vane";
        private const float Near = 40f, Range = 150f, Far = 0.1f, MaxTurn = 120f;

        private Transform? vane;
        private Renderer? look;
        private Quaternion rest;
        private float yaw, turning, wait, last, phase;
        private bool broken;

        /// <summary>Put on the vane's piece (<see cref="KwDockDef.Fit"/>); it finds its part when it wakes in the world.</summary>
        public static void Fit(GameObject piece, GameObject look) => piece.AddComponent<KwDockVane>();

        private void Start()
        {
            vane = KwIcons.CanRender ? GameMaterials.Find(transform, PartName) : null;
            if (vane == null || vane.parent == null)
            {
                enabled = false;
                return;
            }
            rest = vane.localRotation;
            look = vane.GetComponentInChildren<Renderer>();
            // each vane swings on its own beat, the same on every screen
            phase = Mathf.Repeat(transform.position.x * 0.37f + transform.position.z * 0.61f, 2f * Mathf.PI);
            EnvMan? env = EnvMan.instance;
            yaw = env != null ? Downwind(env.GetWindDir()) : 0f;
            last = Time.time;
            Turn();
        }

        // A throw is reported once and stops this vane, rather than once a frame.
        private void Update()
        {
            if (broken || vane == null)
                return;
            wait -= Time.deltaTime;
            if (wait > 0f)
                return;
            Camera? eye = Utils.GetMainCamera();
            float far = eye != null ? (eye.transform.position - transform.position).sqrMagnitude : float.MaxValue;
            wait = far > Near * Near ? Far : 0f;
            if ((look != null && !look.isVisible) || far > Range * Range)
                return;
            broken = true;
            Guard.Run("Broadside weather vane", static v => v.Step(), this);
            broken = false;
        }

        // Eased toward the wind since the last step, a few degrees of gust on top.
        private void Step()
        {
            float now = Time.time, dt = Mathf.Min(now - last, 0.5f);
            last = now;
            EnvMan? env = EnvMan.instance;
            if (env == null || dt <= 0f)
                return;
            float strength = Mathf.Clamp01(env.GetWindIntensity());
            float gust = strength * 4f * (Mathf.Sin(now * 1.3f + phase) + 0.5f * Mathf.Sin(now * 3.1f + 2f * phase));
            float target = Downwind(env.GetWindDir()) + gust;
            yaw = Mathf.SmoothDampAngle(yaw, target, ref turning, Mathf.Lerp(2.5f, 0.8f, strength), MaxTurn, dt);
            Turn();
        }

        /// <summary>
        /// The turn about the post's axis (degrees) that lays the plate along the wind: at a turn of θ the plate, toward -Z
        /// at rest, points along (-sin θ, -cos θ) in the post's axes, so θ = atan2(-x, -z) of the wind there. A wind
        /// straight up or down the post (or none) keeps the turn it has.
        /// </summary>
        private float Downwind(Vector3 wind)
        {
            Vector3 local = vane!.parent.InverseTransformDirection(wind);
            if (local.x * local.x + local.z * local.z < 1e-6f)
                return yaw;
            return Mathf.Atan2(-local.x, -local.z) * Mathf.Rad2Deg;
        }

        private void Turn() => vane!.localRotation = Quaternion.AngleAxis(yaw, Vector3.up) * rest;
    }
}
