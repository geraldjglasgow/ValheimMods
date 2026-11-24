namespace DevBridge.Load
{
    /// <summary>
    /// The connection of a fake player (/load): the game's ISocket for a peer that is not there. Everything the server
    /// sends it is counted and dropped: all bytes, and apart the world data (the ZDOData messages ZDOMan.SendZDOs sends),
    /// told by a message's first four bytes, the stable hash of its name that ZRpc writes first, read in place (the
    /// package's position put back after, nothing copied). Its send queue is always empty, so the server's send loop
    /// always offers it its whole budget (10 KB a send), as to a player on a fast line: the figures are what the server
    /// offers, and leave out the copy and the work a Steam socket does for each send. It never receives anything, so it
    /// never answers the ZRpc pings either (<see cref="FakeTicker"/> keeps its connection from timing out). Dispose (the
    /// game's own disconnect) and Close (a ZRpc timeout, the game stopping) shut it; a shut socket reads as disconnected,
    /// which ZNet.UpdatePeers finishes as for any lost connection. HighSeat's delta packages ("HighSeat_ZDODeltas", sent
    /// in place of ZDOData to a fake that said hello, <see cref="FakeDeltas"/>) count as world data too, and apart; a
    /// verifying fake's socket also hands every world package to its copy of the world (<see cref="Shadow"/>).
    /// </summary>
    internal sealed class FakeSocket : ISocket
    {
        private static readonly int ZdoData = "ZDOData".GetStableHashCode();
        private readonly string host;
        private readonly string endPoint;
        private int sentSinceStats;

        internal FakeSocket(string host, string endPoint)
        {
            this.host = host;
            this.endPoint = endPoint;
        }

        internal bool Open { get; private set; } = true;

        /// <summary>Bytes the server sent it, every message.</summary>
        internal long Sent { get; private set; }

        /// <summary>Bytes of world data (ZDOData messages), and how many such messages.</summary>
        internal long WorldBytes { get; private set; }

        internal int WorldSends { get; private set; }

        /// <summary>Bytes of world data that came in HighSeat's delta format (a part of <see cref="WorldBytes"/>).</summary>
        internal long DeltaWorldBytes { get; private set; }

        /// <summary>A verifying fake's copy of the world, or null.</summary>
        internal FakeShadow Shadow { get; set; }

        public void Send(ZPackage pkg)
        {
            if (!Open) return;
            int size = pkg.Size();
            Sent += size;
            sentSinceStats += size;
            int hash = size < 4 ? 0 : MethodHash(pkg);
            if (hash != ZdoData && hash != FakeDeltas.DataHash) return;
            WorldBytes += size;
            WorldSends++;
            if (hash == FakeDeltas.DataHash) DeltaWorldBytes += size;
            Shadow?.Receive(pkg, hash);
        }

        // ZRpc writes the method's stable hash first; read it there and put the package's position back as it was.
        private static int MethodHash(ZPackage pkg)
        {
            int at = pkg.GetPos();
            pkg.SetPos(0);
            int hash = pkg.ReadInt();
            pkg.SetPos(at);
            return hash;
        }

        public ZPackage Recv() => null;

        public bool IsConnected() => Open;

        public int GetSendQueueSize() => 0;

        public int GetCurrentSendRate() => 0;

        public bool IsHost() => false;

        public bool GotNewData() => false;

        public void Close() => Open = false;

        public void Dispose() => Open = false;

        public string GetEndPointString() => endPoint;

        public string GetHostName() => host;

        public ISocket Accept() => null;

        public int GetHostPort() => -1;

        public bool Flush() => true;

        public void VersionMatch()
        {
        }

        public void GetAndResetStats(out int totalSent, out int totalRecv)
        {
            totalSent = sentSinceStats;
            totalRecv = 0;
            sentSinceStats = 0;
        }

        public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
        {
            localQuality = 1f;
            remoteQuality = 1f;
            ping = 0;
            outByteSec = 0f;
            inByteSec = 0f;
        }
    }
}
