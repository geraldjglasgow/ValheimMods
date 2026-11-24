using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>
    /// A fake player's body: a ZDO of the game's Player prefab, made on the server the way a client's own character arrives
    /// there: its id under the fake's uid (a client's ZDOs are numbered from 1 under its session id), owned by the fake,
    /// set up from the prefab's ZNetView as ZNetView.Awake sets up a new object, but never persistent, so it is never
    /// saved and the game's own disconnect removes it (ZDOMan.RemovePeer destroys the non-persistent ZDOs of every peer
    /// that is gone). It is the peer's m_characterID, as RPC_CharacterID sets it, so it counts in GetAllCharacterZDOS and
    /// the player list. Once, it is named and given a player id, the animator parameters ZSyncAnimation reads for a player
    /// on foot (the forward speed at the path's speed, on the ground), a healthy walker's health, stamina, noise and flags,
    /// and its looks (<see cref="FakeLook"/>), so each update is about the size of a real player's. Then it moves along its
    /// path as often as a client sends: position, rotation and velocity (what a remote ZSyncTransform reads and
    /// extrapolates). The server takes a non-owner's new position only from the owner's data, so each move raises the data
    /// revision itself, through the ZDO's own IncreaseDataRevision (where a patch watching changes sees it), and then marks
    /// the latest revision as held by the fake's own peer, as ZDOMan.RPC_ZDOData records what a client sent, so the server
    /// never sends a fake its own body.
    /// </summary>
    internal static class FakeCharacter
    {
        // ZSyncAnimation keeps an animator parameter under its name's hash plus this salt (its c_ZDOSalt).
        private const int AnimSalt = 438569;
        private static readonly int PlayerPrefab = "Player".GetStableHashCode();
        private static readonly int ForwardSpeed = AnimSalt + ZSyncAnimation.GetHash("forward_speed");
        private static readonly int SidewaySpeed = AnimSalt + ZSyncAnimation.GetHash("sideway_speed");
        private static readonly int TurnSpeed = AnimSalt + ZSyncAnimation.GetHash("turn_speed");
        private static readonly int OnGround = AnimSalt + ZSyncAnimation.GetHash("onGround");
        private static readonly int InWater = AnimSalt + ZSyncAnimation.GetHash("inWater");
        private static readonly int Encumbered = AnimSalt + ZSyncAnimation.GetHash("encumbered");
        private static readonly int Flying = AnimSalt + ZSyncAnimation.GetHash("flying");

        /// <summary>Makes the body and gives it to the peer; false when a ZDO with its id exists already.</summary>
        internal static bool Create(FakePlayer fake, long playerId, float now, System.Random random)
        {
            var id = new ZDOID(fake.Uid, 1u);
            if (ZDOMan.instance.GetZDO(id) != null) return false;
            ZDO zdo = ZDOMan.instance.CreateNewZDO(id, fake.Path.Position(now), PlayerPrefab);
            Shape(zdo, fake, now);
            Describe(zdo, fake, playerId, random);
            fake.Body = zdo;
            fake.BodyId = id;
            fake.Peer.m_characterID = id;
            Hold(fake);
            return true;
        }

        // What ZNetView.Awake gives a new object's ZDO, from the Player prefab; then the fake as its owner.
        private static void Shape(ZDO zdo, FakePlayer fake, float now)
        {
            GameObject prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(PlayerPrefab) : null;
            ZNetView view = prefab ? prefab.GetComponent<ZNetView>() : null;
            zdo.Persistent = false;
            zdo.Type = view ? view.m_type : ZDO.ObjectType.Default;
            zdo.Distant = view && view.m_distant;
            zdo.SetPrefab(PlayerPrefab);
            zdo.SetRotation(fake.Path.Facing(now));
            zdo.SetOwner(fake.Uid);
        }

        private static void Describe(ZDO zdo, FakePlayer fake, long playerId, System.Random random)
        {
            zdo.Set(ZDOVars.s_playerID, playerId);
            zdo.Set(ZDOVars.s_playerName, fake.Name);
            Animate(zdo, fake.Path.Speed);
            Vitals(zdo);
            FakeLook.Dress(zdo, random);
        }

        // What ZSyncAnimation keeps for a player on foot (Character.UpdateWalking's parameters) and the animator's speed.
        private static void Animate(ZDO zdo, float speed)
        {
            zdo.Set(ForwardSpeed, speed);
            zdo.Set(SidewaySpeed, 0f);
            zdo.Set(TurnSpeed, 0f);
            zdo.Set(OnGround, 1);
            zdo.Set(InWater, 0);
            zdo.Set(Encumbered, 0);
            zdo.Set(Flying, 0);
            zdo.Set(ZDOVars.s_animationSpeed, 1f);
        }

        // The rest of what a player's own game keeps on its body, at a healthy walker's values, so an update is the size of a real one.
        private static void Vitals(ZDO zdo)
        {
            zdo.Set(ZDOVars.s_maxHealth, 120f);
            zdo.Set(ZDOVars.s_health, 120f);
            zdo.Set(ZDOVars.s_stamina, 80f);
            zdo.Set(ZDOVars.s_eitr, 0f);
            zdo.Set(ZDOVars.s_noise, 15f);
            zdo.Set(ZDOVars.s_stealth, 1f);
            zdo.Set(ZDOVars.s_baseValue, 0);
            zdo.Set(ZDOVars.s_pvp, false);
            zdo.Set(ZDOVars.s_dead, false);
            zdo.Set(ZDOVars.s_isBlockingHash, false);
            zdo.Set(ZDOVars.s_inBed, false);
            zdo.Set(ZDOVars.s_dodgeinv, false);
        }

        /// <summary>One step along the path, as a client's update arrives; drops the body when the game has removed it.</summary>
        internal static void Move(FakePlayer fake, float now)
        {
            ZDO zdo = fake.Body;
            if (zdo == null) return;
            if (ZDOMan.instance.GetZDO(fake.BodyId) != zdo)
            {
                Lose(fake);
                return;
            }
            zdo.InternalSetPosition(fake.Path.Position(now));
            zdo.SetRotation(fake.Path.Facing(now));
            zdo.Set(ZDOVars.s_velHash, fake.Path.Velocity(now));
            zdo.IncreaseDataRevision();
            Hold(fake);
        }

        // The fake's own peer holds its body's latest revisions, as ZDOMan.RPC_ZDOData records for what a client sent.
        private static void Hold(FakePlayer fake)
        {
            ZDO zdo = fake.Body;
            if (fake.Holdings == null || zdo == null) return;
            fake.Holdings.m_zdos[zdo.m_uid] = new ZDOMan.ZDOPeer.PeerZDOInfo(zdo.DataRevision, zdo.OwnerRevision, Time.time);
        }

        // Something else destroyed the body (its pooled ZDO may already be another object's): never touch it again. The
        // peer keeps the id, as a client's peer keeps a dead character's: GetAllCharacterZDOS skips an id with no ZDO, and
        // the fake's entry in the player list is still known by it.
        private static void Lose(FakePlayer fake)
        {
            fake.Body = null;
            fake.BodyId = ZDOID.None;
        }
    }
}
