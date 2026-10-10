using System.Collections.Generic;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>The director's frame: film clock, camera clock, shot cues, slow-motion ramps, hidden actors, endpoint replies.</summary>
    internal sealed class DirectorDriver : MonoBehaviour
    {
        internal static void Ensure()
        {
            GameObject host = DevBridgePlugin.Instance.gameObject;
            if (!host.GetComponent<DirectorDriver>()) host.AddComponent<DirectorDriver>();
        }

        private void Update()
        {
            FilmClock.Tick();
            CameraRig.Tick(FilmClock.Delta);
            ShotRunner.Tick();
            TimeRamp.Tick();
            Hider.Tick();
            TitleCard.Tick(FilmClock.Delta);
            Eyelids.Tick(FilmClock.Delta);
            Calls.Check();
        }

        private void LateUpdate() => Clean.Apply();
    }

    /// <summary>Endpoints run from cues on the main thread; replies that come later are checked for errors.</summary>
    internal static class Calls
    {
        private static readonly List<BridgeRequest> Pending = new List<BridgeRequest>();

        internal static Router Router;

        internal static void Invoke(string path, Dictionary<string, string> args)
        {
            BridgeRequest request = (Router ?? throw new BridgeException("no router")).Invoke(path, args);
            if (request.Answered) Check(request);
            else Pending.Add(request);
        }

        internal static void Check()
        {
            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                if (!Pending[i].Answered && (System.DateTime.UtcNow - Pending[i].Arrived).TotalSeconds < 60) continue;
                Check(Pending[i]);
                Pending.RemoveAt(i);
            }
        }

        private static void Check(BridgeRequest request)
        {
            Reply reply = request.Result;
            if (reply == null) ShotRunner.Report($"{request.Path}: no reply in 60 s");
            else if (reply.Status != 200) ShotRunner.Report($"{request.Path}: {Fmt.Clip(reply.Body, 300)}");
        }
    }
}
