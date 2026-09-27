using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Kitchen trash to compost: dishes a kitchen's trash filter throws away (on a cooking station's owner, or the
    /// crafter's client at a cauldron or prep table) become one point each in the nearest loaded compost bin within 20 m
    /// of where they were made, sent to the bin's owner (<see cref="CompostBin.Send"/>). Needs Farming, composting and
    /// Kitchen Trash Compost on.
    /// </summary>
    public static class CompostTrash
    {
        private const float Range = 20f;

        public static void Add(Vector3 position, int dishes)
        {
            if (dishes <= 0 || !CompostBin.Working || !CompostSettings.KitchenTrash.Value)
                return;
            HookGuard.Run("Compost kitchen trash", () => Nearest(position)?.Send(dishes));
        }

        /// <summary>The dishes below <paramref name="minStars"/> in a craft's rolls, as <see cref="CraftStars.ReportTrashed"/> counts them.</summary>
        public static void AddTrashed(Vector3 position, int[] rolled, int minStars)
        {
            int dishes = 0;
            for (int stars = 0; stars < minStars && stars < rolled.Length; stars++)
                dishes += rolled[stars];
            Add(position, dishes);
        }

        private static CompostBin Nearest(Vector3 position)
        {
            CompostBin nearest = null;
            float best = Range;
            foreach (CompostBin bin in CompostBin.All)
            {
                if (bin == null || !bin.IsValid)
                    continue;
                float distance = Vector3.Distance(bin.transform.position, position);
                if (distance <= best)
                {
                    best = distance;
                    nearest = bin;
                }
            }
            return nearest;
        }
    }
}
