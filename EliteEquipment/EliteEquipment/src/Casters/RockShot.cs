using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One throw of the Greywood Wand's rock (<see cref="WandRock"/>), on the caster's machine, where the game fires a
    /// player's attack and sets up what it fired (<c>Projectile.Setup</c>, read 2026-11-02: it takes the velocity and the
    /// attack's hit, its push included). Before: the throw goes straight from the wand's tip at what the crosshair is on
    /// (<see cref="TipSpawn.AimPoint"/>) at the speed the game gave it, like a spell (no gravity, no arc, no spread). After:
    /// the charge at the trigger (<see cref="WindUp.Thrown"/>) makes it <see cref="Size"/> (1 to 2.5 times the stone's
    /// size; the stone at the tip while charging grows the same way, <see cref="ChargeStone"/>), its hit width with it, adds
    /// up to <see cref="Knock"/> push (the wand's own is 10; the Club's 30, the bronze mace's 80), and a rock charged for two
    /// pieces or more is armed to break (<see cref="RockBreak.Pieces"/>). The rock's pieces, set up while
    /// <see cref="RockBreak.Splitting"/>, are left as they are. <see cref="BoltSize"/> leaves rocks alone: their size is set here.
    /// </summary>
    public static class RockShot
    {
        private const float Smallest = 1f;
        private const float Largest = 2.5f;
        private const float Knock = 30f;

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
        private static class Throw
        {
            [HarmonyPrefix]
            private static void Prefix(Projectile __instance, Character owner, ref Vector3 velocity)
            {
                if (!Thrown(__instance, owner))
                    return;
                Vector3 aimed = velocity;
                CasterSafe.Run("rock aim", () => { aimed = Aim(__instance, (Player)owner, aimed); });
                velocity = aimed;
            }

            [HarmonyPostfix]
            private static void Postfix(Projectile __instance, Character owner)
            {
                if (Thrown(__instance, owner))
                    CasterSafe.Run("rock charge", () => Charge(__instance));
            }
        }

        /// <summary>A rock's size for a charge, 0 to 1, in times the stone's: 1 at a tap, 2.5 at full charge.</summary>
        public static float Size(float charge) => Mathf.Lerp(Smallest, Largest, Mathf.Clamp01(charge));

        /// <summary>A rock the local player's wand threw just now (not one of a rock's pieces).</summary>
        private static bool Thrown(Projectile projectile, Character owner) =>
            CastersSettings.On && !RockBreak.Splitting && owner != null && owner == Player.m_localPlayer
            && WandRock.Is(projectile) && WandRock.Rock != null && projectile.m_nview != null;

        /// <summary>Straight from where the rock is (the wand's tip) at the crosshair, at the speed it was given.</summary>
        private static Vector3 Aim(Projectile rock, Player player, Vector3 velocity)
        {
            Vector3 toAim = TipSpawn.AimPoint(player) - rock.transform.position;
            if (toAim.sqrMagnitude < 0.0001f || velocity.sqrMagnitude < 0.0001f)
                return velocity;
            Vector3 straight = toAim.normalized * velocity.magnitude;
            rock.transform.rotation = Quaternion.LookRotation(straight);
            return straight;
        }

        private static void Charge(Projectile rock)
        {
            float charge = Mathf.Clamp01(WindUp.Thrown);
            float size = Size(charge);
            rock.m_nview.SetLocalScale(WandRock.Rock.transform.localScale * size);
            rock.m_rayRadius = WandRock.HitWidth * size;
            rock.m_attackForce += Knock * charge;
            int pieces = RockBreak.Pieces(charge);
            if (pieces > 1)
                rock.gameObject.AddComponent<RockBreak>().Arm(rock, pieces);
        }
    }
}
