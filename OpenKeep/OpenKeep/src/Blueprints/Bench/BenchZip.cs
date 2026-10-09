using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// A blueprint file packed for the network (gzip; JSON shrinks to a fraction) and cut into parts of
    /// <see cref="BenchLimits.PartBytes"/>. Unpacking stops at a size limit, so a small packed file can never grow into a
    /// huge one on the server.
    /// </summary>
    public static class BenchZip
    {
        public static byte[] Pack(byte[] data)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(output, CompressionMode.Compress))
                    zip.Write(data, 0, data.Length);
                return output.ToArray();
            }
        }

        /// <summary>The unpacked bytes, or null when they are not packed data or come to more than <paramref name="limit"/> bytes.</summary>
        public static byte[] Unpack(byte[] packed, int limit)
        {
            try
            {
                using (GZipStream zip = new GZipStream(new MemoryStream(packed), CompressionMode.Decompress))
                using (MemoryStream output = new MemoryStream())
                {
                    byte[] buffer = new byte[16384];
                    int read;
                    while ((read = zip.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, read);
                        if (output.Length > limit)
                            return null;
                    }
                    return output.ToArray();
                }
            }
            catch (Exception e) when (e is InvalidDataException || e is IOException)
            {
                return null;
            }
        }

        public static List<byte[]> Split(byte[] packed)
        {
            List<byte[]> parts = new List<byte[]>();
            for (int at = 0; at < packed.Length || parts.Count == 0; at += BenchLimits.PartBytes)
            {
                byte[] part = new byte[Math.Min(BenchLimits.PartBytes, packed.Length - at)];
                Array.Copy(packed, at, part, 0, part.Length);
                parts.Add(part);
            }
            return parts;
        }
    }

    /// <summary>
    /// Parts arriving, put back together per file (the key names the sender and the file). Routed RPCs from one machine
    /// keep their order, so a part out of turn means a lost transfer: the file is dropped. Unfinished files are dropped
    /// after <see cref="BenchLimits.Timeout"/> seconds.
    /// </summary>
    public sealed class BenchInbox
    {
        private sealed class Partial
        {
            public int Parts;
            public int Next;
            public float Started;
            public readonly MemoryStream Data = new MemoryStream();
        }

        private readonly Dictionary<string, Partial> open = new Dictionary<string, Partial>();

        /// <summary>Adds a part; the whole packed file once its last part arrived, else null.</summary>
        public byte[] Add(string key, int part, int parts, byte[] bytes)
        {
            if (parts < 1 || parts > BenchLimits.MaxParts || part < 0 || part >= parts || bytes == null)
                return null;
            Prune();
            if (part == 0)
                open[key] = new Partial { Parts = parts, Started = UnityEngine.Time.realtimeSinceStartup };
            if (!open.TryGetValue(key, out Partial file) || file.Parts != parts || file.Next != part)
            {
                open.Remove(key);
                return null;
            }
            file.Data.Write(bytes, 0, bytes.Length);
            if (++file.Next < parts)
                return null;
            open.Remove(key);
            return file.Data.ToArray();
        }

        private void Prune()
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            List<string> old = null;
            foreach (KeyValuePair<string, Partial> file in open)
            {
                if (now - file.Value.Started > BenchLimits.Timeout)
                    (old = old ?? new List<string>()).Add(file.Key);
            }
            old?.ForEach(key => open.Remove(key));
        }
    }
}
