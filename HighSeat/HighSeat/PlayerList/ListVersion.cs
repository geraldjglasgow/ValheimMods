using System;
using System.Collections.Generic;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// A list's change count. The runtime the game ships counts every change to a List in its private field _version
    /// (Add, Clear, setting an item, Insert, Remove, Sort, Reverse: each adds one), so reading it tells in one step
    /// whether a list changed since it was last looked at, whoever changed it (the game, another mod), and an index
    /// built over the list knows when it must be built again. Read through a field reference made once per list type:
    /// no garbage per read. <see cref="Ready"/> is false when the runtime has no such field, and the patches that need it
    /// then leave the game's own code running.
    /// </summary>
    internal static class ListVersion<T>
    {
        private static readonly AccessTools.FieldRef<List<T>, int> Field = Make();

        public static bool Ready => Field != null;

        public static int Of(List<T> list) => Field(list);

        private static AccessTools.FieldRef<List<T>, int> Make()
        {
            try
            {
                return AccessTools.FieldRefAccess<List<T>, int>("_version");
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
