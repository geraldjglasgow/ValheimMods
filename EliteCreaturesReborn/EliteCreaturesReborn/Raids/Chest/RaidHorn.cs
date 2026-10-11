using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Util;
using LocalEffects;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The war horn on the Raiders Chest (features/raids.md sections 2 and 4.1), which sounds as a raid starts.
    /// <para>
    /// The game has no horn recording, and the user wants the game's own sounds rather than made ones, so the horn is the
    /// nearest real one: the lox's long, low bellow (<c>sfx_lox_shout</c>, a single clip at about 64 Hz, the game's
    /// large-sound mix and 50 m reach), played lower and slower than the lox does (pitch 0.75 to 0.8 against its 0.8 to
    /// 1.1) so it reads as one deep blast, and captioned "War horn". It is a copy of the game's prefab with those settings,
    /// never registered: each machine plays it on its own.
    /// </para>
    /// Contract (<see cref="RaidStart"/>): <see cref="Sound"/> is called once on the host's owner right after a raid is
    /// sounded. On a Raiders Chest one RPC (<see cref="RaidKeys.HornRpc"/> on the chest's ZNetView) goes to each player
    /// within the message range, the same peers that read the start (<see cref="RaidNet.PeersNear"/>), never to the
    /// whole server; each plays the horn at its bell (<see cref="RaidChest"/>). Any other host (the test marker) has no
    /// horn. Never throws.
    /// </summary>
    public static class RaidHorn
    {
        /// <summary>The caption's word in the translation table (<see cref="ChestWords"/>).</summary>
        public const string CaptionWord = "ecr_raid_horn";

        private const string GameSound = "sfx_lox_shout";
        private const string HornName = "ECR_RaidHorn";
        private const float MinPitch = 0.75f;
        private const float MaxPitch = 0.8f;

        private static GameObject? _sound;

        /// <summary>On the host's owner, as its raid is sounded: the horn blows for everybody holding the chest.</summary>
        public static void Sound(RaidRunner runner) => SafeCall.Run("raid horn", static r => Blow(r), runner);

        /// <summary>The horn heard on this machine at <paramref name="at"/>; nothing on a dedicated server.</summary>
        internal static void Play(Vector3 at) => LocalEffect.Sound(_sound, at);

        /// <summary>Makes the horn's sound from the game's, once, as the scene wakes (every peer).</summary>
        internal static void Prepare(List<GameObject> prefabs)
        {
            if (_sound != null)
            {
                return;
            }
            GameObject? game = ChestPrefab.Find(prefabs, GameSound);
            if (game == null)
            {
                Log.Warn($"the game has no {GameSound}: the Raiders Chest's horn is silent.");
                return;
            }
            _sound = PrefabBench.Copy(game, HornName);
            foreach (ZSFX sfx in _sound.GetComponentsInChildren<ZSFX>(true))
            {
                Tune(sfx);
            }
        }

        private static void Blow(RaidRunner runner)
        {
            ZNetView view = runner.View;
            if (view == null || !view.IsValid() || view.GetZDO().GetPrefab() != ChestPrefab.PrefabHash)
            {
                return;
            }
            foreach (long peer in RaidNet.PeersNear(runner))
            {
                view.InvokeRPC(peer, RaidKeys.HornRpc);
            }
        }

        private static void Tune(ZSFX sfx)
        {
            sfx.m_minPitch = MinPitch;
            sfx.m_maxPitch = MaxPitch;
            sfx.m_minVol = 1f;
            sfx.m_maxVol = 1f;
            sfx.m_closedCaptionToken = "$" + CaptionWord;
            sfx.m_secondaryCaptionToken = "";
            sfx.m_hash = HornName.GetStableHashCode(); // its own concurrency count, not the lox's
        }
    }
}
