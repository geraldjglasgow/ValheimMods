using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Build On Wood. Player.UpdatePlacementGhost refuses a piece whose Piece.m_notOnWood is set when the ray hits a
    /// piece whose WearNTear material is Wood or HardWood; that field is the only thing keeping the campfire off a
    /// wooden floor. The field is cleared on every listed prefab and on the local player's placement ghost (a copy
    /// made from the prefab when the piece is selected, and the object the check reads); a prefab that leaves the
    /// list gets back the value it had before OpenKeep first changed it. Every other placement rule is untouched.
    /// Runs on every machine at scene load and on every change of the setting (including the server's value arriving
    /// at login); only the building player's client ever reads the field.
    /// </summary>
    public static class FirePlacement
    {
        // Prefab name to m_notOnWood as it was the first time this module changed the prefab.
        private static readonly Dictionary<string, bool> original = new Dictionary<string, bool>(StringComparer.Ordinal);

        public static void ApplyAll()
        {
            if (ZNetScene.instance == null)
                return;
            Dictionary<string, Piece> listed = FirePrefabs.Listed();
            foreach (KeyValuePair<string, Piece> entry in listed)
                Allow(entry.Key, entry.Value);
            foreach (KeyValuePair<string, bool> entry in original)
            {
                if (!listed.ContainsKey(entry.Key))
                    Restore(entry.Key, entry.Value);
            }
            RefreshGhost();
        }

        private static void Allow(string name, Piece piece)
        {
            if (!original.ContainsKey(name))
            {
                original[name] = piece.m_notOnWood;
                Plugin.Log.LogInfo($"OpenKeep: {name} may be built on wood (the game's m_notOnWood: {piece.m_notOnWood}).");
            }
            piece.m_notOnWood = false;
        }

        private static void Restore(string name, bool notOnWood)
        {
            Piece piece = FirePrefabs.Exact(name);
            if (piece != null)
                piece.m_notOnWood = notOnWood;
        }

        /// <summary>The ghost of the piece in hand copies the field from its prefab, so a change applies without reselecting.</summary>
        private static void RefreshGhost()
        {
            Player player = Player.m_localPlayer;
            GameObject ghost = player != null ? player.m_placementGhost : null;
            Piece piece = ghost != null ? ghost.GetComponent<Piece>() : null;
            Piece prefab = piece != null ? FirePrefabs.Exact(ghost.name) : null;
            if (prefab != null)
                piece.m_notOnWood = prefab.m_notOnWood;
        }
    }
}
