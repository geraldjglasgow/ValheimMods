using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Boss drops for players nearby. A drop the game gives once per player (CharacterDrop.Drop.m_onePerPlayer: a boss's
    /// own item, such as Eikthyr's antlers) counts every player online on the whole server (ZNet.GetNrOfPlayers, capped
    /// at 100), so on a big server one kill drops one for each of 50 players wherever they are. Here it counts the
    /// players whose character is within <see cref="Range"/> m of the creature when its drops are chosen, at least one:
    /// CharacterDrop.GenerateDropList, on the creature's owner as it dies (CharacterDrop.OnDeath) or as its ragdoll saves
    /// the loot (Ragdoll.SaveLootList); both use the list it returns. Only where this machine is the server, which by
    /// default runs every creature near a player; a creature a player's game runs (Creatures only where shared, a busy
    /// server) drops by that game's own rule. The list is put right after the game made it: only an entry for a
    /// once-per-player drop that holds the game's count of players online is changed.
    /// </summary>
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    internal static class PerPlayerDrops
    {
        private const float Range = 100f;
        private const int MaxAmount = 100;

        private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
        {
            if (__result == null || __result.Count == 0 || !Settings.DropsForPlayersNearby.Value)
            {
                return;
            }
            if (ZNet.instance != null && ZNet.instance.IsServer() && ZDOMan.instance != null)
            {
                Adjust(__instance, __result);
            }
        }

        private static void Adjust(CharacterDrop drop, List<KeyValuePair<GameObject, int>> list)
        {
            int online = Mathf.Min(ZNet.instance.GetNrOfPlayers(), MaxAmount);
            int near = -1;
            for (int i = 0; i < list.Count; i++)
            {
                KeyValuePair<GameObject, int> entry = list[i];
                if (entry.Value != online || !OncePerPlayer(drop, entry.Key))
                {
                    continue;
                }
                if (near < 0)
                {
                    near = Mathf.Clamp(PlayersNear(drop.transform.position), 1, MaxAmount);
                }
                list[i] = new KeyValuePair<GameObject, int>(entry.Key, near);
            }
        }

        private static bool OncePerPlayer(CharacterDrop drop, GameObject prefab)
        {
            foreach (CharacterDrop.Drop d in drop.m_drops)
            {
                if (d.m_onePerPlayer && d.m_prefab == prefab)
                {
                    return true;
                }
            }
            return false;
        }

        // The host's own character on a hosted game (none on a dedicated server), then every ready player's.
        private static int PlayersNear(Vector3 at)
        {
            ZNet net = ZNet.instance;
            int count = Near(net.LocalPlayerCharacterID, at) ? 1 : 0;
            foreach (ZNetPeer peer in net.GetPeers())
            {
                if (peer.IsReady() && Near(peer.m_characterID, at))
                {
                    count++;
                }
            }
            return count;
        }

        private static bool Near(ZDOID character, Vector3 at)
        {
            ZDO zdo = character.IsNone() ? null : ZDOMan.instance.GetZDO(character);
            return zdo != null && (zdo.GetPosition() - at).sqrMagnitude <= Range * Range;
        }
    }
}
