using System.Linq;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// Builds the Bone Ballista, its Bone Missiles and their flight once, and registers them whenever ZNetScene wakes,
    /// identically on the server and every client, whether the switch is on or not (ballistas already built always
    /// load). The bundle `ecp_boneballista` comes from ValheimAssets (Assets/Props/BoneBallista); the models wear the
    /// game ballista's body material. The sounds are the game's: the ballista's shot and added ammo, the crossbow's
    /// draw and latch.
    /// </summary>
    public static class BallistaPrefabs
    {
        public const string Bundle = "ecp_boneballista";
        private const string MissileModel = "ecp_bone_missile", BodyRenderer = "Base";
        private static bool built;

        public static void Install(Harmony harmony)
        {
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("BallistaPrefabs.Build", () => Build(scene, harmony)));
            Settings.Changed += () => SafeCall.Run("bone ballista settings", static db => BallistaCrafting.Refresh(db), ObjectDB.instance);
        }

        private static void Build(ZNetScene scene, Harmony harmony)
        {
            if (!built)
            {
                built = true;
                Create(scene, harmony);
            }
            foreach (GameObject prefab in new[] { BallistaPiece.Prefab, BallistaMissile.Item, BallistaMissile.Shot }.OfType<GameObject>())
            {
                NetPrefabs.Register(scene, prefab);
            }
            BallistaWords.Add(Localization.instance);
            BallistaCrafting.Refresh(ObjectDB.instance);
        }

        private static void Create(ZNetScene scene, Harmony harmony)
        {
            AssetBundle bundle = EmbeddedBundle.Load(typeof(BallistaPrefabs).Assembly, Bundle);
            BallistaMissile.Build(scene, bundle, GameMaterials.Borrow(scene.GetPrefab(BallistaPiece.GamePiece), BodyRenderer));
            BallistaPiece.Build(scene, bundle, EmbeddedBundle.Prefab(bundle, MissileModel));
            if (BallistaMissile.Item != null)
            {
                ItemPrefabs.Register(harmony, BallistaMissile.Item);
            }
            BallistaLook.Fire = Effect(scene, "fx_turret_fire");
            BallistaLook.Draw = Effect(scene, "sfx_reload_start");
            BallistaLook.Latch = Effect(scene, "sfx_reload_done");
            BallistaLook.Laid = Effect(scene, "fx_turret_addammo");
            Log.Info($"Bone ballista ready: {(BallistaPiece.Prefab != null ? "piece" : "no piece")}, {(BallistaMissile.Item != null ? "missiles" : "no missiles")}.");
        }

        private static EffectList? Effect(ZNetScene scene, string name)
        {
            GameObject? prefab = scene.GetPrefab(name);
            if (prefab == null)
            {
                Log.Warn($"Bone ballista: the game has no {name}; that sound is left out.");
                return null;
            }
            return new EffectList { m_effectPrefabs = new[] { new EffectList.EffectData { m_prefab = prefab, m_enabled = true } } };
        }
    }
}
