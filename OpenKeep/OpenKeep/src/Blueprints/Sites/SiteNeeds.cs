using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// What a construction site still needs, in item prefab names: the materials of every piece not built yet plus the
    /// ground's Stone while the ground is not shaped (nothing at all with "Build Without Materials"), and what of that
    /// its store does not hold yet. The missing list is kept per site until its ZDO changes (or a few seconds pass, for
    /// the world's "no build cost" keys), so hover text every frame costs nothing.
    /// </summary>
    public static class SiteNeeds
    {
        private const float CacheLife = 5f;

        /// <summary>Everything the site still needs, the store not counted.</summary>
        public static Dictionary<string, int> Remaining(SiteState s)
        {
            Dictionary<string, int> sum = new Dictionary<string, int>();
            Blueprint bp = s.Blueprint;
            if (bp == null || BlueprintSettings.FreeMaterials)
                return sum;
            bool[] built = s.Built;
            for (int i = 0; i < bp.Pieces.Count; i++)
            {
                if (i >= built.Length || !built[i])
                    SiteCosts.AddTo(sum, SiteCosts.Of(bp.Pieces[i].Prefab));
            }
            if (!s.GroundDone && s.GroundStone > 0)
                SiteCosts.Add(sum, SiteCosts.StoneItem, s.GroundStone);
            return sum;
        }

        /// <summary>What the site still needs beyond its store, worked out now.</summary>
        public static Dictionary<string, int> Missing(SiteState s)
        {
            Dictionary<string, int> missing = Remaining(s);
            foreach (KeyValuePair<string, int> stored in s.Store)
            {
                if (missing.ContainsKey(stored.Key))
                    SiteCosts.Add(missing, stored.Key, -stored.Value);
            }
            return missing;
        }

        /// <summary>What a loaded site still needs beyond its store, from this machine's cache when nothing changed.</summary>
        public static Dictionary<string, int> Missing(SiteMarker site)
        {
            SiteRun run = site.Run;
            uint revision = site.State.Zdo.DataRevision;
            bool free = BlueprintSettings.FreeMaterials;
            if (run.Missing != null && run.MissingRevision == revision && run.MissingFree == free && Time.time < run.MissingAt + CacheLife)
                return run.Missing;
            run.Missing = Missing(site.State);
            run.MissingRevision = revision;
            run.MissingFree = free;
            run.MissingAt = Time.time;
            return run.Missing;
        }
    }
}
