using System.Collections.Generic;
using System.Text;
using EliteCrafting.Core;

namespace EliteCrafting.Affixes
{
    /// <summary>
    /// Reads and writes the socket keys (sockets.md section 2): <c>ecf_sockets</c> an integer, <c>ecf_gems</c> the
    /// filled sockets as <c>gem:affix:grade:value</c> entries separated by <c>;</c>, oldest first, and
    /// <c>ecf_catalyst</c> as <c>family:quality</c>. Culture-invariant numbers. A key that does not parse in full is
    /// kept verbatim and written back; the entries of <c>ecf_gems</c> that do parse still count.
    /// </summary>
    internal static class SocketCodec
    {
        public static SocketData Read(Dictionary<string, string> data)
        {
            string? count = Text(data, ItemKeys.Sockets);
            string? gems = Text(data, ItemKeys.Gems);
            string? catalyst = Text(data, ItemKeys.Catalyst);
            if (count == null && gems == null && catalyst == null)
            {
                return SocketData.None;
            }
            bool countOk = Numbers.TryInt(count, out int sockets) && sockets >= 0;
            SocketGem[] parsed = ParseGems(gems, out bool gemsOk);
            bool catalystOk = TryCatalyst(catalyst, out string? family, out float quality);
            return SocketData.Read(countOk ? sockets : 0, countOk ? null : count, parsed, gemsOk ? null : gems,
                family, quality, catalystOk ? null : catalyst);
        }

        public static void Write(SocketData sockets, Dictionary<string, string> target)
        {
            ItemWriter.Set(target, ItemKeys.Sockets, sockets.CountRaw ?? (sockets.Count > 0 ? Numbers.Format(sockets.Count) : null));
            ItemWriter.Set(target, ItemKeys.Gems, sockets.GemsRaw ?? EncodeGems(sockets.Gems));
            ItemWriter.Set(target, ItemKeys.Catalyst, sockets.CatalystRaw ?? EncodeCatalyst(sockets));
        }

        public static string Encode(SocketGem gem) => gem.GemId + ItemKeys.FieldSeparator + ItemCodec.Encode(gem.Roll);

        private static string? EncodeGems(SocketGem[] gems)
        {
            if (gems.Length == 0)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder(gems.Length * 28);
            for (int i = 0; i < gems.Length; i++)
            {
                sb.Append(i == 0 ? "" : ItemKeys.EntrySeparator.ToString()).Append(Encode(gems[i]));
            }
            return sb.ToString();
        }

        private static string? EncodeCatalyst(SocketData sockets) => sockets.CatalystFamily == null ? null
            : sockets.CatalystFamily + ItemKeys.FieldSeparator + Numbers.Format(sockets.CatalystQuality);

        // gemsOk is false when any entry fails: the text is then kept and written back as it was.
        private static SocketGem[] ParseGems(string? text, out bool ok)
        {
            ok = true;
            if (text == null)
            {
                return System.Array.Empty<SocketGem>();
            }
            List<SocketGem> gems = new List<SocketGem>();
            foreach (string entry in text.Split(ItemKeys.EntrySeparator))
            {
                if (TryGem(entry, out SocketGem gem))
                {
                    gems.Add(gem);
                }
                else
                {
                    ok = false;
                }
            }
            return gems.ToArray();
        }

        private static bool TryGem(string text, out SocketGem gem)
        {
            gem = default;
            string[] f = text.Split(ItemKeys.FieldSeparator);
            if (f.Length != 4 || !Ids.IsValid(f[0]) || !Ids.IsValid(f[1]))
            {
                return false;
            }
            if (!Numbers.TryInt(f[2], out int grade) || grade < 1 || !Numbers.TryFloat(f[3], out float value))
            {
                return false;
            }
            gem = new SocketGem(f[0], new AffixRoll(f[1], grade, value));
            return true;
        }

        private static bool TryCatalyst(string? text, out string? family, out float quality)
        {
            family = null;
            quality = 0f;
            if (text == null)
            {
                return true;
            }
            string[] f = text.Split(ItemKeys.FieldSeparator);
            if (f.Length != 2 || !Ids.IsValid(f[0]) || !Numbers.TryFloat(f[1], out quality) || quality < 0f)
            {
                quality = 0f;
                return false;
            }
            family = f[0];
            return true;
        }

        private static string? Text(Dictionary<string, string> data, string key) =>
            data.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value) ? value : null;
    }
}
