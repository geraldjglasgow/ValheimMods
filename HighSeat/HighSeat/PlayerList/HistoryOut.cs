using System;
using System.Collections.Generic;
using System.Text;
using Splatform;

namespace HighSeat
{
    /// <summary>
    /// The player history as the server sends it (RPC "HistoricalPlayerList"), cut to what a player's game needs. The game
    /// sends the whole history (every player who ever joined: id, display name, server-assigned name, PlayFab id, some 60
    /// to 90 bytes each) to every player at each join and change; past some 6000 to 8000 players it is over Steam's 512 KB
    /// message limit and the send stays stuck at the head of every player's queue. A player's game uses the list for three
    /// things: a piece it builds records its builder's place in the list (Piece.SetCreator: FindIndex of the builder's own
    /// id, saved in the piece as "creatorIndex"); the build piece author box (Hud, off by default on PC, on by default on
    /// PS5) shows the entry at a hovered piece's place; and on PlayStation every entry's id is checked against the block
    /// lists. The place is what counts, so every place stays where it is and a piece always records its builder's true
    /// place. Sent in full: every player online (their own place, their pieces' author, a blocked player who is here now),
    /// then up to <see cref="RecentPlayers"/> more, those seen latest this session and then the newest arrivals
    /// (<see cref="SeenPlayers"/>). Every other place is a place holder of 7 bytes: the id "H_0" (a platform no game has: it
    /// reads back without the parse failure an empty id logs, matches no player and no block list, and the author box shows
    /// no name, no id and the default picture) and three empty names. A history of up to <see cref="RecentPlayers"/>
    /// players plus those online goes out whole, exactly as the game sends it.
    /// The message holds at most <see cref="MaxBytes"/>: every place fits up to about 35000 players. Past that the list
    /// ends where the room ends (the online players after that point and the newest arrivals are left out), and only a
    /// player whose own place is in it in full is sent it (<see cref="Reaches"/>): a player further on gets no list, so
    /// their game records no place for their pieces (-1, author unknown) rather than a wrong one (said once in the log).
    /// The bytes of each build are kept: <see cref="Version"/> moves only when they differ, so a player who holds the
    /// newest list is not sent it again. The server's own saved history stays whole. Main thread, server only.
    /// </summary>
    internal static class HistoryOut
    {
        /// <summary>The most a history message holds: half of Steam's 524288-byte message limit.</summary>
        private const int MaxBytes = 256 * 1024;

        /// <summary>
        /// Players kept in full besides those online: at some 60 to 90 bytes each, at most about 18 KB, so the author box
        /// and the block check cover everyone who played here lately (the regulars of the last hours on a busy server) as
        /// well as everyone online, while each other player costs only their 7-byte place.
        /// </summary>
        private const int RecentPlayers = 200;

        private const int HolderBytes = 7;
        private const int HolderRun = 512;
        private static readonly byte[] Holders = MakeHolders();
        private static readonly List<int> Full = new List<int>();
        private static readonly HashSet<int> InFull = new HashSet<int>();
        private static readonly List<int> Candidates = new List<int>();
        private static readonly ZPackage Pkg = new ZPackage();
        private static byte[] _last = new byte[0];
        private static int _lastSize = -1;
        private static int _told;

        public static int Version { get; private set; }
        public static bool Built => _lastSize >= 0;
        public static ZPackage Package => Pkg;

        public static void Reset()
        {
            _lastSize = -1;
            _told = 0;
            Version = 0;
            Full.Clear();
            InFull.Clear();
        }

        public static void Build(ZNet net)
        {
            List<ZNet.CrossNetworkUserInfo> history = ZNet.m_world.m_playerHistory;
            HistoryIndex.Sync(history);
            long bytes = Online(net, history);
            int end = bytes > MaxBytes ? Cut(history) : history.Count;
            if (end == history.Count)
            {
                AddRecent(history, bytes);
            }
            Write(history, end);
            Keep();
            Tell(history.Count, end);
        }

        /// <summary>Whether the newest list holds this player's own place in full, so their game finds itself in it.</summary>
        public static bool Reaches(ZNet net, ZNetPeer peer)
        {
            int place = HistoryIndex.IndexOf(PeerIds.Of(net, peer));
            return place >= 0 && InFull.Contains(place);
        }

        // Every player online in full, the rest place holders: the bytes that makes.
        private static long Online(ZNet net, List<ZNet.CrossNetworkUserInfo> history)
        {
            Full.Clear();
            InFull.Clear();
            long bytes = 4 + (long)HolderBytes * history.Count;
            foreach (ZNet.PlayerInfo player in net.m_players)
            {
                int place = HistoryIndex.IndexOf(player.m_userInfo.m_id);
                if (place >= 0 && InFull.Add(place))
                {
                    Full.Add(place);
                    bytes += Size(history[place]) - HolderBytes;
                }
            }
            Full.Sort();
            return bytes;
        }

        // The list ends where the room ends: the places that fit, the online players among them in full.
        private static int Cut(List<ZNet.CrossNetworkUserInfo> history)
        {
            long used = 4;
            int from = 0;
            int kept = 0;
            for (; kept < Full.Count; kept++)
            {
                int place = Full[kept];
                long before = used + (long)(place - from) * HolderBytes;
                long size = Size(history[place]);
                if (before + size > MaxBytes)
                {
                    break;
                }
                used = before + size;
                from = place + 1;
            }
            int stop = kept < Full.Count ? Full[kept] : history.Count;
            for (int i = kept; i < Full.Count; i++)
            {
                InFull.Remove(Full[i]);
            }
            Full.RemoveRange(kept, Full.Count - kept);
            return (int)Math.Min(from + (MaxBytes - used) / HolderBytes, stop);
        }

