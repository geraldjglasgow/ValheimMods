using System.Globalization;
using DevBridge.Server;

namespace DevBridge.Load
{
    internal enum LoadAction
    {
        Status,
        Add,
        Remove,
        Measure,
        Verify,
    }

    /// <summary>
    /// The arguments of one /load call, read and checked before anything changes, so a call with a bad argument changes
    /// nothing: one action (players=, remove=all, measure=, verify=1 or status=1; none is status), and for players= the
    /// mode, the crowd's centre, the radius, the speed, and whether the new fakes read HighSeat's deltas (deltas=1) and
    /// keep a copy of the world to verify (verify=1; alone, verify=1 is the report). Bounds: up to
    /// <see cref="FakeCrowd.Max"/> fakes, radius 0 to 200 m (default 20), speed 0 to 20 m/s (default 4, a player's jog),
    /// measure 1 to 120 s (default 10).
    /// </summary>
    internal sealed class LoadArgs
    {
        internal const string Spread = "spread";
        internal const string Crowd = "crowd";
        private const int MaxSeconds = 120;
        private const float MaxRadius = 200f;
        private const float MaxSpeed = 20f;
        private const float WorldEdge = 10500f;
        private static readonly string[] AddOptions = { "mode", "x", "z", "radius", "speed", "deltas" };

        internal LoadAction Action { get; private set; }

        internal int Players { get; private set; }

        internal string Mode { get; private set; } = Spread;

        internal bool HasCentre { get; private set; }

        internal float X { get; private set; }

        internal float Z { get; private set; }

        internal float Radius { get; private set; } = 20f;

        internal float Speed { get; private set; } = 4f;

        internal int Seconds { get; private set; } = 10;

        /// <summary>The fakes added say hello in HighSeat's delta format (deltas=1).</summary>
        internal bool Deltas { get; private set; }

        /// <summary>The fakes added keep a copy of the world from what they are sent (verify=1 with players=).</summary>
        internal bool Verify { get; private set; }

        internal static LoadArgs Read(BridgeRequest request)
        {
            var args = new LoadArgs { Action = PickAction(request) };
            if (args.Action == LoadAction.Add) args.ReadAdd(request);
            else RefuseAddOptions(request);
            if (args.Action == LoadAction.Remove && request.Get("remove") != "all")
                throw new BridgeException("remove= takes all: remove=all takes every fake player off");
            if (args.Action == LoadAction.Measure) args.Seconds = (int)Number(request, "measure", 10f, 1f, MaxSeconds, "whole seconds", true);
            return args;
        }

        private static LoadAction PickAction(BridgeRequest request)
        {
            int asked = 0;
            LoadAction action = LoadAction.Status;
            if (request.Has("players")) (asked, action) = (asked + 1, LoadAction.Add);
            if (request.Has("remove")) (asked, action) = (asked + 1, LoadAction.Remove);
            if (request.Has("measure")) (asked, action) = (asked + 1, LoadAction.Measure);
            if (request.Has("status")) (asked, action) = (asked + 1, LoadAction.Status);
            if (request.Has("verify") && !request.Has("players")) (asked, action) = (asked + 1, LoadAction.Verify);
            if (asked > 1) throw new BridgeException("one of players=, remove=all, measure=, verify=1 or status=1 per call");
            return action;
        }

        // A switch given as 1 or 0 (true or false); off when not given.
        private static bool Flag(BridgeRequest request, string name)
        {
            string text = request.Get(name);
            if (text == null || text == "0" || text == "false") return false;
            if (text == "1" || text == "true") return true;
            throw new BridgeException($"{name}= takes 1 or 0, not '{text}'");
        }

        private void ReadAdd(BridgeRequest request)
        {
            if (request.Get("players") == null) throw new BridgeException($"players= takes how many fake players in all, 1 to {FakeCrowd.Max}");
            Players = (int)Number(request, "players", 0f, 1f, FakeCrowd.Max, "fake players in all", true);
            Mode = (request.Get("mode") ?? Spread).ToLowerInvariant();
            if (Mode != Spread && Mode != Crowd) throw new BridgeException("mode= takes spread (each on its own spot) or crowd (round one spot)");
            ReadCentre(request);
            Radius = Number(request, "radius", Radius, 0f, MaxRadius, "metres", false);
            Speed = Number(request, "speed", Speed, 0f, MaxSpeed, "metres a second", false);
            Deltas = Flag(request, "deltas");
            Verify = Flag(request, "verify");
        }

        private void ReadCentre(BridgeRequest request)
        {
            bool x = request.Get("x") != null, z = request.Get("z") != null;
            if (x != z) throw new BridgeException("x= and z= go together: the crowd's centre in world metres");
            if (!x) return;
            if (Mode != Crowd) throw new BridgeException("x= and z= place a crowd (mode=crowd); spread picks its own spots of land");
            HasCentre = true;
            X = Number(request, "x", 0f, -WorldEdge, WorldEdge, "world metres", false);
            Z = Number(request, "z", 0f, -WorldEdge, WorldEdge, "world metres", false);
        }

        private static void RefuseAddOptions(BridgeRequest request)
        {
            foreach (string option in AddOptions)
            {
                if (request.Has(option))
                    throw new BridgeException($"{option}= goes with players=: mode=, x=, z=, radius=, speed=, deltas= and verify= describe the fakes added");
            }
        }

        // A number from the request within bounds (whole when asked), the fallback when it is not given.
        private static float Number(BridgeRequest request, string name, float fallback, float min, float max, string unit, bool whole)
        {
            string text = request.Get(name);
            if (text == null) return fallback;
            bool parsed = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
            if (!parsed || float.IsNaN(value) || value < min || value > max || whole && value != (int)value)
                throw new BridgeException($"{name}= takes {unit} from {min.ToString(CultureInfo.InvariantCulture)} to " +
                                          $"{max.ToString(CultureInfo.InvariantCulture)}, not '{text}'");
            return value;
        }
    }
}
