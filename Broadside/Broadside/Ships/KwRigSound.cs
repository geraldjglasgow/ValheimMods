using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The game's own sounds played where a rig piece is hurt, on this screen only (<see cref="KwMastFall"/>): each a copy of
    /// its sound prefab made with its network view switched off, the game's build-ghost way, so no ZDO is made. A sound the
    /// network scene does not list is found in a game piece's door effects (the heavy gate's creak). Its volume and pitch
    /// may be scaled, the game's own spread of both kept round them.
    /// </summary>
    public static class KwRigSound
    {
        public const string Snap = "sfx_fishingrod_linebreak", Crack = "sfx_wood_break", Land = "sfx_ship_destroyed",
            Creak = "sfx_darkwood_door_open", Splinter = "sfx_branch_break";

        private static readonly (string Sound, string Piece)[] InDoors = { (Creak, "darkwood_gate") };
        private static readonly Dictionary<string, GameObject> found = new Dictionary<string, GameObject>();

        public static void Play(string name, Vector3 at) => Play(name, at, 1f, 1f);

        /// <summary>A sound played there, its volume and pitch scaled; nothing when the game has no such sound.</summary>
        public static void Play(string name, Vector3 at, float volume, float pitch)
        {
            GameObject? prefab = Find(name);
            if (prefab == null)
                return;
            // made under a switched-off holder, so it wakes, and plays, only once scaled
            GameObject hold = new GameObject("KwRigSoundHold");
            hold.SetActive(false);
            bool was = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            try
            {
                GameObject made = Object.Instantiate(prefab, at, Quaternion.identity, hold.transform);
                Scale(made.GetComponent<ZSFX>(), volume, pitch);
                made.transform.SetParent(null, true);
            }
            finally
            {
                ZNetView.m_forceDisableInit = was;
                Object.Destroy(hold);
            }
        }

        private static void Scale(ZSFX? sound, float volume, float pitch)
        {
            if (sound == null)
                return;
            (sound.m_minVol, sound.m_maxVol) = (sound.m_minVol * volume, sound.m_maxVol * volume);
            (sound.m_minPitch, sound.m_maxPitch) = (sound.m_minPitch * pitch, sound.m_maxPitch * pitch);
        }

        private static GameObject? Find(string name)
        {
            if (found.TryGetValue(name, out GameObject known) && known != null)
                return known;
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            foreach ((string sound, string piece) in InDoors)
            {
                if (prefab == null && sound == name)
                    prefab = InDoor(piece, name);
            }
            if (prefab != null)
                found[name] = prefab;
            return prefab;
        }

        // A sound in a game piece's door's opening effects, by name.
        private static GameObject? InDoor(string piece, string name)
        {
            GameObject? gate = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(piece) : null;
            Door? door = gate != null ? gate.GetComponent<Door>() : null;
            if (door == null || door.m_openEffects == null)
                return null;
            foreach (EffectList.EffectData effect in door.m_openEffects.m_effectPrefabs)
            {
                if (effect.m_prefab != null && effect.m_prefab.name == name)
                    return effect.m_prefab;
            }
            return null;
        }
    }
}
