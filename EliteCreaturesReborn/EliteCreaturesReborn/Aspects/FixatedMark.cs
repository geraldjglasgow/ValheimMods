using System.Collections.Generic;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Fixated's mark in the boss's ZDO, and finding the player it names. The mark is the marked player's character
    /// ZDOID: the ID every machine resolves a player by, and a new one after a death, so a player who dies is never still
    /// marked when they come back. Beside it, when it last landed on the shared clock. Only the boss's owner writes it;
    /// the victim's owner reads it to scale a hit, and every client to draw the eye and say the line. The key hashes are
    /// worked out once, because the eye reads the mark every frame.
    /// </summary>
    internal static class FixatedMark
    {
        private static readonly KeyValuePair<int, int> MarkKey = ZDO.GetHashZDOID(TraitKeys.Fixated);
        private static readonly int LandedKey = TraitKeys.FixatedAt.GetStableHashCode();

        public static ZDOID Get(ZDO zdo) => zdo.GetZDOID(MarkKey);

        /// <summary>Seconds since the mark last landed or moved, on the shared clock.</summary>
        public static float SecondsSinceLanded(ZDO zdo) => NetTime.SecondsSince(zdo.GetLong(LandedKey, 0L));

        /// <summary>Owner only. <see cref="ZDOID.None"/> clears the mark.</summary>
        public static void Set(ZDO zdo, ZDOID player)
        {
            zdo.Set(MarkKey, player);
            zdo.Set(LandedKey, NetTime.NowMs());
        }

        /// <summary>The marked player, when this machine has them loaded.</summary>
        public static Player? Resolve(ZDOID id)
        {
            GameObject? go = id != ZDOID.None && ZNetScene.instance != null ? ZNetScene.instance.FindInstance(id) : null;
            return go != null ? go.GetComponent<Player>() : null;
        }

        /// <summary>The marked player's name: from their character when it is loaded, else from the server's player list.</summary>
        public static string NameOf(ZDOID id)
        {
            Player? player = Resolve(id);
            if (player != null)
            {
                return player.GetPlayerName();
            }
            if (ZNet.instance != null)
            {
                foreach (ZNet.PlayerInfo info in ZNet.instance.GetPlayerList())
                {
                    if (info.m_characterID == id)
                    {
                        return info.m_name;
                    }
                }
            }
            return "a player who has left";
        }

        /// <summary>The line `elite inspect` prints for a Fixated boss.</summary>
        public static string Report(ZDO zdo)
        {
            ZDOID mark = Get(zdo);
            return mark == ZDOID.None
                ? "marked: nobody yet"
                : $"marked: {NameOf(mark)} ({SecondsSinceLanded(zdo):0} s ago)";
        }
    }
}
