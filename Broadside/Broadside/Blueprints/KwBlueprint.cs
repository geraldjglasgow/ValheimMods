using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A saved ship: its name (the file's path in the ships folder, "karves/longboat") and its pieces, each placed in the dock's frame, the aft section of the dry dock
    /// it was saved on (Unity axes of that section: x right, y up, z forward, metres from its origin on the ground at its
    /// aft end's centre).
    /// </summary>
    public sealed class KwBlueprint
    {
        public KwBlueprint(string name) => Name = name;

        public string Name { get; }
        public List<KwBpPiece> Pieces { get; } = new List<KwBpPiece>();

        /// <summary>
        /// The ship on a dock, each piece relative to the dock's frame, in order along the dock (aft first), bow last: turned
        /// round when its stern is forward (<see cref="KwBpBow"/>).
        /// </summary>
        public static KwBlueprint Capture(string name, Transform frame, List<Piece> ship)
        {
            Quaternion back = Quaternion.Inverse(frame.rotation);
            KwBlueprint blueprint = new KwBlueprint(name);
            foreach (Piece piece in ship)
            {
                string? prefab = KwBpDock.NameOf(piece);
                if (prefab == null)
                    continue;
                Transform at = piece.transform;
                blueprint.Pieces.Add(new KwBpPiece(prefab, back * (at.position - frame.position), back * at.rotation, KwBpData.Capture(piece.m_nview.GetZDO())));
            }
            blueprint.Pieces.Sort((a, b) => a.Position.z.CompareTo(b.Position.z));
            KwBpBow.Fix(blueprint);
            return blueprint;
        }
    }

    /// <summary>One piece of a blueprint: its prefab, place and turn in the frame, and its saved ZDO values.</summary>
    public sealed class KwBpPiece
    {
        public KwBpPiece(string prefab, Vector3 position, Quaternion rotation, List<KwBpValue> data)
        {
            Prefab = prefab;
            Position = position;
            Rotation = rotation;
            Data = data;
        }

        public string Prefab { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public List<KwBpValue> Data { get; }
    }
}
