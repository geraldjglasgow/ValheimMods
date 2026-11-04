using System.Linq;
using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The harbour beacon's fire: the beacon is a copy of the game's standing brazier (<c>piece_brazierfloor01</c>), so it
    /// keeps the brazier's <see cref="Fireplace"/> (its fuel, the fuel it burns and how long, its hover, adding fuel by hand,
    /// going low in rain and wind), and the brazier's fire moves from the brazier's bowl to the model's <c>fire</c> point,
    /// as high above it as it stood above the bowl's ash bed (<c>ashlayer</c>): the objects the Fireplace switches (its
    /// fire, the low and high flames for rain, the smoke spawner) whole, and of everything else the brazier holds, the
    /// highest objects carrying a particle system, light, sound (ZSFX), effect area or burning area (Aoe), wherever they sit,
    /// so none of the brazier's bowl comes along. Its player base area stays at the foot. The model's <c>glow_embers</c> go
    /// under the fire the Fireplace switches, so they glow only while it burns. Nothing new is sent: the game's Fireplace
    /// keeps its fuel and state in the ZDO and shows the fire on every screen from it, as for the brazier.
    /// </summary>
    public static class KwDockFire
    {
        public const string PointName = "fire", GlowName = "glow_embers";
        private const string Ash = "ashlayer";
        // the brazier's ash bed under its pivot when the game's prefab has no ashlayer (measured 2026-11-02: -0.321)
        private const float Bed = -0.32f, AboveBasket = 0.2f;

        /// <summary>The brazier copy's fire moved to the model's fire point (<see cref="KwDockDef.Fit"/>); unlit (logged) without one.</summary>
        public static void Light(GameObject beacon, GameObject look)
        {
            Fireplace? fire = beacon.GetComponent<Fireplace>();
            Transform? point = GameMaterials.Find(look.transform, PointName);
            if (fire == null || point == null)
            {
                Log.Warn($"Broadside: {beacon.name} has no " + (fire == null ? "Fireplace from its base" : $"{PointName} point in its model") + "; it stands unlit.");
                return;
            }
            Transform? ash = beacon.transform.Find(Ash);
            float bed = ash != null ? ash.localPosition.y : Bed;
            // from the bowl's bed to the basket's fire point, in the world (the bench copy stands where its prefab does)
            Vector3 shift = point.position - beacon.transform.TransformPoint(new Vector3(0f, bed, 0f));
            GameObject? smoke = fire.m_smokeSpawner != null ? fire.m_smokeSpawner.gameObject : null;
            foreach (GameObject? part in new[] { fire.m_enabledObject, fire.m_enabledObjectLow, fire.m_enabledObjectHigh, smoke })
                Move(part != null ? part.transform : null, point, shift, look.transform);
            Keep(fire.m_playerBaseObject, look.transform);
            foreach (Transform part in beacon.transform.Cast<Transform>().ToArray())
                Effects(part, point, shift, look.transform);
            Embers(look.transform, fire);
            Settle(fire, beacon.transform.InverseTransformPoint(point.position).y, bed, look.transform);
            // its hover's first line: the beacon's name, not the brazier's
            fire.m_name = beacon.GetComponent<Piece>().m_name;
            Log.Info($"Broadside: {beacon.name} burns like the standing brazier: {(fire.m_fuelItem != null ? fire.m_fuelItem.name : "no fuel item")}, {fire.m_maxFuel} at most, {fire.m_secPerFuel} s each.");
        }

        // An object of the brazier's under the fire point, as high over it as it was over the bowl's bed; one already in the model stays.
        private static void Move(Transform? part, Transform point, Vector3 shift, Transform look)
        {
            if (part == null || part.IsChildOf(look))
                return;
            part.SetParent(point, true);
            part.position += shift;
        }

        // The player base area at the foot, where the brazier's stood (its pivot is the beacon's foot), unless the fire took it.
        private static void Keep(GameObject? area, Transform look)
        {
            if (area != null && !area.transform.IsChildOf(look))
                area.transform.SetParent(look, true);
        }

        // The highest objects under the part that carry a fire effect, each moved whole; the rest left to be stripped.
        private static void Effects(Transform part, Transform point, Vector3 shift, Transform look)
        {
            if (part.IsChildOf(look))
                return;
            if (Burns(part))
            {
                Move(part, point, shift, look);
                return;
            }
            foreach (Transform child in part.Cast<Transform>().ToArray())
                Effects(child, point, shift, look);
        }

        private static bool Burns(Transform part) =>
            part.GetComponent<ParticleSystem>() != null || part.GetComponent<Light>() != null || part.GetComponent<ZSFX>() != null ||
            part.GetComponent<EffectArea>() != null || part.GetComponent<Aoe>() != null;

        // The embers glow while the fire burns: under the object the Fireplace shows only then (always, without one).
        private static void Embers(Transform look, Fireplace fire)
        {
            Transform? glow = GameMaterials.Find(look, GlowName);
            GameObject? lit = fire.m_enabledObject;
            if (glow != null && lit != null && lit.transform.IsChildOf(look))
                glow.SetParent(lit.transform, true);
        }

        /// <summary>
        /// The Fireplace's checks from the beacon's foot made the fire's: the roof and rain check (a short ray up from the
        /// foot would hit the beacon's own post and put it out for good) just over the basket, buried only when the ground
        /// is over the fire, the Ashlands' ignite capsule raised with it; the brazier's fuel looks, gone with its bowl, cleared.
        /// </summary>
        private static void Settle(Fireplace fire, float height, float bed, Transform look)
        {
            fire.m_coverCheckOffset = height + AboveBasket;
            fire.m_checkTerrainOffset = height;
            Vector3 raise = Vector3.up * (height - bed);
            (fire.m_igniteCapsuleStart, fire.m_igniteCapsuleEnd) = (fire.m_igniteCapsuleStart + raise, fire.m_igniteCapsuleEnd + raise);
            fire.m_fullObject = Kept(fire.m_fullObject, look);
            fire.m_halfObject = Kept(fire.m_halfObject, look);
            fire.m_emptyObject = Kept(fire.m_emptyObject, look);
        }

        private static GameObject? Kept(GameObject? part, Transform look) => part != null && part.transform.IsChildOf(look) ? part : null;
    }
}
