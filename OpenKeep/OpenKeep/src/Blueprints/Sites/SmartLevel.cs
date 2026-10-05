using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// One storey of a blueprint, seen from its floor height <see cref="Base"/>: the plan of what stands in the storey
    /// (walls, doors, gates, windows, fences, furniture: any piece but a roof filling at least <see cref="MinFill"/> m of
    /// the height band from <see cref="BandLow"/> to <see cref="BandHigh"/> above the floor) and of what is roofed (roof pieces from
    /// <see cref="RoofAbove"/> m up, shrunk by <see cref="Eaves"/> m so a roof's drip edge does not reach the
    /// neighbours), its rooms (<see cref="SmartRooms"/>) and which room every piece belongs to (<see cref="SmartAssign"/>).
    /// Worked out once per blueprint and floor height.
    /// </summary>
    internal sealed class SmartLevel
    {
        private const float BandLow = 0.3f;
        private const float BandHigh = 2.3f;
        private const float MinFill = 0.5f;
        private const float RoofAbove = 1.5f;
        private const float RoofReach = 30f;
        private const float Eaves = 0.75f;
        private const byte WallFlag = 1;
        private const byte RoofFlag = 2;

        public readonly float Base;

        /// <summary>Per piece: the room (building) it belongs to on this storey, 0 for none.</summary>
        public readonly int[] Owners;

        private readonly List<int>[] members;

        public SmartLevel(SmartModel model, float floor)
        {
            Base = floor;
            SmartRooms rooms = new SmartRooms(model.Grid, Kinds(model));
            Owners = SmartAssign.Owners(model, rooms, floor);
            members = new List<int>[rooms.Count + 1];
            for (int i = 0; i < Owners.Length; i++)
            {
                if (Owners[i] > 0)
                    (members[Owners[i]] ?? (members[Owners[i]] = new List<int>())).Add(i);
            }
        }

        /// <summary>The pieces of the building the piece belongs to (blueprint order), or null when it belongs to none.</summary>
        public List<int> Building(int index) => Owners[index] > 0 ? new List<int>(members[Owners[index]]) : null;

        /// <summary>Every cell's kind for <see cref="SmartRooms"/>: roofed before wall before open.</summary>
        private byte[] Kinds(SmartModel model)
        {
            SmartGrid grid = model.Grid;
            byte[] flags = new byte[grid.Count];
            foreach (SmartBox box in model.Boxes)
            {
                if (!box.Roof && box.Overlap(Base + BandLow, Base + BandHigh) >= MinFill)
                    grid.Mark(flags, box, SmartGrid.Cell * 0.5f, WallFlag);
                if (box.Roof && box.Bottom >= Base + RoofAbove && box.Bottom <= Base + RoofReach)
                    grid.Mark(flags, box, 0f, RoofFlag);
            }
            grid.Erode(flags, RoofFlag, Mathf.RoundToInt(Eaves / SmartGrid.Cell));
            for (int i = 0; i < flags.Length; i++)
            {
                byte f = flags[i];
                flags[i] = (f & RoofFlag) != 0 ? SmartRooms.Roofed : (f & WallFlag) != 0 ? SmartRooms.Wall : SmartRooms.Open;
            }
            return flags;
        }
    }
}
