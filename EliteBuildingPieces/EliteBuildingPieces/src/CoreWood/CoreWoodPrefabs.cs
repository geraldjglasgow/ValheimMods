using System;
using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using UnityEngine;

namespace EliteBuildingPieces.CoreWood
{
    /// <summary>
    /// Makes the core wood pieces: each a copy of its game piece (<see cref="CoreWoodPieces"/>) wearing its own model
    /// (<see cref="CoreWoodModel"/>), named and costed as its own. Made once per process on every peer, dedicated server
    /// included, under BundlePrefabs' inactive bench (no Awake, no ZDO), and added to the scene's prefab list before
    /// ZNetScene.Awake registers it, whether the switch is on or not, so pieces already built always load.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class CoreWoodPrefabs
    {
        [HarmonyPrefix]
        private static void Prefix(ZNetScene __instance)
        {
            try
            {
                AddTo(__instance.m_prefabs);
            }
            catch (Exception e)
            {
                // Never out of ZNetScene.Awake: a throw there leaves the world loading for ever.
                Plugin.Log.LogError($"EliteBuildingPieces: the core wood pieces could not be made: {e}");
            }
        }

        private static void AddTo(List<GameObject> prefabs)
        {
            Material wood = null;
            foreach (CoreWoodPiece piece in CoreWoodPieces.All)
            {
                if (piece.Built == null)
                    piece.Built = Build(prefabs, piece, wood ??= CoreWoodModel.LogWall(prefabs));
                if (piece.Built != null && !prefabs.Contains(piece.Built))
                    prefabs.Add(piece.Built);
            }
        }

        private static GameObject Build(List<GameObject> prefabs, CoreWoodPiece piece, Material wood)
        {
            GameObject source = prefabs.Find(p => p != null && p.name == piece.Source);
            if (source == null || source.GetComponent<Piece>() == null)
            {
                Plugin.Log.LogWarning($"EliteBuildingPieces: the game's {piece.Source} was not found: there is no {piece.Prefab}");
                return null;
            }
            GameObject copy = PrefabBench.Copy(source, piece.Prefab);
            if (!CoreWoodModel.Wear(copy, piece, wood))
                Plugin.Log.LogWarning($"EliteBuildingPieces: {piece.Prefab} keeps the look of the game's {piece.Source}");
            Configure(copy, piece);
            return copy;
        }

        private static void Configure(GameObject copy, CoreWoodPiece piece)
        {
            Piece own = copy.GetComponent<Piece>();
            own.m_name = piece.NameToken;
            own.m_description = piece.DescriptionToken;
            own.m_enabled = true;
            Door door = copy.GetComponent<Door>();
            if (door != null)
                door.m_name = piece.NameToken;
            WearNTear wear = copy.GetComponent<WearNTear>();
            if (wear != null)
                wear.m_health *= piece.Width;
        }
    }
}
