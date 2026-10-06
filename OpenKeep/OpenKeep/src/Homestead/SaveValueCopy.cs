using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>ZDOExtraData.PrepareSave</c> runs on the main thread while the game stands still and copies every stored value
    /// of every object in the world, though the save thread writes only the objects of the chunks that changed
    /// (<c>ZDOMan.m_saveData</c>, filled just before; <c>ZDO.Save</c> through <c>GetSaveData</c> is the copies' only
    /// reader). The prefix copies the values of those objects alone, with the game's own shallow <c>Clone</c>, the seven
    /// tables side by side on worker threads when there are many: nothing else writes them while the main thread waits
    /// here. Any throw falls back to the game's own copy.
    /// </summary>
    [HarmonyPatch(typeof(ZDOExtraData), nameof(ZDOExtraData.PrepareSave))]
    public static class SaveValueCopy
    {
        /// <summary>Objects to copy from which the work is split over worker threads; below it the thread start costs more than it saves.</summary>
        public const int ParallelFrom = 20000;

        [HarmonyPrefix]
        public static bool Prefix()
        {
            ZDOMan.SaveData save = ZDOMan.instance?.m_saveData;
            if (!SaveSettings.QuickWorldSave.Value || save == null)
                return true;
            try
            {
                ZDOExtraData.RegenerateConnectionHashData();
                CopyValues(SavedIds(save.m_objectsByChunk));
                ZDOExtraData.s_saveConnections = ZDOHelper.Clone(ZDOExtraData.s_connectionsHashData);
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"OpenKeep: quick world save fell back to the game's own copy: {e.Message}");
                return true;
            }
        }

        private static List<ZDOID> SavedIds(List<Tuple<ZoneSystem.ChunkIndex, List<ZDO>>> chunks)
        {
            var ids = new List<ZDOID>();
            foreach (Tuple<ZoneSystem.ChunkIndex, List<ZDO>> chunk in chunks)
                foreach (ZDO zdo in chunk.Item2)
                    ids.Add(zdo.m_uid);
            return ids;
        }

        private static void CopyValues(List<ZDOID> ids)
        {
            Action[] copies =
            {
                () => ZDOExtraData.s_saveFloats = Pick(ZDOExtraData.s_floats, ids),
                () => ZDOExtraData.s_saveVec3s = Pick(ZDOExtraData.s_vec3, ids),
                () => ZDOExtraData.s_saveQuats = Pick(ZDOExtraData.s_quats, ids),
                () => ZDOExtraData.s_saveInts = Pick(ZDOExtraData.s_ints, ids),
                () => ZDOExtraData.s_saveLongs = Pick(ZDOExtraData.s_longs, ids),
                () => ZDOExtraData.s_saveStrings = Pick(ZDOExtraData.s_strings, ids),
                () => ZDOExtraData.s_saveByteArrays = Pick(ZDOExtraData.s_byteArrays, ids),
            };
            if (ids.Count >= ParallelFrom)
                Parallel.Invoke(copies);
            else
                foreach (Action copy in copies)
                    copy();
        }

        private static Dictionary<ZDOID, BinarySearchDictionary<int, T>> Pick<T>(Dictionary<ZDOID, BinarySearchDictionary<int, T>> live, List<ZDOID> ids)
        {
            var copy = new Dictionary<ZDOID, BinarySearchDictionary<int, T>>(Math.Min(ids.Count, live.Count));
            foreach (ZDOID id in ids)
                if (live.TryGetValue(id, out BinarySearchDictionary<int, T> values))
                    copy[id] = (BinarySearchDictionary<int, T>)values.Clone();
            return copy;
        }
    }
}
