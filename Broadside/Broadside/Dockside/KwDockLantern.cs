using BundlePrefabs;
using LocalEffects;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A lantern's light (the lantern pole's and the dock lantern's): a small torch flame burning in the oil cup inside the
    /// glass and a warm point light, both at the model's <c>light</c> point, lit from dusk to dawn and out by day (the wick's
    /// ember, the model's <c>glow_wick</c>, glows always on its own material). Night is the game's own
    /// (<see cref="EnvMan.IsNight"/>: a quarter of the day either side of midnight on the world's clock, which every peer
    /// shares), read every two seconds, so every screen agrees without anything kept or sent. The flame is a local copy of
    /// the game's wall torch's (<c>piece_walltorch</c>'s Fireplace <c>m_enabledObject</c>, its <c>fx_Torch_Basic</c>: the flame
    /// balls and their glow, none of the torch's light, warmth or sound) made through LocalEffects (never networked, nothing
    /// of it can hurt) at a fifth of its size (<see cref="FlameScale"/>), no particle wider than the lantern's inside
    /// (<see cref="Inside"/>), and sorted after the glass round it (<see cref="AfterGlass"/>), so it shows whole through the
    /// glass from every side. The light is the game's kind: it comes on and goes off with the player's distance and the
    /// game's light limit (<see cref="LightLod"/>, within 40 m), flickers a little like a flame and fades in at dusk
    /// (<see cref="LightFlicker"/>), and casts no shadow. Made on each screen as the piece wakes there; nothing on a dedicated
    /// server, which draws nothing, or on a placement ghost.
    /// </summary>
    public sealed class KwDockLantern : MonoBehaviour
    {
        public const string PointName = "light";
        private const string Torch = "piece_walltorch", Flames = "fx_Torch_Basic";
        private const float Check = 2f, Range = 7f, Brightness = 1.3f, Reach = 40f;
        // the flame against the wall torch's; its widest particle, metres (the glass is 21 cm across inside); its sorting
        // bias, metres nearer than it stands (the glass's middle is within 15 cm of the flame's)
        private const float FlameScale = 0.2f, Inside = 0.17f, AfterGlass = 1f;
        private static readonly Color Candle = new Color(1f, 0.72f, 0.42f);
        private static bool warned;

        private GameObject? flame, lamp;
        private float wait;
        private bool lit, broken;

        /// <summary>
        /// Put on the lantern pole's piece (<see cref="KwDockDef.Fit"/>) with what seats it on a dock's piling
        /// (<see cref="KwDockMooring"/>: snapped over a piling station, the curb cut round it) and leaves its cairn off there
        /// (<see cref="KwDockTrim"/>); it finds its parts when it wakes in the world.
        /// </summary>
        public static void Fit(GameObject piece, GameObject look)
        {
            piece.AddComponent<KwDockLantern>();
            piece.AddComponent<KwDockMooring>();
            KwDockTrim.Fit(piece);
        }

        private void Start()
        {
            ZNetView? view = GetComponent<ZNetView>();
            Transform? point = KwIcons.CanRender ? GameMaterials.Find(transform, PointName) : null;
            if (point == null || view == null || view.GetZDO() == null)
            {
                enabled = false;
                return;
            }
            lamp = Lamp(point);
            flame = Guard.Run("Broadside lantern flame", static at => Flame(at), point);
            // the opposite of now first, so the first look always switches both
            lit = !EnvMan.IsNight();
            Apply(!lit);
        }

        // A throw is reported once and stops this lantern, rather than every two seconds.
        private void Update()
        {
            wait -= Time.deltaTime;
            if (wait > 0f || broken)
                return;
            wait = Check;
            broken = true;
            Guard.Run("Broadside lantern pole", static l => l.Apply(EnvMan.IsNight()), this);
            broken = false;
        }

        private void Apply(bool night)
        {
            if (night == lit)
                return;
            lit = night;
            lamp!.SetActive(night);
            if (flame == null)
                return;
            flame.SetActive(night);
            ParticleSystem? burning = night ? flame.GetComponentInChildren<ParticleSystem>() : null;
            if (burning != null)
                burning.Play(true);
        }

        // The wall torch's flames, made switched off under the light point, upright; null without the game's torch.
        private static GameObject? Flame(Transform point)
        {
            GameObject? made = LocalEffect.AttachScaled(Source(), point, point.position, endless: true, FlameScale);
            if (made == null)
                return null;
            made.SetActive(false);
            made.transform.localRotation = Quaternion.identity;
            foreach (ParticleSystem system in made.GetComponentsInChildren<ParticleSystem>(true))
                Within(system);
            foreach (ParticleSystemRenderer drawn in made.GetComponentsInChildren<ParticleSystemRenderer>(true))
                drawn.sortingFudge -= AfterGlass;
            return made;
        }

        // The wall torch's flames (fx_Torch_Basic under the object its Fireplace shows while lit); null, warned once, without them.
        private static GameObject? Source()
        {
            GameObject? torch = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Torch) : null;
            Fireplace? fire = torch != null ? torch.GetComponent<Fireplace>() : null;
            Transform? flames = fire != null && fire.m_enabledObject != null ? fire.m_enabledObject.transform.Find(Flames) : null;
            if (flames == null && !warned)
            {
                warned = true;
                Log.Warn($"Broadside: the game's {Torch} has no {Flames}; lanterns light without a flame.");
            }
            return flames != null ? flames.gameObject : null;
        }

        // No particle wider than the lantern's inside: the flame balls are not at this size; the torch's glow (2 m, here 0.4 m) is held to it.
        private static void Within(ParticleSystem system)
        {
            ParticleSystem.MainModule main = system.main;
            ParticleSystem.MinMaxCurve size = main.startSize;
            if (size.mode == ParticleSystemCurveMode.Constant)
                size.constant = Mathf.Min(size.constant, Inside);
            else if (size.mode == ParticleSystemCurveMode.TwoConstants)
                (size.constantMin, size.constantMax) = (Mathf.Min(size.constantMin, Inside), Mathf.Min(size.constantMax, Inside));
            main.startSize = size;
        }

        // The candle's light, made switched off: its LightLod and LightFlicker read it when it first comes on.
        private static GameObject Lamp(Transform point)
        {
            GameObject lamp = new GameObject("lamp_light");
            lamp.SetActive(false);
            lamp.transform.SetParent(point, false);
            Light light = lamp.AddComponent<Light>();
            (light.type, light.color, light.range, light.intensity, light.shadows) = (LightType.Point, Candle, Range, Brightness, LightShadows.None);
            LightLod lod = lamp.AddComponent<LightLod>();
            (lod.m_lightDistance, lod.m_shadowLod) = (Reach, false);
            LightFlicker flicker = lamp.AddComponent<LightFlicker>();
            (flicker.m_flickerIntensity, flicker.m_flickerSpeed, flicker.m_movement, flicker.m_fadeInDuration) = (0.08f, 6f, 0.004f, 1.5f);
            return lamp;
        }
    }
}
