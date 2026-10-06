using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>ZDOMan.AddObjectsPerChunk</c> (private, four calls from <c>GetSaveClonePerChunk</c> on the main thread while the
    /// game stands still) clones the persistent objects of every changed chunk of one size. The prefix does the same
    /// work per chunk with <see cref="SaveChunkCopy"/>, the chunks side by side on worker threads when they hold many
    /// objects, and appends them in the game's order. Any throw falls back to the game's own copy, which starts afresh
    /// because nothing is appended until every chunk is done.
    /// </summary>
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.AddObjectsPerChunk))]
    public static class SaveObjectCopy
    {
        [HarmonyPrefix]
        public static bool Prefix(ZDOMan __instance, int size, byte chunkSize, int[] numZDOPerChunk,
            ref List<Tuple<ZoneSystem.ChunkIndex, List<ZDO>>> objectsPerChunk)
        {
            if (!SaveSettings.QuickWorldSave.Value)
                return true;
            try
            {
                var job = new SaveChunkCopy(__instance, size, chunkSize, numZDOPerChunk);
                var found = new Tuple<ZoneSystem.ChunkIndex, List<ZDO>>[numZDOPerChunk.Length];
                if (job.ObjectCount() >= SaveValueCopy.ParallelFrom)
                    Parallel.For(0, found.Length, i => found[i] = job.Copy(i));
                else
                    for (int i = 0; i < found.Length; i++)
                        found[i] = job.Copy(i);
                foreach (Tuple<ZoneSystem.ChunkIndex, List<ZDO>> chunk in found)
                    if (chunk != null)
                        objectsPerChunk.Add(chunk);
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"OpenKeep: quick world save fell back to the game's own object copy: {e.Message}");
                return true;
            }
        }
    }
}
