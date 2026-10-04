using System;
using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>Where a jumping ship is. Kept in the ship's ZDO so whoever owns the ship carries the jump on.</summary>
    public enum JumpState
    {
        None = 0,
        Frozen = 1,  // stopped at the source gate; the crew measures its deck spots against it
        Moved = 2,   // placed at the destination, still frozen, waiting for the destination to load
        Settled = 3  // checked clear at the destination; the crew may land
    }

    /// <summary>Every sea gate key, RPC name and id rule, so the modules never disagree about a name. Pillars,
    /// pairs and destinations use ids of our own (<see cref="IdKey"/>): the game renumbers every ZDOID on each
    /// world load (<c>ZDO.Load</c>: <c>m_uid.SetID(++ZDOID.m_loadID)</c>), so a stored ZDOID would point at
    /// something else after a restart. The anchor of a pair is the pillar with the smaller id: it holds the gate's
    /// own fields (destination, name, and the portals' <c>wf_mode</c>/<c>wf_owner</c>), and its id is the gate's
    /// id.</summary>
    public static class SeaGateFields
    {
        public const string PillarPrefab = "WF_SeaGatePillar";
        public static readonly int PillarHash = PillarPrefab.GetStableHashCode();

        // Pillar ZDO keys.
        public const string IdKey = "wf_sg_id";            // long, this pillar's own id
        public const string PartnerKey = "wf_sg_partner";  // long, the partner pillar's id
        public const string SidesKey = "wf_sg_sides";      // int, SideFront | SideBack: which sides are deep enough to exit on
        public const string DestKey = "wf_sg_dest";        // long, the destination gate's id (anchor only)
        public const string NameKey = "wf_sg_name";        // string, the gate's name (anchor only)

        // Ship ZDO keys.
        public const string JumpKey = "wf_sg_jump";        // int, the current jump's id
        public const string StateKey = "wf_sg_state";      // int, JumpState
        public const string SpeedKey = "wf_sg_speed";      // float, speed to give back on release
        public const string CrewKey = "wf_sg_crew";        // int, players aboard when the jump began
        public const string LandedKey = "wf_sg_landed";    // int, crew members who reported landing
        public const string LandedAtKey = "wf_sg_landedat";    // long ticks, when the latest crew report was counted
        public const string LandedPlayerPrefix = "wf_sg_landed_"; // + player id: int, the jump that player's report counted for
        public const string MoveAtKey = "wf_sg_moveat";    // long ticks, when the ship moves to the destination
        public const string UntilKey = "wf_sg_until";      // long ticks, give up waiting for the crew
        public const string SafeKey = "wf_sg_safe";        // long ticks, the ship takes no damage until then
        public const string DestPosKey = "wf_sg_dpos";     // Vector3, the ship's destination position
        public const string DestRotKey = "wf_sg_drot";     // Quaternion, the ship's destination rotation
        public const string DestAnchorKey = "wf_sg_danchor";   // Vector3, the destination anchor pillar
        public const string DestPartnerKey = "wf_sg_dpartner"; // Vector3, the destination partner pillar

        // Player ZDO key, written by each client for its own player.
        public const string HeavyKey = "wf_sg_heavy";      // bool, carrying something that may not teleport

        // RPCs on a pillar's ZNetView.
        public const string PairRpc = "wf_SeaGatePair";
        public const string UnpairRpc = "wf_SeaGateUnpair";
        public const string SetDestRpc = "wf_SeaGateSetDest";
        public const string SetNameRpc = "wf_SeaGateSetName";
        public const string SetModeRpc = "wf_SeaGateSetMode";

        // RPC on a ship's ZNetView: (int jump id, long player id), counted once per player per jump.
        public const string LandedRpc = "wf_SeaGateLanded";

        public const int SideFront = 1;
        public const int SideBack = 2;
        public const int BothSides = SideFront | SideBack;

        public static bool IsPillar(ZDO zdo) => zdo != null && zdo.GetPrefab() == PillarHash;

        public static long GetId(ZDO zdo) => zdo != null ? zdo.GetLong(IdKey, 0L) : 0L;
        public static long GetPartner(ZDO zdo) => zdo != null ? zdo.GetLong(PartnerKey, 0L) : 0L;
        public static int GetSides(ZDO zdo) => zdo != null ? zdo.GetInt(SidesKey, 0) : 0;
        public static long GetDest(ZDO zdo) => zdo != null ? zdo.GetLong(DestKey, 0L) : 0L;
        public static string GetName(ZDO zdo) => zdo != null ? zdo.GetString(NameKey, "") : "";

        /// <summary>A pair counts only when each pillar names the other: a one-sided link (a pairing race lost, a
        /// partner destroyed while unloaded) reads as unpaired, never as a gate.</summary>
        public static bool IsMutual(ZDO a, ZDO b)
        {
            long idA = GetId(a);
            long idB = GetId(b);
            return idA != 0L && idB != 0L && idA != idB && GetPartner(a) == idB && GetPartner(b) == idA;
        }

        /// <summary>The anchor of a mutual pair: the smaller id.</summary>
        public static bool IsAnchor(ZDO self, ZDO partner) => GetId(self) < GetId(partner);

        /// <summary>A random non-zero id.</summary>
        public static long NewId()
        {
            long id = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
            return id != 0L ? id : 1L;
        }

        public static JumpState GetState(ZDO ship) => ship != null ? (JumpState)ship.GetInt(StateKey, 0) : JumpState.None;
        public static int GetJump(ZDO ship) => ship != null ? ship.GetInt(JumpKey, 0) : 0;
        public static bool IsJumping(ZDO ship) => GetState(ship) != JumpState.None;

        /// <summary>Server time in ticks, the clock every machine shares (ZDO times are stored in it).</summary>
        public static long Now => ZNet.instance != null ? ZNet.instance.GetTime().Ticks : 0L;

        public static long After(float seconds) => Now + (long)(seconds * TimeSpan.TicksPerSecond);

        public static float WaterLevel => ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
    }
}
