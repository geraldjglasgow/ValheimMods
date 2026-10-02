using System;
using System.Collections.Generic;

namespace EliteCrafting.Affixes
{
    /// <summary>One filled socket: the gem's stone id and the affix roll it gave when it was set (sockets.md section 3).</summary>
    public readonly struct SocketGem
    {
        public SocketGem(string gemId, AffixRoll roll)
        {
            GemId = gemId;
            Roll = roll;
        }

        /// <summary>The stone id of the gem (<c>gem_frost</c>); its definition may be gone, the roll still counts.</summary>
        public string GemId { get; }

        /// <summary>The affix the gem gave, with its strength grade and value (stored like an affix, item-data.md).</summary>
        public AffixRoll Roll { get; }
    }

    /// <summary>
    /// The parsed <c>ecf_sockets</c>, <c>ecf_gems</c> and <c>ecf_catalyst</c> of an item (sockets.md section 2). A key
    /// that does not parse keeps its text in the matching <c>*Raw</c> field and is written back unchanged, never
    /// destroyed; changing that part through the builder replaces it. Immutable: every change makes a new instance.
    /// </summary>
    internal sealed class SocketData
    {
        public static readonly SocketData None = new SocketData();

        /// <summary>Sockets on the item, filled or not.</summary>
        public int Count { get; private set; }

        /// <summary>The filled sockets, oldest first (the next gem into a full item breaks the first).</summary>
        public SocketGem[] Gems { get; private set; } = Array.Empty<SocketGem>();

        /// <summary>The catalyst's essence family id; null = no catalyst.</summary>
        public string? CatalystFamily { get; private set; }

        /// <summary>Catalyst quality in percent points.</summary>
        public float CatalystQuality { get; private set; }

        public string? CountRaw { get; private set; }
        public string? GemsRaw { get; private set; }
        public string? CatalystRaw { get; private set; }

        public bool IsEmpty =>
            Count == 0 && Gems.Length == 0 && CatalystFamily == null && CountRaw == null && GemsRaw == null && CatalystRaw == null;

        public static SocketData Read(int count, string? countRaw, SocketGem[] gems, string? gemsRaw, string? family, float quality,
            string? catalystRaw)
        {
            SocketData data = new SocketData
            {
                Count = count, CountRaw = countRaw, Gems = gems, GemsRaw = gemsRaw,
                CatalystFamily = family, CatalystQuality = quality, CatalystRaw = catalystRaw,
            };
            return data.IsEmpty ? None : data;
        }

        public SocketData WithCount(int count) => Copy(d => { d.Count = Math.Max(count, 0); d.CountRaw = null; });

        public SocketData WithGems(List<SocketGem> gems) => Copy(d => { d.Gems = gems.ToArray(); d.GemsRaw = null; });

        public SocketData WithCatalyst(string? family, float quality) => Copy(d =>
        {
            d.CatalystFamily = family;
            d.CatalystQuality = family == null ? 0f : quality;
            d.CatalystRaw = null;
        });

        private SocketData Copy(Action<SocketData> change)
        {
            SocketData copy = (SocketData)MemberwiseClone();
            change(copy);
            return copy.IsEmpty ? None : copy;
        }
    }
}
