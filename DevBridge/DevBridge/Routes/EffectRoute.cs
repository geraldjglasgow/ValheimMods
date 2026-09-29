using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Stage;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Routes
{
    /// <summary>
    /// /effect and /sound: particle effects and sounds, the workshop's or the game's own, played at the row or a point on
    /// this machine only (as the LocalEffects library plays them: nothing that hurts, flies or syncs).
    /// </summary>
    internal static class EffectRoute
    {
        private const float SoundLife = 10f;

        internal static void Register(Router router)
        {
            router.Add("/effect",
                "/effect?asset=<bundle prefab>&bundle=|game=<vfx_/fx_ prefab>&at=<id>|x,y,z&offset=x,y,z&scale=1&life=<s>&dress=...\n" +
                "                       play a particle effect at a placed object's centre (at=<id>), a point, or the row; scale resizes\n" +
                "                       every particle system with the root; life= removes it after that many seconds",
                Effect);
            router.Add("/sound",
                "/sound?game=<sfx_ prefab>|clip=<bundle AudioClip>&base=<sfx_ prefab>&at=<id>|x,y,z&life=<s>\n" +
                "                       play a game sound, or a bundle clip through a copy of a game sound (base=): its mixer group, 3D\n" +
                "                       rolloff, pitch and volume spread and reverb; heard from the camera (the free camera's too)",
                Sound);
        }

        private static void Effect(BridgeRequest request)
        {
            GameObject prefab = request.Has("game") ? GamePrefabs.Require(request.Get("game")) : null;
            LoadedBundle from = null;
            if (!prefab) prefab = Bundles.Asset<GameObject>(request.Get("asset") ?? throw new BridgeException("give asset= (a bundle prefab) or game= (a game effect)"), request.Get("bundle"), out from);
            float scale = request.Float("scale", 1f);
            GameObject copy = LocalCopy.Harmless(prefab, Point(request), Quaternion.LookRotation(Facing()), bench => Resize(bench, scale));
            Placement placement = Placements.Add(Placement.Effect, prefab.name, from?.Name, copy);
            if (request.Has("dress")) Dress.Add(placement, Dress.Spec(request, request.Require("dress")));
            if (request.Has("life")) Object.Destroy(copy, request.Float("life", 5f));
            Dictionary<string, object> info = Placements.Describe(placement);
            info["systems"] = copy.GetComponentsInChildren<ParticleSystem>(true).Select(SystemLine).ToList();
            request.Json(info);
        }

        // Most game effects scale each particle system by its own transform alone; this makes every one follow the root.
        private static void Resize(GameObject copy, float scale)
        {
            if (Mathf.Approximately(scale, 1f)) return;
            foreach (ParticleSystem system in copy.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            copy.transform.localScale *= scale;
        }

        private static string SystemLine(ParticleSystem system)
        {
            ParticleSystem.MainModule main = system.main;
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            string look = renderer && renderer.sharedMaterial ? $"{renderer.sharedMaterial.name} [{renderer.sharedMaterial.shader.name}]" : "no material";
            return $"{system.name}: {main.duration:0.##} s{(main.loop ? " looping" : "")}, max {main.maxParticles}, {look}";
        }

        private static void Sound(BridgeRequest request)
        {
            AudioClip clip = request.Has("clip") ? Bundles.Asset<AudioClip>(request.Get("clip"), request.Get("bundle"), out _) : null;
            string baseName = clip ? request.Get("base") : request.Get("game");
            if (baseName == null)
                throw new BridgeException(clip ? "give base=<sfx_ prefab>: the game sound whose mixer, 3D settings and spread the clip plays with" : "give game=<sfx_ prefab> or clip=<bundle clip>&base=<sfx_ prefab>");
            GameObject prefab = GamePrefabs.Require(baseName);
            if (!prefab.GetComponentInChildren<ZSFX>(true)) throw new BridgeException($"{baseName} has no ZSFX sound player (search sfx_ prefabs with /prefabs?kind=sfx)");
            GameObject copy = LocalCopy.Harmless(prefab, Point(request), Quaternion.identity, bench => Clip(bench, clip));
            float life = request.Float("life", clip ? clip.length / MinPitch(copy) + 0.5f : SoundLife);
            Object.Destroy(copy, life);
            Placement placement = Placements.Add(Placement.Sound, clip ? clip.name : prefab.name, null, copy);
            request.Json(Heard(placement, copy, clip, life));
        }

        // The clip replaces the game sound's own; its timer goes, so a clip longer than the game's is not cut short.
        private static void Clip(GameObject copy, AudioClip clip)
        {
            if (!clip) return;
            foreach (ZSFX sfx in copy.GetComponentsInChildren<ZSFX>(true)) sfx.m_audioClips = new[] { clip };
            LocalCopy.Purge(copy.GetComponentsInChildren<TimedDestruction>(true));
        }

        private static float MinPitch(GameObject copy) => Mathf.Max(0.1f, copy.GetComponentsInChildren<ZSFX>(true).Select(s => s.m_minPitch).DefaultIfEmpty(1f).Min());

        private static Dictionary<string, object> Heard(Placement placement, GameObject copy, AudioClip clip, float life)
        {
            ZSFX sfx = copy.GetComponentInChildren<ZSFX>(true);
            AudioSource source = sfx.GetComponent<AudioSource>();
            return new Dictionary<string, object>
            {
                ["id"] = placement.Id,
                ["position"] = Fmt.V3(copy.transform.position),
                ["clips"] = sfx.m_audioClips.Where(c => c).Select(AssetInfo.Line).ToList(),
                ["base"] = clip ? Utils.GetPrefabName(copy) : null,
                ["mixer"] = source && source.outputAudioMixerGroup ? source.outputAudioMixerGroup.name : null,
                ["3d"] = source ? $"blend {source.spatialBlend:0.##}, {source.rolloffMode} rolloff {source.minDistance:0.#}-{source.maxDistance:0.#} m, loop {source.loop}" : null,
                ["spread"] = $"pitch {sfx.m_minPitch:0.##}-{sfx.m_maxPitch:0.##}, volume {sfx.m_minVol:0.##}-{sfx.m_maxVol:0.##}, delay {sfx.m_minDelay:0.##}-{sfx.m_maxDelay:0.##} s",
                ["life"] = Fmt.R(life),
            };
        }

        /// <summary>at=<id> (the placed object's centre), at=x,y,z, or the row's middle a metre up; offset=x,y,z moves it.</summary>
        private static Vector3 Point(BridgeRequest request)
        {
            Vector3 offset = request.Has("offset") ? Fmt.ParseV3(request.Get("offset"), "offset") : Vector3.zero;
            string at = request.Get("at");
            if (at != null && at.Contains(",")) return Fmt.ParseV3(at, "at") + offset;
            if (at != null) return AssetInfo.Bounds(Placements.Get(request.Int("at", 0)).Root).center + offset;
            if (Placements.Bounds(Placements.All, out Bounds box)) return new Vector3(box.center.x, box.min.y + 1f, box.center.z) + offset;
            Player player = Player.m_localPlayer ? Player.m_localPlayer : throw new BridgeException("no local player");
            return player.transform.position + player.transform.forward * 3f + Vector3.up + offset;
        }

        private static Vector3 Facing() => Row.Started ? Row.Front : Vector3.forward;
    }
}
