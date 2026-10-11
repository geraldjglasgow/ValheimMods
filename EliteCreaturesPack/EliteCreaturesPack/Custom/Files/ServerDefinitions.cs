using System.Collections.Generic;
using Charter;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Build;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>
    /// The files the server built this world's custom creatures from, sent to every player who joins: a standing Charter
    /// article (it travels whether or not the server binds the configuration, since prefabs must match the server's
    /// world, not a player's taste), path and content alternating like YamlConfig's own. The server assigns it when it
    /// builds; a player reads it after the first push of each connection (Charter's <c>Pushed</c>, raised once all of that
    /// push's articles are in, whether this one changed or not) and builds from it.
    /// </summary>
    internal static class ServerDefinitions
    {
        public const string Name = "ecp.creatures.built";

        private static Article<List<string>>? article;

        public static void Register(Charter.Charter charter)
        {
            article = new Article<List<string>>(charter, Name, new List<string>(), standing: true);
            charter.Pushed += first =>
            {
                if (first)
                {
                    SafeCall.Run("custom creatures from the server", Arrived);
                }
            };
        }

        /// <summary>The server: the files this world's creatures were built from (empty when none were).</summary>
        public static void Publish(IReadOnlyDictionary<string, string> files)
        {
            List<string> flat = new List<string>(files.Count * 2);
            foreach (KeyValuePair<string, string> file in files)
            {
                flat.Add(file.Key);
                flat.Add(file.Value);
            }
            article?.Assign(flat);
        }

        private static void Arrived()
        {
            if (article == null || BuildTiming.IsServerSide)
            {
                return;
            }
            Dictionary<string, string> files = new Dictionary<string, string>();
            List<string> flat = article.Value ?? new List<string>();
            for (int i = 0; i + 1 < flat.Count; i += 2)
            {
                files[flat[i]] = flat[i + 1];
            }
            BuildTiming.ServerFilesArrived(files);
        }
    }
}
