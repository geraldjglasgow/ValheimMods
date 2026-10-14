using System;
using BundlePrefabs;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The river's two effects from its bundle (<c>ecr_helarena_river</c>): the bubble cluster <c>ecr_helarena_bubble</c>
    /// (a one-shot of about 3 s, 51 particles at most, origin on the surface) and the mist <c>ecr_helarena_rivermist</c>
    /// (looping, prewarmed, about 12 cards). Prepared once per process by BundlePrefabs' BundleEffects: inactive copies on
    /// the prefab bench, their placeholders dressed in the game's particle shaders, as the archways' portal is. Clients
    /// copy them local-only (LocalEffects). Null without the bundle, logged once.
    /// </summary>
    internal static class HallRiverFx
    {
        private static bool _tried;
        private static GameObject? _bubble;
        private static GameObject? _mist;

        public static GameObject? Bubble
        {
            get
            {
                Prepare();
                return _bubble;
            }
        }

        public static GameObject? Mist
        {
            get
            {
                Prepare();
                return _mist;
            }
        }

        // After the scene wakes, so the game's particle shaders are loaded.
        private static void Prepare()
        {
            if (_tried || ZNetScene.instance == null)
            {
                return;
            }
            _tried = true;
            AssetBundle? bundle = HallBundles.Load(HallBundles.River);
            try
            {
                _bubble = bundle != null ? BundleEffects.Prepare(bundle, HallBundles.Bubble, "ecr_arena_bubble") : null;
                _mist = bundle != null ? BundleEffects.Prepare(bundle, HallBundles.RiverMist, "ecr_arena_rivermist") : null;
            }
            catch (Exception e)
            {
                Log.Error($"Hel's arena: the river's bubbles and mist did not load: {e.Message}");
            }
        }
    }
}
