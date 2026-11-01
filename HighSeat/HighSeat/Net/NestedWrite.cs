using System;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// A package written into another (ZPackage.Write(ZPackage)) without the game's copy, on the server. It is how every
    /// RPC with a package parameter is written, once for each player it goes to (ZRpc.Serialize: the player list every
    /// 2 s (ZNet.SendPlayerList), the player history and admin list when a player joins, a routed RPC to one player, the
    /// game's own ZDO packages, many mods' RPCs), and how a routed RPC holds its parameters (RoutedRPCData.Serialize).
    /// The game copies the inner package (GetArray: its writer and stream flushed, then MemoryStream.ToArray) and writes
    /// that copy's length and bytes; here, after the same two flushes, the length and the bytes are written straight from
    /// the inner package's own buffer: the same bytes, no array for the garbage collector. Left to the game: a null inner
    /// package and a package written into itself. Server only (<see cref="NetGate.Nested"/>); it may run off the main
    /// thread (it touches only its two packages). The steps before writing are guarded (if they throw, the game's code
    /// runs for that call and this stays off for the session); the two writes are the game's own two writes, which would
    /// throw at the same point in the game's code, so nothing is written twice.
    /// </summary>
    [HarmonyPatch(typeof(ZPackage), nameof(ZPackage.Write), typeof(ZPackage))]
    internal static class NestedWrite
    {
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ZPackage __instance, ZPackage pkg, bool __runOriginal)
        {
            if (!__runOriginal || pkg == null || ReferenceEquals(pkg, __instance) || !NetGate.Nested ||
                !Bytes(pkg, out ArraySegment<byte> bytes))
            {
                return true;
            }
            __instance.m_writer.Write(bytes.Count);
            __instance.m_writer.Write(bytes.Array, bytes.Offset, bytes.Count);
            return false;
        }

        // GetArray's flushes, then the bytes it would copy: the stream's buffer from its start to its length.
        private static bool Bytes(ZPackage pkg, out ArraySegment<byte> bytes)
        {
            try
            {
                pkg.m_writer.Flush();
                pkg.m_stream.Flush();
                return pkg.m_stream.TryGetBuffer(out bytes);
            }
            catch (Exception e)
            {
                bytes = default;
                NetGate.Fail(NetGate.Feature.Nested, e);
                return false;
            }
        }
    }
}
