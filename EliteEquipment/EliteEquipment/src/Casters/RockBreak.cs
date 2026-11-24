using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A charged rock of the Greywood Wand breaking apart (user, 2026-11-02: "it should shoot the large rock for 5 meters,
    /// then break up, but keep a tight cone ... into like 15 pieces and keep going forward"), on the caster's machine, which
    /// owns the rock: once it has flown its break distance from the wand's tip (<see cref="Distance"/> m, give or take
    /// <see cref="DistanceSway"/>) it bursts (<see cref="RockBurst"/>, drawn on every machine near) into its pieces
    /// (<see cref="Pieces"/>: none below <see cref="Least"/> of a charge, else round(<see cref="Most"/> x the charge), at least
    /// 2: five a second of the three-second charge, user 2026-11-03: "a little more proportional to how long it took to
    /// charge the shot"), each a small copy of the rock thrown on from a point of its own on the rock at about its speed,
    /// straight, in a tight cone round its flight. Nothing in the break is laid out (user, the same day: "it also just
    /// shouldn't look like some uniform pre planned distribution"): each piece takes a random heading in the cone (spread
    /// evenly over its disc, out to <see cref="Cone"/> degrees), a random speed (<see cref="SpeedSway"/>) and a random size
    /// (<see cref="PieceSizes"/> times the stone's). Each carries the rock's hit (the wand's damage with its quality, the
    /// charge's +30%, the skill and the charge's push) times <see cref="AllLanded"/> by its share of the pieces' sizes: all of
    /// them landing is 1.5 times the rock, a bigger piece hitting harder. The game sets each up and hits with it as with any
    /// projectile (each that hits someone raises Elemental Magic); the rock itself goes. A rock that hits something before it
    /// breaks hits whole. Every player sees the pieces by the game's own sync, each with its trail (<see cref="RockTrail"/>).
    /// </summary>
    public sealed class RockBreak : MonoBehaviour
    {
        public const int Most = 15;
        private const float Least = 0.15f;
        private const float Distance = 5f;
        private const float DistanceSway = 0.5f;
        private const float Cone = 4.5f;
        private const float SpeedSway = 0.15f;
        private const float AllLanded = 1.5f;
        private const float StartSpread = 0.1f;
        private static readonly Vector2 PieceSizes = new Vector2(0.3f, 0.6f);

        private Projectile rock;
        private int pieces;
        private Vector3 start;
        private float distance;

        /// <summary>True while the pieces are set up, so <see cref="RockShot"/> leaves them as they are.</summary>
        public static bool Splitting { get; private set; }

        /// <summary>How many pieces a rock thrown at this charge (0 to 1) breaks into; 1 is a whole rock that never breaks.</summary>
        public static int Pieces(float charge) =>
            charge < Least ? 1 : Mathf.Max(2, Mathf.RoundToInt(Most * Mathf.Clamp01(charge)));

        public void Arm(Projectile thrown, int count)
        {
            rock = thrown;
            pieces = count;
            start = thrown.transform.position;
            distance = Distance + Random.Range(-DistanceSway, DistanceSway);
        }

        private void FixedUpdate()
        {
            if (rock == null || rock.m_didHit || rock.m_nview == null || !rock.m_nview.IsValid() || !rock.m_nview.IsOwner())
            {
                Destroy(this);
                return;
            }
            if ((rock.transform.position - start).sqrMagnitude < distance * distance)
                return;
            if (rock.m_vel.sqrMagnitude > 0.01f)
                CasterSafe.Run("rock break", Break);
            Destroy(this);
        }

        private void Break()
        {
            Vector3 point = rock.transform.position;
            RockBurst.Send(point, rock.m_vel.normalized);
            float[] sizes = Sizes();
            float total = 0f;
            foreach (float size in sizes)
                total += size;
            Splitting = true;
            try
            {
                for (int i = 0; i < pieces; i++)
                    Throw(From(point), Spread(rock.m_vel), PieceHit(sizes[i] / total), sizes[i]);
            }
            finally
            {
                Splitting = false;
            }
            ZNetScene.instance.Destroy(rock.gameObject);
        }

        private float[] Sizes()
        {
            var sizes = new float[pieces];
            for (int i = 0; i < pieces; i++)
                sizes[i] = Random.Range(PieceSizes.x, PieceSizes.y);
            return sizes;
        }

        /// <summary>The rock's hit, <paramref name="share"/> of it for one piece, with the rock's push.</summary>
        private HitData PieceHit(float share)
        {
            HitData hit = rock.m_originalHitData != null ? rock.m_originalHitData.Clone() : new HitData();
            hit.m_damage.Modify(AllLanded * share);
            hit.m_pushForce = rock.m_attackForce;
            return hit;
        }

        /// <summary>A point of the rock's own for a piece to leave from, within its size round where it broke.</summary>
        private Vector3 From(Vector3 point) =>
            point + Random.insideUnitSphere * (StartSpread * rock.transform.localScale.x / WandRock.Rock.transform.localScale.x);

        /// <summary>A piece's velocity: a random heading in the cone round the rock's flight, at a random share of its speed.</summary>
        private static Vector3 Spread(Vector3 velocity)
        {
            Vector3 forward = velocity.normalized;
            Vector3 side = Vector3.Cross(forward, Vector3.up);
            if (side.sqrMagnitude < 0.0001f)
                side = Vector3.right;
            float away = Cone * Mathf.Sqrt(Random.value);
            Quaternion tilt = Quaternion.AngleAxis(away, side.normalized);
            float speed = 1f + Random.Range(-SpeedSway, SpeedSway);
            return Quaternion.AngleAxis(Random.Range(0f, 360f), forward) * tilt * velocity * speed;
        }

        /// <summary>One piece from the break, its raycast starting there (not back at the caster, as an arrow's does).</summary>
        private void Throw(Vector3 point, Vector3 velocity, HitData hit, float size)
        {
            GameObject made = Instantiate(WandRock.Rock, point, Quaternion.LookRotation(velocity));
            Projectile piece = made.GetComponent<Projectile>();
            piece.Setup(rock.m_owner, velocity, rock.m_hitNoise, hit, rock.m_weapon, rock.m_ammo);
            piece.m_haveStartPoint = false;
            piece.m_startPoint = point;
            if (piece.m_nview != null)
                piece.m_nview.SetLocalScale(WandRock.Rock.transform.localScale * size);
            piece.m_rayRadius = WandRock.HitWidth;
        }
    }
}
