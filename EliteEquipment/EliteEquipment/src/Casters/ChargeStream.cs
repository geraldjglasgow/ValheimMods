using System.Collections.Generic;
using LocalEffects;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// White energy from the caster's other hand into the wand while a charge builds (user, 2026-10-11, with a reference
    /// image: "have some white energy emanating from the other hand toward the wand while it's charging. When fully charged
    /// stop the other hand from emanating white energy"). Drawn for every player near on this machine from the state the
    /// caster shares (<see cref="ChargeState"/>): the workshop's <c>ee_fx_charge_stream</c>, authored along +Z over one
    /// metre, set at the left hand (with a spellbook open on it, at the book's pages: <see cref="StreamStart"/>) each frame,
    /// turned to the wand's tip and stretched to reach it; gone at full charge,
    /// when the charge is let go, or when the hand or wand is not there. Local copies only.
    /// </summary>
    public static class ChargeStream
    {
        private const float Shortest = 0.01f;

        private sealed class Drawn
        {
            public Transform Palm;
            public Transform Tip;
            public Transform Book;
            public Vector3 BookTop;
            public GameObject Stream;
        }

        private static readonly Dictionary<Player, Drawn> drawn = new Dictionary<Player, Drawn>();
        private static readonly List<Player> gone = new List<Player>();

        public static void Tick()
        {
            foreach (Player player in Player.GetAllPlayers())
                Draw(player);
            if (drawn.Count > 0)
                Sweep();
        }

        private static void Draw(Player player)
        {
            bool on = ChargeState.Holding(player, out float charge) && charge < 1f;
            Transform tip = on ? TipSpawn.Tip(player) : null;
            Transform palm = on && player.m_visEquipment != null ? player.m_visEquipment.m_leftHand : null;
            drawn.TryGetValue(player, out Drawn now);
            if (now != null && (palm == null || tip == null || now.Palm != palm || now.Tip != tip))
            {
                Remove(now);
                drawn.Remove(player);
                now = null;
            }
            if (palm == null || tip == null)
                return;
            if (now == null)
                drawn[player] = now = Make(player, palm, tip);
            Aim(now);
        }

        private static Drawn Make(Player player, Transform palm, Transform tip)
        {
            GameObject stream = LocalEffect.Attach(BoltTint.Stream(player), palm, palm.position, endless: true);
            if (stream != null)
                ChargeOrb.ScaleWithParent(stream);
            Transform book = StreamStart.Book(player, out Vector3 top);
            return new Drawn { Palm = palm, Tip = tip, Book = book, BookTop = top, Stream = stream };
        }

        /// <summary>From the palm (or the open book on it, <see cref="StreamStart"/>) to the tip: +Z turned to the tip, stretched to reach it.</summary>
        private static void Aim(Drawn now)
        {
            if (now.Stream == null)
                return;
            Vector3 palm = StreamStart.From(now.Palm, now.Book, now.BookTop);
            Vector3 reach = now.Tip.position - palm;
            float length = reach.magnitude;
            if (length < Shortest)
                return;
            Transform stream = now.Stream.transform;
            stream.position = palm;
            stream.rotation = Quaternion.LookRotation(reach / length);
            Vector3 parent = now.Palm.lossyScale;
            stream.localScale = new Vector3(1f / Mathf.Max(parent.x, Shortest), 1f / Mathf.Max(parent.y, Shortest),
                length / Mathf.Max(parent.z, Shortest));
        }

        private static void Remove(Drawn now)
        {
            if (now.Stream != null)
                Object.Destroy(now.Stream);
        }

        /// <summary>Forgets players no longer here; their copies went with their hands.</summary>
        private static void Sweep()
        {
            gone.Clear();
            foreach (KeyValuePair<Player, Drawn> entry in drawn)
            {
                if (entry.Key == null || entry.Value.Palm == null)
                    gone.Add(entry.Key);
            }
            foreach (Player player in gone)
            {
                Remove(drawn[player]);
                drawn.Remove(player);
            }
        }
    }
}
