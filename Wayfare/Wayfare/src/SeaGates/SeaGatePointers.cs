using System.Collections.Generic;
using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>One crew member's pointer on the picker's map, as last heard.</summary>
    internal sealed class CrewPointer
    {
        public Vector3 World;
        public float HeardAt;
        public long PlayerId;
        public string Name;
    }

    /// <summary>Everyone aboard a stopped ship sees the others' map pointers, so they can point things out to each other
    /// while the helmsman picks. Each crew client with the picker open sends its own pointer, as a world point, to every
    /// other crew member's machine (<see cref="SeaGateFields.PointerRpc"/> on the ship's ZNetView, aimed at each
    /// player's own peer, so nobody off the ship gets it): ten times a second while it moves over the map, twice a
    /// second while it rests there, nothing while it is off the map. A pointer is taken only for the stop this picker is
    /// open for and only from a machine that owns a player aboard, whose name goes beside it; one not heard from for
    /// <see cref="LifeSeconds"/> is dropped. Drawn by <see cref="SeaGatePointerMarks"/>.</summary>
    internal static class SeaGatePointers
    {
        private const float MoveSendSeconds = 0.1f;
        private const float RestSendSeconds = 0.5f;
        private const float MovePixels = 2f;
        private const float LifeSeconds = 1.5f;

        private static readonly Dictionary<long, CrewPointer> pointers = new Dictionary<long, CrewPointer>();   // by sender peer
        private static readonly List<long> peers = new List<long>();
        private static readonly List<long> stale = new List<long>();
        private static float sentAt;
        private static Vector2 sentPointer;

        /// <summary>Every frame on a machine with a map (called by <see cref="SeaGateMapDriver"/>).</summary>
        internal static void Tick()
        {
            Ship ship = SeaGatePicker.Ship;
            if (!SeaGatePicker.Active || ship == null || ship.m_nview == null || !ship.m_nview.IsValid())
            {
                Clear();
                return;
            }
            SendOwn(ship);
            DropStale();
            SeaGatePointerMarks.Draw(pointers, HelmsmanId(ship));
        }

        private static void SendOwn(Ship ship)
        {
            Minimap map = Minimap.instance;
            Vector2 pointer = ZInput.pointerPosition;
            if (map == null || !RectTransformUtility.RectangleContainsScreenPoint(map.m_mapImageLarge.rectTransform, pointer, null))
                return;
            bool moved = (pointer - sentPointer).sqrMagnitude > MovePixels * MovePixels;
            if (Time.unscaledTime - sentAt < (moved ? MoveSendSeconds : RestSendSeconds))
                return;
            sentAt = Time.unscaledTime;
            sentPointer = pointer;
            Vector3 world = map.ScreenToWorldPoint(pointer);
            foreach (long peer in CrewPeers(ship))
                ship.m_nview.InvokeRPC(peer, SeaGateFields.PointerRpc, SeaGatePicker.StopId, world.x, world.z);
        }

        /// <summary>The machines of everyone else aboard, once each: <c>Ship.m_players</c> holds every player in the
        /// ship's trigger, remote ones included.</summary>
        private static List<long> CrewPeers(Ship ship)
        {
            peers.Clear();
            foreach (Player player in ship.m_players)
            {
                long peer = player != null && player != Player.m_localPlayer ? player.GetOwner() : 0L;
                if (peer != 0L && !peers.Contains(peer))
                    peers.Add(peer);
            }
            return peers;
        }

        /// <summary>A crew member's pointer (<see cref="SeaGateFields.PointerRpc"/>), registered on every ship.</summary>
        internal static void OnPointer(Ship ship, long sender, int stopId, float x, float z)
        {
            if (!SeaGatePicker.Active || SeaGatePicker.Ship != ship || SeaGatePicker.StopId != stopId)
                return;
            Player player = CrewMemberOf(ship, sender);
            if (player == null)
                return;
            if (!pointers.TryGetValue(sender, out CrewPointer heard))
                pointers[sender] = heard = new CrewPointer();
            heard.World = new Vector3(x, 0f, z);
            heard.HeardAt = Time.unscaledTime;
            heard.PlayerId = player.GetPlayerID();
            heard.Name = player.GetPlayerName();
        }

        private static Player CrewMemberOf(Ship ship, long peer)
        {
            foreach (Player player in ship.m_players)
            {
                if (player != null && player != Player.m_localPlayer && player.GetOwner() == peer)
                    return player;
            }
            return null;
        }

        private static long HelmsmanId(Ship ship)
        {
            ShipControlls controls = ship.m_shipControlls;
            return controls != null ? controls.GetUser() : 0L;
        }

        private static void DropStale()
        {
            stale.Clear();
            foreach (KeyValuePair<long, CrewPointer> pair in pointers)
            {
                if (Time.unscaledTime - pair.Value.HeardAt > LifeSeconds)
                    stale.Add(pair.Key);
            }
            foreach (long peer in stale)
                pointers.Remove(peer);
        }

        internal static void Clear()
        {
            pointers.Clear();
            SeaGatePointerMarks.Clear();
        }
    }
}
