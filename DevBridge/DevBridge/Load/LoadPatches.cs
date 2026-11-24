using System;
using System.Collections.Generic;
using DevBridge.Events;
using HarmonyLib;
using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>
    /// The patches that exist only while there are fake players, under their own Harmony id: on with the first fake, off
    /// after the last (<see cref="FakeTicker"/>). ZNet.Shutdown and ZNet.ShutdownWithoutSave (a logout, the game or the
    /// server closing): every fake leaves through the game's own disconnect before the world is saved. ZNet.OnDestroy:
    /// the fakes are forgotten if the world goes without either. ZNet.UpdatePlayerHistory: the fakes are taken out of the
    /// player list while the game copies it into the world's player history (saved in the world's .fwl) and Steam's
    /// recent players, and put back where they were after, so the list sent to every player has them and the history
    /// never does. ZNet.InternalKick(ZNetPeer), where every kick ends: a fake is never kicked (a permitted list kicks
    /// everyone not on it every five seconds); remove=all takes the fakes off. Each patch catches its own failures, so the
    /// game's own code goes on.
    /// </summary>
    internal static class LoadPatches
    {
        private const string HarmonyId = DevBridgePlugin.PluginGuid + ".load";
        private static readonly List<KeyValuePair<int, ZNet.PlayerInfo>> Hidden = new List<KeyValuePair<int, ZNet.PlayerInfo>>();
        private static Harmony harmony;

        internal static bool On => harmony != null;

        internal static void Apply()
        {
            if (harmony != null) return;
            harmony = new Harmony(HarmonyId);
            Hook(nameof(ZNet.Shutdown), null, nameof(BeforeShutdown));
            Hook(nameof(ZNet.ShutdownWithoutSave), null, nameof(BeforeShutdown));
            Hook(nameof(ZNet.OnDestroy), null, nameof(BeforeDestroy));
            Hook(nameof(ZNet.UpdatePlayerHistory), null, nameof(HideFakes), nameof(RestoreFakes));
            Hook(nameof(ZNet.InternalKick), new[] { typeof(ZNetPeer) }, nameof(BeforeKick));
        }

        internal static void Remove()
        {
            if (harmony == null) return;
            harmony.UnpatchSelf();
            harmony = null;
        }

        private static void Hook(string method, Type[] parameters, string prefix, string finalizer = null)
        {
            try
            {
                var before = new HarmonyMethod(typeof(LoadPatches), prefix);
                HarmonyMethod after = finalizer == null ? null : new HarmonyMethod(typeof(LoadPatches), finalizer);
                harmony.Patch(AccessTools.Method(typeof(ZNet), method, parameters), prefix: before, finalizer: after);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[DevBridge] load: could not patch ZNet.{method} ({error.GetType().Name}: {error.Message}); " +
                                 "fake players work without it, with what it guards left to the game");
            }
        }

        private static void BeforeShutdown()
        {
            try
            {
                FakeCrowd.RemoveAll("the world is closing");
            }
            catch (Exception error)
            {
                Publish.WarnOnce("load (fake players leaving as the world closes)", error);
            }
        }

        private static void BeforeDestroy() => FakeCrowd.Forget("the world unloaded");

        private static void HideFakes(ZNet __instance)
        {
            Hidden.Clear();
            try
            {
                List<ZNet.PlayerInfo> players = __instance.m_players;
                for (int i = players.Count - 1; i >= 0; i--)
                {
                    if (!FakeCrowd.IsFake(players[i])) continue;
                    Hidden.Add(new KeyValuePair<int, ZNet.PlayerInfo>(i, players[i]));
                    players.RemoveAt(i);
                }
            }
            catch (Exception error)
            {
                Publish.WarnOnce("load (keeping fake players out of the world's player history)", error);
            }
        }

        // A finalizer, so the fakes go back into the list even when the game's own method throws. Taken out from the
        // end, put back from the front: each goes back at the index it had.
        private static Exception RestoreFakes(ZNet __instance, Exception __exception)
        {
            List<ZNet.PlayerInfo> players = __instance.m_players;
            for (int k = Hidden.Count - 1; k >= 0; k--) players.Insert(Mathf.Min(Hidden[k].Key, players.Count), Hidden[k].Value);
            Hidden.Clear();
            return __exception;
        }

        private static bool BeforeKick(ZNetPeer peer) => !FakeCrowd.IsFake(peer);
    }
}
