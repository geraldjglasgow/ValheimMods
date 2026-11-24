using System;
using System.Collections;
using System.Collections.Generic;
using DevBridge.Events;
using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>
    /// Runs the fake players every frame while there are any, as a coroutine on DevBridge's own object, after the game's
    /// updates (nothing is patched for it). Each frame, for each fake: its ZRpc's time since the last ping answer is set
    /// back to 0 (ZRpc.UpdatePing closes a connection whose pings have gone unanswered for 30 s, 90 with the long timeout,
    /// and a fake never answers); every <see cref="MoveInterval"/> its body takes a step along its path, as fast as a
    /// client sends its own objects; every <see cref="RefPosInterval"/> its reference position is updated on the server's
    /// peer, as a client's RPC_ServerSyncedPlayerData does; a verifying fake's waiting asks for whole objects go to the
    /// server (<see cref="FakeDeltas.Tick"/>). One ground height of one loop is read again each frame. A fake
    /// whose connection the game closed by itself (a mod's disconnect, a timeout after a long stall) is let go. Ends when
    /// the last fake has gone or the world they joined unloads, and then takes the fakes' patches off.
    /// </summary>
    internal static class FakeTicker
    {
        internal const float MoveInterval = 0.05f;
        internal const float RefPosInterval = 2f;
        private static bool running;
        private static int heightCursor;

        /// <summary>Starts the ticker when there are fakes and it is not running; with none, takes the patches off.</summary>
        internal static void Ensure()
        {
            if (running) return;
            if (FakeCrowd.All.Count == 0)
            {
                LoadPatches.Remove();
                return;
            }
            running = true;
            DevBridgePlugin.Instance.StartCoroutine(Run(ZNet.instance));
        }

        private static IEnumerator Run(ZNet net)
        {
            try
            {
                while (net && ZNet.instance == net && FakeCrowd.All.Count > 0)
                {
                    Step();
                    yield return null;
                }
            }
            finally
            {
                running = false;
                FakeCrowd.Forget("the world they joined is gone");
                LoadPatches.Remove();
            }
        }

        private static void Step()
        {
            try
            {
                float now = Time.time;
                List<FakePlayer> fakes = FakeCrowd.All;
                for (int i = fakes.Count - 1; i >= 0; i--)
                {
                    FakePlayer fake = fakes[i];
                    if (fake.Socket.Open) Tick(fake, now);
                    else FakeCrowd.Drop(fake);
                }
                RefreshHeight(fakes);
            }
            catch (Exception error)
            {
                Publish.WarnOnce("load (moving the fake players)", error);
            }
        }

        private static void Tick(FakePlayer fake, float now)
        {
            fake.Peer.m_rpc.m_timeSinceLastPing = 0f;
            FakeDeltas.Tick(fake);
            if (now >= fake.NextRefPos)
            {
                fake.Peer.m_refPos = fake.Path.Position(now);
                fake.NextRefPos = now + RefPosInterval;
            }
            if (fake.Path.Speed > 0f && now >= fake.NextMove)
            {
                FakeCharacter.Move(fake, now);
                fake.NextMove = now + MoveInterval;
            }
        }

        private static void RefreshHeight(List<FakePlayer> fakes)
        {
            int count = fakes.Count * FakePath.Samples;
            if (count == 0) return;
            heightCursor = (heightCursor + 1) % count;
            fakes[heightCursor / FakePath.Samples].Path.Refresh(heightCursor % FakePath.Samples);
        }
    }
}
