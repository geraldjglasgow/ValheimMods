using System;
using BundlePrefabs;
using LocalEffects;
using PatchGuard;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// A small fire that always burns, for show only (the tar pot, the steam box): the game campfire's full fire
    /// (<c>fire_pit</c>'s Fireplace <c>m_enabledObjectHigh</c>: flames, sparks, smoke wisps, light and crackle) copied at the
    /// model's <c>fire</c> point and sized to the piece (<see cref="FireScale"/>; the steam box's small enough to lick its
    /// kettle's bottom without wrapping it); and where the piece steams (<see cref="SteamScale"/>), the game cauldron's
    /// steam (<c>piece_cauldron</c>'s <c>HaveFire/steam</c>) at its <c>steam</c> point, rising. Both are LocalEffects copies
    /// (never networked, nothing of them can hurt) with only what is seen and heard kept: the warmth and burn areas, any
    /// area damage, the smoke spawner, the cinders and the campfire's ground glow go (the model has embers of its own,
    /// <c>glow_embers</c>, always glowing), so there is no fuel, no heat, no burning, no cooking and no rest or comfort. The
    /// light casts no shadow and reaches <see cref="LightRange"/>; the crackle is quieter. Made on each screen as the piece
    /// wakes there, from nothing kept or sent, so every player sees the same; nothing on a dedicated server or a ghost.
    /// </summary>
    public sealed class KwDockHearth : MonoBehaviour
    {
        public const string SteamPoint = "steam";
        private const string FirePit = "fire_pit", Cauldron = "piece_cauldron", SteamPath = "HaveFire/steam";
        private const float Brightness = 1.5f, Crackle = 0.5f, LightReach = 40f;
        private static bool warned;

        /// <summary>The fire's size against the game's campfire; set on the bench copy.</summary>
        public float FireScale = 0.3f;

        /// <summary>How far its light reaches, metres.</summary>
        public float LightRange = 4f;

        /// <summary>The steam's size against the game cauldron's; 0: no steam.</summary>
        public float SteamScale;

        /// <summary>The <see cref="KwDockDef.Fit"/> putting a hearth of these sizes on the piece.</summary>
        public static Action<GameObject, GameObject> Fit(float fire, float light, float steam) => (piece, look) =>
        {
            KwDockHearth hearth = piece.AddComponent<KwDockHearth>();
            (hearth.FireScale, hearth.LightRange, hearth.SteamScale) = (fire, light, steam);
        };

        private void Start()
        {
            enabled = false;
            if (KwIcons.CanRender && Placed())
                Guard.Run("Broadside dockside fire", static h => h.Kindle(), this);
        }

        // A piece built in the world, not a placement ghost (its view has no ZDO).
        private bool Placed()
        {
            ZNetView? view = GetComponent<ZNetView>();
            return view != null && view.GetZDO() != null;
        }

        private void Kindle()
        {
            Transform? fire = GameMaterials.Find(transform, KwDockFire.PointName);
            if (fire != null)
                Burn(fire);
            Transform? steam = SteamScale > 0f ? GameMaterials.Find(transform, SteamPoint) : null;
            if (steam != null)
                Steam(steam);
        }

        private void Burn(Transform point)
        {
            GameObject? fire = LocalEffect.AttachScaled(Source(FirePit, null), point, point.position, endless: true, FireScale);
            if (fire == null)
                return;
            fire.transform.localRotation = Quaternion.identity;
            Tame(fire);
            Lights(fire);
            foreach (ZSFX sound in fire.GetComponentsInChildren<ZSFX>(true))
                sound.SetVolumeModifier(Crackle);
            fire.SetActive(true);
        }

        // The steam turned as it stands in the cauldron (its cone up), on the piece.
        private void Steam(Transform point)
        {
            GameObject? source = Source(Cauldron, SteamPath);
            GameObject? steam = LocalEffect.AttachScaled(source, point, point.position, endless: true, SteamScale);
            if (steam == null || source == null)
                return;
            steam.transform.localRotation = source.transform.localRotation;
            steam.SetActive(true);
        }

        // The campfire's full fire, or the cauldron's steam; null (warned once) when the game has none.
        private static GameObject? Source(string prefab, string? path)
        {
            GameObject? game = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            GameObject? source = game == null ? null : path == null ? game.GetComponent<Fireplace>()?.m_enabledObjectHigh : game.transform.Find(path)?.gameObject;
            if (source == null && !warned)
            {
                warned = true;
                Log.Warn($"Broadside: the game's {prefab}{(path == null ? "'s fire" : "/" + path)} was not found; the tar pot and steam box show their embers only.");
            }
            return source;
        }

        /// <summary>
        /// Only what is seen and heard: the warmth, burn and shelter areas, area damage, the smoke spawner (its smoke balls
        /// gather under roofs), cinders and the ground glow are taken off at once, before the copy wakes.
        /// </summary>
        private static void Tame(GameObject fire)
        {
            Remove(fire, fire.GetComponentsInChildren<EffectArea>(true));
            Remove(fire, fire.GetComponentsInChildren<Aoe>(true));
            Remove(fire, fire.GetComponentsInChildren<SmokeSpawner>(true));
            Remove(fire, fire.GetComponentsInChildren<CinderSpawner>(true));
            foreach (MeshRenderer glow in fire.GetComponentsInChildren<MeshRenderer>(true))
                Object.DestroyImmediate(glow);
        }

        private static void Remove(GameObject root, Component[] parts)
        {
            foreach (Component part in parts)
            {
                if (part != null)
                    Object.DestroyImmediate(part.gameObject == root ? (Object)part : part.gameObject);
            }
        }

        // No shadow (a small fire, maybe many), the piece's reach and brightness, lit within 40 m (the game's light limit kept).
        private void Lights(GameObject fire)
        {
            foreach (Light light in fire.GetComponentsInChildren<Light>(true))
                (light.shadows, light.range, light.intensity) = (LightShadows.None, LightRange, Brightness);
            foreach (LightLod lod in fire.GetComponentsInChildren<LightLod>(true))
                (lod.m_lightDistance, lod.m_shadowLod) = (LightReach, false);
        }
    }
}
