using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// Watches the rule file the way ConfigReload watches the .cfg: a slow poll of the file's write time and size on
    /// the main thread, no file watcher. When the file changes it re-reads it and lets the server lock re-mirror and
    /// re-push. A reload never takes the game down - a bad file is reported and the last good rules stay in force.
    /// </summary>
    public sealed class RuleReload : MonoBehaviour
    {
        private const float IntervalSeconds = 5f;
        private float _timer;

        public static void Attach()
        {
            GameObject holder = new GameObject("ecr_rule_reload");
            DontDestroyOnLoad(holder);
            holder.AddComponent<RuleReload>();
        }

        private void Update() => Guard.Run("RuleReload.Update", Poll);

        private void Poll()
        {
            _timer += Time.deltaTime;
            if (_timer < IntervalSeconds)
            {
                return;
            }
            _timer = 0f;
            if (RuleFile.ChangedOnDisk() && RuleFile.Load())
            {
                ServerLock.OnLocalReloaded();
            }
        }
    }
}
