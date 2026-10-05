using System;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Which room every piece of a storey belongs to: the room (or the room's walls) under its footprint centre (floors,
    /// props, walls, upper storeys, the roof itself), else the room whose cells come within <see cref="Reach"/> m of its
    /// centre (a post or sill beam just outside a wall), else the room of a piece it touches (steps up to the door, a
    /// ridge dragon, a portal against a thick wall; one step, so a road or a fence joined to a house does not follow).
    /// Pieces whose top lies more than <see cref="Below"/> m under the storey's floor belong to what stands below it
    /// and never to its rooms.
    /// </summary>
    internal static class SmartAssign
    {
        private const float Reach = 0.75f;
        private const float Below = 1.5f;
        private const float Touch = 0.15f;
        private const float TouchSearch = 4f;

        /// <summary>Per piece: its room id, 0 for none.</summary>
        public static int[] Owners(SmartModel model, SmartRooms rooms, float floor)
        {
            int[] owners = new int[model.Boxes.Length];
            SmartReach reach = new SmartReach(model.Grid, rooms.Labels);
            for (int i = 0; i < owners.Length; i++)
                owners[i] = Owner(model, rooms, reach, model.Boxes[i], floor);
            AddTouching(model, reach, owners, floor);
            return owners;
        }

        private static int Owner(SmartModel model, SmartRooms rooms, SmartReach reach, SmartBox box, float floor)
        {
            if (box.Top < floor - Below)
                return 0;
            int cell = model.Grid.Index(box.X, box.Z);
            int room = cell >= 0 ? rooms.Labels[cell] : 0;
            return room > 0 ? room : reach.Nearest(box.X, box.Z, Reach);
        }

        /// <summary>Gives each piece outside every room the room of an owned piece it touches (from the owners before this step).</summary>
        private static void AddTouching(SmartModel model, SmartReach reach, int[] owners, float floor)
        {
            int[] joined = (int[])owners.Clone();
            for (int i = 0; i < owners.Length; i++)
            {
                SmartBox box = model.Boxes[i];
                if (owners[i] == 0 && box.Top >= floor - Below && reach.AnyNear(box, TouchSearch))
                    joined[i] = Touched(model, owners, box);
            }
            Array.Copy(joined, owners, owners.Length);
        }

        /// <summary>The room of the first owned piece the box touches, or 0.</summary>
        private static int Touched(SmartModel model, int[] owners, SmartBox box)
        {
            foreach (int j in model.Hash.Near(box.X, box.Z, box.Reach + Touch))
            {
                if (owners[j] > 0 && box.Touches(model.Boxes[j], Touch))
                    return owners[j];
            }
            return 0;
        }
    }
}
