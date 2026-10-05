using System.Collections.Generic;
using Splatform;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Places one blueprint piece the way <c>Player.PlacePiece</c> does (the game's object with its network view, this
    /// player as its creator, a ward set up for its owner, stations learned, item pieces made, every IPlaced told),
    /// without the swing, the sound and the stats of a hand-placed piece. <c>WearNTear.OnPlaced</c> is left out on
    /// purpose: a piece then waits 30 seconds before its first support check, as a piece just loaded from the save
    /// does, so the whole build and its ground stand before anything is checked.
    /// </summary>
    public static class PiecePlacer
    {
        private static readonly List<IPlaced> placed = new List<IPlaced>();

        /// <summary>The new piece's ZDO id, or ZDOID.None when the prefab is missing.</summary>
        public static ZDOID Place(Player player, Piece prefab, Vector3 position, Quaternion rotation, bool cheated)
        {
            TerrainModifier.SetTriggerOnPlaced(trigger: true);
            GameObject go;
            try
            {
                go = Object.Instantiate(prefab.gameObject, position, rotation);
            }
            finally
            {
                TerrainModifier.SetTriggerOnPlaced(trigger: false);
            }
            Setup(player, go, cheated);
            ZNetView view = go.GetComponent<ZNetView>();
            return view != null && view.GetZDO() != null ? view.GetZDO().m_uid : ZDOID.None;
        }

        private static void Setup(Player player, GameObject go, bool cheated)
        {
            CraftingStation station = go.GetComponentInChildren<CraftingStation>();
            if (station != null)
                player.AddKnownStation(station);
            go.GetComponent<Piece>()?.SetCreator(player.GetPlayerID(), PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            go.GetComponent<PrivateArea>()?.Setup(Game.instance.GetPlayerProfile().GetName());
            go.GetComponent<ItemDrop>()?.MakePiece(sendRPC: true);
            placed.Clear();
            go.GetComponents(placed);
            foreach (IPlaced item in placed)
                item.OnPlaced();
            if (cheated)
                go.GetComponent<ZNetView>()?.GetZDO()?.Set(ZDOVars.s_cheated, value: true);
        }
    }
}
