using System;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The seconds of game time a beehive needs per honey under section 8, read at use time. Only hives whose product is
    /// the game's <c>Honey</c> item follow the settings; the bird nest (a <c>Beehive</c> making feathers) keeps its rate.
    /// </summary>
    public static class HiveRate
    {
        /// <summary>The honey item's prefab name.</summary>
        private const string HoneyPrefab = "Honey";

        /// <summary>EnvMan.m_dayLengthSec in the game's main scene (read from the assets); only used while EnvMan is missing.</summary>
        private const float SceneDayLength = 1800f;

        private static float loggedPerDay = -1f;

        /// <summary>Seconds per honey for this hive, or 0 when the game's own rate applies (settings off, or no honey).</summary>
        public static float SecondsPerHoney(Beehive hive)
        {
            if (!MakesHoney(hive))
                return 0f;
            float perDay = HoneyPerDay();
            LogChange(perDay);
            return perDay > 0f ? DayLength() / perDay : 0f;
        }

        /// <summary>The prefab's m_secPerUnit (the instance may carry this mod's rate), or 0 when the prefab is unknown.</summary>
        public static float VanillaSeconds(Beehive hive)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(hive.m_nview.GetZDO().GetPrefab()) : null;
            Beehive original = prefab != null ? prefab.GetComponent<Beehive>() : null;
            return original != null ? original.m_secPerUnit : 0f;
        }

        /// <summary>Honey per in-game day the settings ask for; 0 means the game's own rate.</summary>
        private static float HoneyPerDay()
        {
            if (HiveSettings.HoneyPerPlayerOnline.Value)
                return PlayersOnline();
            return Mathf.Max(0f, HiveSettings.HoneyPerDay.Value);
        }

        /// <summary>
        /// The server's player list: a client receives it from the server every two seconds, a host and a single player
        /// game build it with the local player in it. The machine running a hive always has a player online (a dedicated
        /// server's clock stands still without one), so the count is at least 1, also in the moment after joining.
        /// </summary>
        private static int PlayersOnline()
        {
            return ZNet.instance != null ? Math.Max(1, ZNet.instance.GetNrOfPlayers()) : 1;
        }

        private static bool MakesHoney(Beehive hive)
        {
            return hive.m_honeyItem != null && Utils.GetPrefabName(hive.m_honeyItem.gameObject) == HoneyPrefab;
        }

        private static float DayLength()
        {
            EnvMan env = EnvMan.instance;
            return env != null && env.m_dayLengthSec > 0 ? env.m_dayLengthSec : SceneDayLength;
        }

        /// <summary>One log line on this machine whenever the rate it runs hives at changes (a player joins, a setting).</summary>
        private static void LogChange(float perDay)
        {
            if (Mathf.Approximately(perDay, loggedPerDay))
                return;
            loggedPerDay = perDay;
            Plugin.Log.LogInfo(perDay > 0f
                ? $"Beehives owned here make {perDay:0.##} honey per day, one every {DayLength() / perDay:0.#} s of game time"
                : "Beehives owned here make honey at the game's own rate");
        }
    }
}
