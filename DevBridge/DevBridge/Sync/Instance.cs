using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevBridge.Sync
{
    /// <summary>One DevBridge found on this machine, as its /status describes it.</summary>
    internal sealed class Instance
    {
        internal int Port;
        internal bool Here;
        internal string Problem;

        /// <summary>Something else listens on the port: it is no peer and is left out of every comparison.</summary>
        internal bool Foreign;
        internal string State;
        internal string Role;
        internal string World;
        internal string Player;
        internal string Bridge;
        internal readonly SortedDictionary<string, string> Plugins = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal static Instance From(PeerAnswer answer, int ownPort)
        {
            var instance = new Instance { Port = answer.Port, Here = answer.Port == ownPort };
            JObject status = answer.Ok ? Parse(answer.Body) : null;
            if (status?["bridge"] != null) instance.Read(status);
            else if (answer.Ok || answer.Code == 404) instance.Foreign = true;
            else instance.Problem = answer.Problem;
            if (instance.Foreign) instance.Problem = "not a DevBridge";
            return instance;
        }

        private static JObject Parse(string body)
        {
            try { return JObject.Parse(body); }
            catch (JsonException) { return null; }
        }

        private void Read(JObject status)
        {
            var network = status["network"] as JObject;
            State = (string)status["state"];
            Bridge = (string)status["bridge"];
            Role = RoleOf(network);
            World = (string)network?["world"];
            Player = (string)(status["player"] as JObject)?["name"];
            foreach (string plugin in (status["plugins"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>())
            {
                int space = plugin.LastIndexOf(' ');
                if (space > 0) Plugins[plugin.Substring(0, space)] = plugin.Substring(space + 1);
            }
        }

        /// <summary>A server with nobody connected cannot be told from single player by /status, so it reads as one.</summary>
        internal static string RoleOf(JObject network)
        {
            if (network == null) return "no world";
            if ((string)network["role"] is string role) return role;
            if ((bool?)network["dedicated"] == true) return "dedicated server";
            if ((bool?)network["server"] != true) return "client";
            return (int?)network["peers"] > 0 ? "host" : "single player";
        }

        /// <summary>Who it is, for the peers of a comparison.</summary>
        internal Dictionary<string, object> Brief()
        {
            var brief = new Dictionary<string, object> { ["port"] = Port };
            if (Problem != null) return brief;
            brief["role"] = Role;
            brief["world"] = World;
            brief["player"] = Player;
            return brief;
        }

        /// <summary>Everything /sync?peers=1 lists.</summary>
        internal Dictionary<string, object> Full()
        {
            Dictionary<string, object> full = Brief();
            if (Here) full["this"] = true;
            if (Problem != null)
            {
                full["problem"] = Problem;
                return full;
            }
            full["state"] = State;
            full["bridge"] = Bridge;
            full["plugins"] = Plugins.Select(pair => pair.Key + " " + pair.Value).ToList();
            return full;
        }
    }
}
