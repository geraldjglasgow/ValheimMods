using System;
using PatchGuard;
using UnityEngine;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// Hot reload of the YAML families, the way ConfigReload watches the .cfg: a five-second poll on the main thread, no
    /// file watcher (it works the same on every dedicated server). A poll costs a few stats: the config folder's write
    /// time and each known file's size and write time; the folder is listed again only when its own time moved
    /// (<see cref="RuleFiles.Changed"/>). A reload never throws out of here; a bad file is reported and the previous
    /// rules stay.
    /// </summary>
    internal sealed class RuleReload : MonoBehaviour
    {
        private const float IntervalSeconds = 5f;
        private static Action? _poll;
        private float _timer;

        public static void Attach(Action poll)
        {
            _poll = poll;
            GameObject holder = new GameObject("ecf_rule_reload");
            DontDestroyOnLoad(holder);
            holder.AddComponent<RuleReload>();
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < IntervalSeconds || _poll == null)
            {
                return;
            }
            _timer = 0f;
            Guard.Run("RuleReload.Update", _poll);
        }
    }
}
