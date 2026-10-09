using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// One placed ballista's parts, found once by name in its frame (<see cref="BallistaFrame"/>): the two turning parts,
    /// each arm's vertebrae with their resting turns, the string's halves and tie points, the points the code reads, and
    /// the two missile models the piece carries (the one laid in the groove, the one in the holder's hand).
    /// </summary>
    public sealed class BallistaParts
    {
        public const string LaidMissile = "missile_laid", HeldMissile = "missile_held";

        public Transform Root = null!, Yaw = null!, Pitch = null!;
        public Transform[][] Arms = null!;
        public Quaternion[][] Rests = null!;
        public Transform?[] Tips = null!, Strings = null!;
        public Transform NockRest = null!, NockDrawn = null!, Seat = null!, Muzzle = null!;
        public Transform GripLeft = null!, GripRight = null!, StandAim = null!, StandLoad = null!;
        public GameObject? Laid, Held;

        /// <summary>The parts of a ballista, or null (logged once) when its model never loaded.</summary>
        public static BallistaParts? Of(Transform root)
        {
            Transform? yaw = GameMaterials.Find(root, BallistaFrame.Yaw), pitch = yaw != null ? GameMaterials.Find(yaw, BallistaFrame.Pitch) : null;
            if (yaw == null || pitch == null)
            {
                return null;
            }
            var parts = new BallistaParts { Root = root, Yaw = yaw, Pitch = pitch };
            parts.Points(pitch, yaw);
            parts.Limbs(pitch);
            parts.Laid = GameMaterials.Find(root, LaidMissile)?.gameObject;
            parts.Held = GameMaterials.Find(root, HeldMissile)?.gameObject;
            return parts;
        }

        private void Points(Transform pitch, Transform yaw)
        {
            NockRest = Point(pitch, BallistaFrame.NockRest);
            NockDrawn = Point(pitch, BallistaFrame.NockDrawn);
            Seat = Point(pitch, BallistaFrame.Seat);
            Muzzle = Point(pitch, BallistaFrame.Muzzle);
            GripLeft = Point(pitch, BallistaFrame.GripLeft);
            GripRight = Point(pitch, BallistaFrame.GripRight);
            StandAim = Point(yaw, BallistaFrame.StandAim);
            StandLoad = Point(yaw, BallistaFrame.StandLoad);
            Strings = new[] { GameMaterials.Find(pitch, BallistaFrame.StringLeft), GameMaterials.Find(pitch, BallistaFrame.StringRight) };
        }

        private void Limbs(Transform pitch)
        {
            string[] sides = { "l", "r" };
            Arms = new Transform[2][];
            Rests = new Quaternion[2][];
            Tips = new Transform?[2];
            for (int side = 0; side < 2; side++)
            {
                var chain = new List<Transform>();
                for (int i = 0; GameMaterials.Find(pitch, BallistaFrame.Segment(sides[side], i)) is Transform segment; i++)
                {
                    chain.Add(segment);
                }
                Arms[side] = chain.ToArray();
                Rests[side] = chain.ConvertAll(s => s.localRotation).ToArray();
                Tips[side] = GameMaterials.Find(pitch, BallistaFrame.Tip(sides[side]));
            }
        }

        /// <summary>A point by name, or a stand-in at the parent's origin (logged) so a model missing one still works.</summary>
        private static Transform Point(Transform parent, string name)
        {
            Transform? found = GameMaterials.Find(parent, name);
            if (found != null)
            {
                return found;
            }
            Log.Warn($"Bone ballista: the model has no point {name}; using its part's origin.");
            var stand = new GameObject(name).transform;
            stand.SetParent(parent, false);
            return stand;
        }
    }
}
