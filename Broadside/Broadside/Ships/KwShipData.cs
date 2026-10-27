using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship's blueprint as its ZDO carries it: the blueprint's JSON (<see cref="KwBpFile"/>), its pieces placed in the
    /// ship's own frame, gzipped and in base 64, so every peer that loads the ship builds the same one from it.
    /// </summary>
    public static class KwShipData
    {
        public static string Pack(KwBlueprint blueprint)
        {
            byte[] text = Encoding.UTF8.GetBytes(KwBpFile.Write(blueprint));
            using MemoryStream packed = new MemoryStream();
            using (GZipStream zip = new GZipStream(packed, CompressionMode.Compress))
                zip.Write(text, 0, text.Length);
            return Convert.ToBase64String(packed.ToArray());
        }

        public static KwBlueprint? Unpack(string saved)
        {
            if (string.IsNullOrEmpty(saved))
                return null;
            using MemoryStream packed = new MemoryStream(Convert.FromBase64String(saved));
            using GZipStream zip = new GZipStream(packed, CompressionMode.Decompress);
            using StreamReader reader = new StreamReader(zip, Encoding.UTF8);
            // exactly as packed: no piece left out (a sea edit's tombstone is no prefab the game has) and none turned (it was
            // turned bow forward at launch; an edit to its rig must never turn it round), so every index stays (KwSeaApply)
            return KwBpFile.Exact("ship", reader.ReadToEnd());
        }

        /// <summary>The same blueprint with every piece moved by -origin: placed in a frame whose origin is there.</summary>
        public static KwBlueprint Rebased(KwBlueprint blueprint, Vector3 origin)
        {
            KwBlueprint moved = new KwBlueprint(blueprint.Name);
            foreach (KwBpPiece piece in blueprint.Pieces)
                moved.Pieces.Add(new KwBpPiece(piece.Prefab, piece.Position - origin, piece.Rotation, piece.Data));
            return moved;
        }
    }
}
