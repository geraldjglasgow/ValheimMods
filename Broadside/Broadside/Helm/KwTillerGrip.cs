using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A helmsman's hold on a tiller's peg (<see cref="KwRudderLinkage"/>): the tiller's own, or a deck tiller's. He stands
    /// beside the grip facing the bow, the peg at his side. At the height the lay gives it (<see cref="KwRudderLay.Hand"/>
    /// over the deck) the hand on the peg's side holds it; a peg standing higher over the deck he is on (a deck lower than
    /// the lay's) takes both hands, the near one on the peg and the other on the bar beside it, and he stands a little
    /// further back and out so both reach. His upper body turns toward the peg, and leans in the lower it is. His hands
    /// follow the blade as drawn (the game's paddling sway at slow speed too); his feet the helm's steady turn
    /// (<see cref="Stand"/>), so a sway moves only his arms.
    /// </summary>
    public sealed class KwTillerGrip : IKwHelmGrip
    {
        // The fist's rise from the idle hand's place to the peg's middle; above this height over his deck the peg takes
        // both hands; how close to his middle the peg may come before he steps away; how far across his front the peg goes
        // before the other hand takes it.
        private const float PegRise = 0.03f, BothAbove = 1.05f, Crowd = 0.15f, Switch = 0.08f;

        // Where he stands from the idle hand's place (kit axes of the rudder): beside the grip for one hand, further back
        // and out for both.
        private static readonly Vector3 One = new Vector3(0f, KwRudderLay.Side, -KwRudderLay.Hand);
        private static readonly Vector3 Both = new Vector3(-0.15f, KwRudderLay.Side + 0.05f, -KwRudderLay.Hand);

        private readonly KwRudderLinkage link;
        private readonly Ship ship;
        private readonly Vector3 stand;
        private bool right, fresh = true;

        /// <summary>A tiller's hold, the deck the helmsman stands on <paramref name="lift"/> over the lay's (the rudder's up).</summary>
        public KwTillerGrip(KwRudderLinkage link, Ship ship, float lift)
        {
            (this.link, this.ship) = (link, ship);
            BothHands = KwRudderLay.Hand - lift > BothAbove;
            stand = KwMesh.ToUnity(BothHands ? Both : One) + Vector3.up * lift;
        }

        /// <summary>Whether the peg stands high enough for both hands.</summary>
        public bool BothHands { get; }

        /// <summary>The parts whose colliders the helmsman passes through while he holds them: the rudder's.</summary>
        public Transform Parts => link.transform;

        /// <summary>
        /// Where his feet go, in the world: beside the grip with the helm where the game holds it (the ship's rudder value),
        /// on the deck he stands on.
        /// </summary>
        public Vector3 Stand => link.transform.TransformPoint(link.HandAt(Steady) + stand);

        /// <summary>The way he faces at the tiller: the ship's bow.</summary>
        public Vector3 Bow => ship.transform.forward;

        // The blade's turn for the helm the game holds, without the paddling sway (as the game's ship turns its rudder).
        private Quaternion Steady => Quaternion.Euler(0f, ship.m_rudderRotationMax * -ship.GetRudderValue(), 0f);

        /// <summary>Whether the peg, with the helm as held, has come in too close to his middle for his hand: he steps away.</summary>
        public bool Crowded(Player player)
        {
            Vector3 off = link.transform.TransformPoint(link.HandAt(Steady)) - player.transform.position;
            off.y = 0f;
            return off.magnitude < Crowd;
        }

        /// <summary>Where the helmsman's hands go this frame, and his turn and lean.</summary>
        public KwHelmHold Hold(Player player, float dt)
        {
            Transform rudder = link.transform, body = player.transform;
            Quaternion turn = link.Turn;
            Vector3 peg = rudder.TransformPoint(link.HandAt(turn)) + rudder.up * PegRise;
            Vector3? bar = BothHands ? rudder.TransformPoint(link.BarAt(turn)) : (Vector3?)null;
            Side(body, peg);
            float twist = Twist(body, peg), lean = Lean(body, peg);
            return right ? new KwHelmHold(bar, peg, lean, twist) : new KwHelmHold(peg, bar, lean, twist);
        }

        /// <summary>A new helmsman: his hands take hold afresh.</summary>
        public void Reset() => fresh = true;

        // The hand on the peg's side of him; the other takes it only once the peg is well across his front.
        private void Side(Transform body, Vector3 peg)
        {
            float across = Vector3.Dot(peg - body.position, body.right);
            if (fresh || (right ? across < -Switch : across > Switch))
                right = across >= 0f;
            fresh = false;
        }

        // Turned toward the peg: a little with one hand (it is at his side), more with both (it is before him).
        private float Twist(Transform body, Vector3 peg)
        {
            Vector3 to = peg - body.position, ahead = body.forward;
            (to.y, ahead.y) = (0f, 0f);
            if (to.sqrMagnitude < 1e-4f || ahead.sqrMagnitude < 1e-4f)
                return 0f;
            float most = BothHands ? 40f : 15f;
            return Mathf.Clamp(Vector3.SignedAngle(ahead, to, Vector3.up) * (BothHands ? 0.5f : 0.2f), -most, most);
        }

        // Leaned in a little, more the lower the peg is over his feet.
        private float Lean(Transform body, Vector3 peg)
        {
            float height = Vector3.Dot(peg - body.position, body.up);
            return Mathf.Clamp((0.95f - height) * 60f, 0f, 25f) + (BothHands ? 8f : 3f);
        }
    }
}
