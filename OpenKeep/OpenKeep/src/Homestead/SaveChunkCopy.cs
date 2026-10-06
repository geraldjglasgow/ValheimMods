using System;
using System.Collections.Generic;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// One call of the game's <c>ZDOMan.AddObjectsPerChunk</c> as independent work per chunk: chunk <c>i</c> of a
    /// <c>size</c> x <c>size</c> grid, skipped when it holds nothing or has not changed since the last save, otherwise
    /// the clones of its persistent objects that are not portals, sector by sector in the game's order. It only reads
    /// the game's tables (captured here on the main thread), so chunks may be copied on several threads at once while
    /// the main thread waits.
    /// </summary>
    internal sealed class SaveChunkCopy
    {
        private readonly int size;
        private readonly byte chunkSize;
        private readonly int[] counts;
        private readonly HashSet<ZoneSystem.ChunkIndex> dirty;
        private readonly List<ZDO>[] sectors;
        private readonly List<int> portals;

        public SaveChunkCopy(ZDOMan man, int size, byte chunkSize, int[] counts)
        {
            this.size = size;
            this.chunkSize = chunkSize;
            this.counts = counts;
            dirty = man.m_dirtyChunks[0];
            sectors = man.m_objectsBySector;
            portals = Game.instance.PortalPrefabHash;
        }

        /// <summary>Objects (persistent or not) in the changed chunks: how much work there is.</summary>
        public int ObjectCount()
        {
            int total = 0;
            for (int i = 0; i < counts.Length; i++)
                if (counts[i] > 0 && dirty.Contains(IndexOf(i)))
                    total += counts[i];
            return total;
        }

        public Tuple<ZoneSystem.ChunkIndex, List<ZDO>> Copy(int i)
        {
            if (counts[i] <= 0)
                return null;
            ZoneSystem.ChunkIndex index = IndexOf(i);
            if (!dirty.Contains(index))
                return null;
            var objects = new List<ZDO>();
            (int x, int y) zone = ZoneSystem.GetZoneFromChunk(index);
            int span = 8 * (64 / size);
            for (int y = zone.y; y < zone.y + span; y++)
                for (int x = zone.x; x < zone.x + span; x++)
                    AddSector(objects, ZoneSystem.SectorToIndex(x, y).Sector);
            return new Tuple<ZoneSystem.ChunkIndex, List<ZDO>>(index, objects);
        }

        private ZoneSystem.ChunkIndex IndexOf(int i)
        {
            int step = 64 / size;
            return ZoneSystem.ChunkIndexFromXY((uint)(i % size * step), (uint)(i / size * step), chunkSize);
        }

        private void AddSector(List<ZDO> objects, uint sector)
        {
            List<ZDO> zdos = sectors[sector];
            if (zdos == null)
                return;
            foreach (ZDO zdo in zdos)
                if (zdo.Persistent && !portals.Contains(zdo.GetPrefab()))
                    objects.Add(zdo.Clone());
        }
    }
}
