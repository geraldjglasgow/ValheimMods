using System.Collections.Generic;
using LocalEffects;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Ward's bubble on a warded character, on each machine that draws (<see cref="CasterFx"/>'s message, from
    /// <see cref="ReleaseDraws"/>): <c>ee_fx_cast_ward</c>, or while the bundle lacks it a local copy of the game shield's
    /// own bubble (<c>vfx_StaffShield</c>, never networked), sized to the body and following it until its owner says the
    /// ward ended (<see cref="Ended"/>, from <see cref="WardEffect"/>) or <see cref="Ward.Seconds"/> pass. One per
    /// character: a ward cast again replaces it. Nothing when the character is not loaded here.
    /// </summary>
    public static class WardBubble
    {
        public const string Ended = "ee_ward_end";
        private const string GameBubble = "vfx_StaffShield";
        private const float PlayerHeight = 1.8f;
        private const float Smallest = 0.6f;
        private const float Largest = 2.5f;
        private const int PruneAbove = 32;

        private static readonly Dictionary<ZDOID, GameObject> shown = new Dictionary<ZDOID, GameObject>();
        private static readonly List<ZDOID> gone = new List<ZDOID>();

        /// <summary>On the warded one's owner: the ward is over, to every player near.</summary>
        public static void SendEnd(Character warded) =>
            CasterFx.Send(Ended, warded.transform.position, warded.GetZDOID(), Spells.Ward);

        /// <summary>The bubble on (the ward's release) or off (its end) on this machine.</summary>
        public static void Show(Character warded, bool on)
        {
            if (warded == null)
                return;
            ZDOID id = warded.GetZDOID();
            Remove(id);
            if (!on)
                return;
            float size = Mathf.Clamp(warded.GetHeight() / PlayerHeight, Smallest, Largest);
            GameObject copy = LocalEffect.AttachScaled(Bubble(), warded.transform, warded.transform.position, endless: true, size);
            if (copy == null)
                return;
            Object.Destroy(copy, Ward.Seconds);
            Prune();
            shown[id] = copy;
            ReleaseDraws.Sound(SpellFx.CastWard, warded.transform.position);
        }

        private static GameObject Bubble()
        {
            GameObject own = CasterFx.Prepared(SpellFx.CastWard);
            return own != null || ZNetScene.instance == null ? own : ZNetScene.instance.GetPrefab(GameBubble);
        }

        private static void Remove(ZDOID id)
        {
            if (shown.TryGetValue(id, out GameObject old) && old != null)
                Object.Destroy(old);
            shown.Remove(id);
        }

        /// <summary>Forgets bubbles already gone (run out, or their character unloaded), once there are many.</summary>
        private static void Prune()
        {
            if (shown.Count < PruneAbove)
                return;
            gone.Clear();
            foreach (KeyValuePair<ZDOID, GameObject> pair in shown)
            {
                if (pair.Value == null)
                    gone.Add(pair.Key);
            }
            foreach (ZDOID id in gone)
                shown.Remove(id);
            gone.Clear();
        }
    }
}