        private static void AddRecent(List<ZNet.CrossNetworkUserInfo> history, long bytes)
        {
            SeenPlayers.Candidates(history, Candidates, RecentPlayers + Full.Count);
            int added = 0;
            for (int i = 0; i < Candidates.Count && added < RecentPlayers; i++)
            {
                if (TryAdd(history, Candidates[i], ref bytes))
                {
                    added++;
                }
            }
            Full.Sort();
        }

        // One more place in full, unless it is in already or would not fit.
        private static bool TryAdd(List<ZNet.CrossNetworkUserInfo> history, int place, ref long bytes)
        {
            if (InFull.Contains(place))
            {
                return false;
            }
            long more = Size(history[place]) - HolderBytes;
            if (bytes + more > MaxBytes)
            {
                return false;
            }
            InFull.Add(place);
            Full.Add(place);
            bytes += more;
            return true;
        }

        private static void Write(List<ZNet.CrossNetworkUserInfo> history, int end)
        {
            Pkg.Clear();
            Pkg.Write(end);
            int from = 0;
            foreach (int place in Full)
            {
                WriteHolders(place - from);
                WriteEntry(history[place]);
                from = place + 1;
            }
            WriteHolders(end - from);
        }

        // As the game writes each entry; a missing name goes out empty, as the game writes the history to disk.
        private static void WriteEntry(ZNet.CrossNetworkUserInfo entry)
        {
            Pkg.Write(entry.m_id.ToString());
            Pkg.Write(entry.m_displayName ?? string.Empty);
            Pkg.Write(entry.m_serverAssignedDisplayName ?? string.Empty);
            Pkg.Write(entry.m_playfabId ?? string.Empty);
        }

        private static void WriteHolders(int count)
        {
            while (count > 0)
            {
                int run = Math.Min(count, HolderRun);
                Pkg.m_writer.Write(Holders, 0, run * HolderBytes);
                count -= run;
            }
        }

        // The new bytes against the last build's: the version moves only when they differ.
        private static void Keep()
        {
            Pkg.m_writer.Flush();
            int size = (int)Pkg.m_stream.Length;
            if (size > MaxBytes || !Pkg.m_stream.TryGetBuffer(out ArraySegment<byte> bytes))
            {
                throw new InvalidOperationException($"the player history came to {size} bytes, more than {MaxBytes}");
            }
            if (size == _lastSize && Same(bytes, size))
            {
                return;
            }
            if (_last.Length < size)
            {
                _last = new byte[Math.Max(size, _last.Length * 2)];
            }
            Buffer.BlockCopy(bytes.Array, bytes.Offset, _last, 0, size);
            _lastSize = size;
            Version++;
        }

        private static bool Same(ArraySegment<byte> bytes, int size)
        {
            byte[] array = bytes.Array;
            int offset = bytes.Offset;
            for (int i = 0; i < size; i++)
            {
                if (array[offset + i] != _last[i])
                {
                    return false;
                }
            }
            return true;
        }

        // Each shape (whole, in part, cut) is said once per session.
        private static void Tell(int players, int end)
        {
            int shape = end < players ? 2 : Full.Count < players ? 1 : 0;
            if ((_told & (1 << shape)) != 0)
            {
                return;
            }
            _told |= 1 << shape;
            if (shape == 2)
            {
                HighSeat.Log.LogWarning($"the player history ({players} players) is too big to send whole even as places: " +
                    $"the first {end} places go out, and a player whose own place is further on gets no history (pieces " +
                    "they build record no author)");
                return;
            }
            HighSeat.Log.LogInfo(shape == 1
                ? $"player history: {players} players, {Full.Count} sent in full and the rest as places ({_lastSize} bytes)"
                : $"player history: {players} players, all sent in full ({_lastSize} bytes)");
        }

        // An entry's bytes as ZPackage.Write(string) writes its four texts: a 7-bit length, then the UTF-8 bytes.
        private static long Size(ZNet.CrossNetworkUserInfo entry) =>
            Text(IdBytes(entry.m_id)) + Text(Utf8(entry.m_displayName)) + Text(Utf8(entry.m_serverAssignedDisplayName)) +
            Text(Utf8(entry.m_playfabId));

        // PlatformUserID.ToString(): "platform_user" when valid, else "".
        private static int IdBytes(PlatformUserID id) =>
            id.IsValid ? Utf8(id.m_platform.ToString()) + 1 + Utf8(id.m_userID) : 0;

        private static int Utf8(string text) => text == null ? 0 : Encoding.UTF8.GetByteCount(text);

        private static long Text(int bytes) =>
            bytes + (bytes < 0x80 ? 1 : bytes < 0x4000 ? 2 : bytes < 0x200000 ? 3 : bytes < 0x10000000 ? 4 : 5);

        // "H_0" (its length, 3, then the text), then three empty strings, repeated.
        private static byte[] MakeHolders()
        {
            byte[] one = { 3, (byte)'H', (byte)'_', (byte)'0', 0, 0, 0 };
            byte[] run = new byte[HolderRun * HolderBytes];
            for (int i = 0; i < HolderRun; i++)
            {
                Buffer.BlockCopy(one, 0, run, i * HolderBytes, HolderBytes);
            }
            return run;
        }
    }
}
