using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a launched ship breaking up looks and sounds: the game's wood break effects (a wood floor's when it is destroyed:
    /// sawdust and the sound of wood breaking) all along the hull at its deck, the sound at its ends and middle only, made
    /// on the ship's owner as the game makes a destroyed piece's (networked effects, so every player sees and hears them);
    /// with them the ship's own destroyed sound, the karve's, which the game plays as it destroys the ship. The karve's
    /// broken hull is not played (<see cref="Kept"/>): it is the karve's shape, not this ship's.
    /// </summary>
    public static class KwShipBreakLook
    {
        private const string Wood = "wood_floor", KarveWreck = "vfx_Destroyed_Karve";
        private const float Every = 3f;
        private const int Most = 8;

        /// <summary>A destroyed effect without the karve's own broken hull (for the ship's prefab, <see cref="KwShipPrefab"/>).</summary>
        public static EffectList Kept(EffectList effect) =>
            new EffectList { m_effectPrefabs = effect.m_effectPrefabs.Where(e => e.m_prefab == null || e.m_prefab.name != KarveWreck).ToArray() };

        /// <summary>On the ship's owner: the wood breaking all along the hull.</summary>
        public static void Play(Transform ship, KwBpHull hull)
        {
            GameObject? floor = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Wood) : null;
            WearNTear? wear = floor != null ? floor.GetComponent<WearNTear>() : null;
            if (wear == null)
                return;
            EffectList dust = Only(wear.m_destroyedEffect, false), sound = Only(wear.m_destroyedEffect, true);
            int count = Mathf.Clamp(Mathf.CeilToInt(hull.Length / Every), 2, Most);
            for (int i = 0; i < count; i++)
            {
                Vector3 at = ship.TransformPoint(new Vector3(0f, hull.Keel + hull.DeckTop, hull.Aft + hull.Length * (i + 0.5f) / count));
                dust.Create(at, ship.rotation);
                if (i == 0 || i == count - 1 || i == count / 2)
                    sound.Create(at, ship.rotation);
            }
        }

        // An effect's sounds only (the game's sfx_ prefabs), or all but its sounds.
        private static EffectList Only(EffectList effect, bool sounds) =>
            new EffectList { m_effectPrefabs = effect.m_effectPrefabs.Where(e => e.m_prefab != null && e.m_prefab.name.StartsWith("sfx_") == sounds).ToArray() };
    }
}
