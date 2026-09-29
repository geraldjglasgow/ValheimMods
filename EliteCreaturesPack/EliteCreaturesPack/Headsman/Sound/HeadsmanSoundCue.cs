using BundlePrefabs;
using UnityEngine;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// A sound cue as a networked effect, for sounds only the attacker's machine knows to make (a player's swing): the
    /// game spawns it from an effect list there, every peer gets it through ZNetScene and plays the cue where it stands
    /// (<see cref="HeadsmanSounds"/>), unless it arrives more than half a second late; its owner removes it after three
    /// seconds. Nothing of it is saved.
    /// </summary>
    public sealed class HeadsmanSoundCue : MonoBehaviour
    {
        private const string BornKey = "ecp_hs_cue";
        private const double Late = 0.5d;

        public string Cue = "";

        /// <summary>A networked, unsaved cue prefab on the prefab bench.</summary>
        public static GameObject Build(string name, string cue)
        {
            var prefab = new GameObject(name);
            prefab.transform.SetParent(PrefabBench.Root, false);
            ZNetView view = prefab.AddComponent<ZNetView>();
            (view.m_persistent, view.m_distant, view.m_type) = (false, false, ZDO.ObjectType.Default);
            TimedDestruction timeout = prefab.AddComponent<TimedDestruction>();
            (timeout.m_timeout, timeout.m_triggerOnAwake) = (3f, true);
            prefab.AddComponent<HeadsmanSoundCue>().Cue = cue;
            return prefab;
        }

        private void Awake()
        {
            ZDO? zdo = GetComponent<ZNetView>()?.GetZDO();
            if (zdo == null)
            {
                return;
            }
            if (HeadsmanTime.Read(zdo, BornKey) == 0d)
            {
                HeadsmanTime.Keep(zdo, BornKey, HeadsmanTime.Now);
            }
            if (HeadsmanTime.Now - HeadsmanTime.Read(zdo, BornKey) < Late)
            {
                HeadsmanSounds.Play(Cue, transform.position);
            }
        }
    }
}
