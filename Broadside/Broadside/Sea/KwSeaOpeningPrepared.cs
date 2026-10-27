using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A part's look made for the tools' ghost on a launched ship, kept for the redraw (the brief: no visible hitch on the
    /// editing player's game): the ghost shows the part exactly as the edit will draw it (<see cref="KwSeaOpeningLooks.Meshes"/>
    /// of the same new text), so when the owner's edit lands a moment later the redraw takes those meshes
    /// (<see cref="Take"/>) instead of building them again, and only the other players build it. The meshes are kept here,
    /// not by the ghost's objects: the ghost's current ones always, the others for <see cref="Kept"/> seconds after the ghost
    /// moved on (an edit window's OK hides the ghost before the owner answers), then let go. Only drawn, local to this player.
    /// </summary>
    public static class KwSeaOpeningPrepared
    {
        private const float Kept = 10f;
        private const int Most = 4;
        private static readonly Dictionary<string, (List<(Material Material, Mesh Mesh)> Meshes, float Used)> made = new Dictionary<string, (List<(Material, Mesh)>, float)>();

        /// <summary>A part's look for the ghost, made now or kept from before; its meshes are kept here.</summary>
        public static List<(Material Material, Mesh Mesh)> For(KwSeaShip ship, int index, KwBpPiece piece)
        {
            Sweep();
            string key = Key(ship, index, piece);
            if (!made.TryGetValue(key, out var kept))
                kept = (KwSeaOpeningLooks.Meshes(ship, piece, KwSeaOpeningLooks.Decks(ship)), Time.time);
            made[key] = (kept.Meshes, Time.time);
            return kept.Meshes;
        }

        /// <summary>A part's look kept for this piece, handed over to the caller (null when none was made for it).</summary>
        public static List<(Material Material, Mesh Mesh)>? Take(KwSeaShip ship, int index, KwBpPiece piece)
        {
            string key = Key(ship, index, piece);
            if (!made.TryGetValue(key, out var kept))
                return null;
            made.Remove(key);
            return kept.Meshes.TrueForAll(m => m.Mesh != null && m.Material != null) ? kept.Meshes : null;
        }

        /// <summary>The ghost still shows these: kept a while longer.</summary>
        public static void Touch(KwSeaShip ship, int index, KwBpPiece piece)
        {
            string key = Key(ship, index, piece);
            if (made.TryGetValue(key, out var kept))
                made[key] = (kept.Meshes, Time.time);
        }

        private static string Key(KwSeaShip ship, int index, KwBpPiece piece) =>
            ship.GetInstanceID() + "/" + index + "/" + KwBiomeTiers.Of(piece.Data).Id + "/" + KwShipParts.Saved(piece);

        /// <summary>Looks not used for a while (or past the most kept, the oldest) let go.</summary>
        public static void Sweep()
        {
            List<string> old = new List<string>();
            foreach (KeyValuePair<string, (List<(Material Material, Mesh Mesh)> Meshes, float Used)> entry in made)
            {
                if (Time.time - entry.Value.Used > Kept || made.Count - old.Count > Most && entry.Value.Used < Time.time - 0.75f)
                    old.Add(entry.Key);
            }
            foreach (string key in old)
            {
                made[key].Meshes.ForEach(m => Object.Destroy(m.Mesh));
                made.Remove(key);
            }
        }
    }
}
