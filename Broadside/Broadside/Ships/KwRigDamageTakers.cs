using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The rig's takers for the shot router (<see cref="KwShipHitRouter"/>), on the ship's owner; each turns what the shot
    /// meets into marks (<see cref="KwRigDamageCuts"/>) and returns the energy it took. Takers never take the ship's health.
    /// <list type="bullet">
    /// <item>A spar (a mast, a yard, a boom, a gaff, a bowsprit): a ball half <see cref="Bite"/> wide passing its axis cuts
    /// the band of its section within that of its path (<see cref="KwRigSection.Band"/>), as much as its energy can cut,
    /// <see cref="Wood"/> to the square metre of section; that much energy it takes. Past half its section a mast,
    /// bowsprit or cap falls.</item>
    /// <item>A sail: holed where the ball passes (<see cref="KwRigDamageCuts.Hole"/>), the ball going on almost whole.</item>
    /// <item>A post cap: chipped where struck; low on it, its base cut as a spar's section is.</item>
    /// <item>A rudder: the blade's boards broken off from where it struck to its after edge; at its stock with
    /// <see cref="WholeBlade"/> energy or more, the whole blade.</item>
    /// </list>
    /// </summary>
    public static class KwRigDamageTakers
    {
        /// <summary>The energy that cuts a square metre of spar section; a small iron ball's half-width at its 108 damage.</summary>
        public const float Wood = 1190f, Ball = 0.1f;

        public const float SailTakes = 2f, GrazeTakes = 5f, CapTakes = 20f, RudderTakes = 30f, WholeBlade = 60f;
        private static bool installed;

        public static void Install()
        {
            if (installed)
                return;
            installed = true;
            foreach (string spar in new[] { KwShipHitRouter.Mast, KwShipHitRouter.Yard, KwShipHitRouter.Bowsprit })
                KwShipHitRouter.Register(spar, Spar);
            KwShipHitRouter.Register(KwShipHitRouter.Sail, Sail);
            KwShipHitRouter.Register(KwShipHitRouter.Cap, Cap);
            KwShipHitRouter.Register(KwShipHitRouter.Rudder, Rudder);
        }

        /// <summary>On the owner: a hole a shot tore through a sail on the shooter's game (<see cref="KwSailDamagePierce"/>).</summary>
        public static void Pierced(KwRigDamage damage, ZPackage package)
        {
            int piece = package.ReadInt(), sail = package.ReadInt();
            Vector2 uv = new Vector2(package.ReadSingle(), package.ReadSingle());
            float radius = Mathf.Clamp(package.ReadSingle(), 0.05f, 0.5f);
            if (uv.x >= -0.01f && uv.x <= 1.01f && uv.y >= -0.01f && uv.y <= 1.01f)
                KwRigDamageCuts.Hole(damage, piece, sail, uv, radius);
        }

        /// <summary>How wide a shot's bite is, half of it: a small ball's at its damage, more for a heavier blow, less for a lighter.</summary>
        public static float Bite(float damage) => Ball * Mathf.Clamp(Mathf.Sqrt(Mathf.Max(damage, 1f) / 108f), 0.6f, 1.5f);

        private static float Spar(KwShipHitRouter ship, KwShipHit hit)
        {
            KwRigDamage? damage = ship.GetComponent<KwRigDamage>();
            KwRigPiece? piece = damage?.Parts.Get(hit.Piece);
            KwRigSpar? spar = piece?.Spar(hit.Kind == KwShipHitRouter.Yard ? hit.Part : KwRigSpar.Column);
            if (damage == null || piece == null || spar == null || damage.Condition.Fallen(piece.Index))
                return 0f;
            Vector3 kit = Local(ship, damage, piece, hit, out Vector3 way);
            if (spar.Value.Part < KwRigSpar.Gaff)
                return KwRigDamageCuts.Notch(damage, piece, spar.Value, kit, way, hit);
            // a gaff stands as its sail is set now; its notch is kept along it as set full
            int i = spar.Value.Part - KwRigSpar.Gaff;
            KwRigSpar now = KwRigPiece.GaffOf(piece.Sails[i], i, Mathf.Max(KwRigDamageHits.Set(ship.GetComponent<Ship>(), piece, i), 0.05f));
            return KwRigDamageCuts.Notch(damage, piece, now, kit, way, hit, spar.Value.Length / Mathf.Max(now.Length, 0.1f));
        }

        private static float Sail(KwShipHitRouter ship, KwShipHit hit)
        {
            KwRigDamage? damage = ship.GetComponent<KwRigDamage>();
            KwRigPiece? piece = damage?.Parts.Get(hit.Piece);
            if (damage == null || piece == null || hit.Part < 0 || hit.Part >= piece.Sails.Count || damage.Condition.Fallen(piece.Index))
                return 0f;
            Vector3 kit = Local(ship, damage, piece, hit, out _);
            float f = KwRigDamageHits.Set(ship.GetComponent<Ship>(), piece, hit.Part);
            if (KwSailDamageHoles.Uv(piece.Sails[hit.Part], kit, f, out Vector2 uv))
                KwRigDamageCuts.Hole(damage, piece.Index, hit.Part, uv, KwRigDamageCuts.HoleRadius(hit.Damage));
            return Mathf.Min(hit.Energy, SailTakes);
        }

        private static float Cap(KwShipHitRouter ship, KwShipHit hit)
        {
            KwRigDamage? damage = ship.GetComponent<KwRigDamage>();
            KwRigPiece? piece = damage?.Parts.Get(hit.Piece);
            if (damage == null || piece == null || piece.Kind != KwRigPiece.CapKind || damage.Condition.Fallen(piece.Index))
                return 0f;
            Vector3 kit = Local(ship, damage, piece, hit, out Vector3 way);
            float cut = KwRigDamageCuts.Chip(damage, piece, kit, way, hit);
            return Mathf.Min(hit.Energy, CapTakes + cut);
        }

        private static float Rudder(KwShipHitRouter ship, KwShipHit hit)
        {
            KwRigDamage? damage = ship.GetComponent<KwRigDamage>();
            KwRigPiece? piece = damage?.Parts.Get(hit.Piece);
            if (damage == null || piece == null || piece.Kind != KwRigPiece.RudderKind || piece.Tier == null)
                return Mathf.Min(hit.Energy, RudderTakes);
            Transform? blade = KwRigHitFrame.Of(hit.Collider);
            Vector3 kit = blade != null ? KwMesh.ToKit(Into(ship, blade, hit.Point, out _)) : KwRigBlade.Rest(piece, piece.ToKit(Into(ship, damage.Frame, hit.Point, out _)));
            KwRigDamageCuts.Chunk(damage, piece, kit, hit.Damage, hit.Energy >= WholeBlade);
            return Mathf.Min(hit.Energy, RudderTakes);
        }

        // Where a hit is in the piece's kit axes and its flight there: in the frame its collider names (a piece's, or a
        // swinging boom's), else the piece's place in the ship.
        private static Vector3 Local(KwShipHitRouter ship, KwRigDamage damage, KwRigPiece piece, KwShipHit hit, out Vector3 way)
        {
            Transform? frame = KwRigHitFrame.Of(hit.Collider);
            if (frame != null)
            {
                Vector3 at = Into(ship, frame, hit.Point, out Matrix4x4 into);
                way = KwMesh.ToKit(into.MultiplyVector(hit.Direction)).normalized;
                return KwMesh.ToKit(at);
            }
            Vector3 local = Into(ship, damage.Frame, hit.Point, out Matrix4x4 toShip);
            way = KwMesh.ToKit(piece.Back * toShip.MultiplyVector(hit.Direction)).normalized;
            return piece.ToKit(local);
        }

        /// <summary>
        /// A world point (as the ship's body stands, where the router found it) in a transform under the ship: through the
        /// body's pose, then the transform's place on the ship, so a drawn ship's interpolation does not move it.
        /// </summary>
        public static Vector3 Into(KwShipHitRouter ship, Transform frame, Vector3 point, out Matrix4x4 into) => Into(ship.transform, ship.Body, frame, point, out into);

        /// <summary>A world point as a ship's body stands in a transform under the ship (<see cref="Into(KwShipHitRouter, Transform, Vector3, out Matrix4x4)"/>).</summary>
        public static Vector3 Into(Transform ship, Rigidbody? body, Transform frame, Vector3 point, out Matrix4x4 into)
        {
            Matrix4x4 pose = body != null ? Matrix4x4.TRS(body.position, body.rotation, Vector3.one) : ship.localToWorldMatrix;
            into = frame.worldToLocalMatrix * ship.localToWorldMatrix * pose.inverse;
            return into.MultiplyPoint3x4(point);
        }
    }
}
