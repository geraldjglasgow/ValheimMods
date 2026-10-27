using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a saved ship built from the hammer becomes: a copy of the game's karve (<see cref="Name"/>) that keeps what
    /// makes it a ship (its Ship and Rigidbody, the buoyancy box, the onboard trigger, the helm, the ladder to climb aboard
    /// from the water, its wake, sounds and wear) and loses what makes it a karve (its hull, mast, sail, ropes, rudder,
    /// seats, colliders and its chest: a Broadside ship has no hold). <see cref="KwShipBody"/> builds the rest from the
    /// blueprint its ZDO carries. Never in the hammer itself: a saved ship's entry makes it (<see cref="KwShipLaunch"/>).
    /// Its piece costs nothing: the hammer took the ship's cost (<see cref="KwBpCost"/>), and gives it back in floating
    /// crates when the ship is broken up with the hammer (<see cref="KwShipBreak"/>); destroyed, it plays no karve's wreck.
    /// </summary>
    public static class KwShipPrefab
    {
        public const string Name = "KW_ship", Visual = "ship/visual", Colliders = "ship/colliders", Fittings = "ship/fittings";

        // The karve's splash triggers are its own hull's places: on a ship of another size they splash all the time.
        private static readonly string[] Gone =
        {
            "ship/smallboat_colliders", "ship/mast", "ship/visual_new", "ship/visual_worn", "ship/visual_broken", "ship/ropes",
            "interactive/sit_box_back", "interactive/sit_box_fr", "interactive/sit_box_fl", "interactive/sitboard_front",
            "interactive/sitboard_back", "interactive/mast", "interactive/controlls/rudder", "piece_chest/visual",
            "SurfaceEffects/Splash_effects",
        };

        public static GameObject? Make(ZNetScene scene)
        {
            GameObject? karve = scene.GetPrefab("Karve");
            if (karve == null || karve.GetComponent<Ship>() == null)
                return null;
            GameObject copy = PrefabBench.Copy(karve, Name);
            foreach (string path in Gone)
            {
                Transform? part = copy.transform.Find(path);
                if (part != null)
                    Object.DestroyImmediate(part.gameObject);
            }
            Transform ship = copy.transform.Find("ship");
            GameObject visual = Holder(ship, "visual"), mast = Holder(ship, "mast");
            Holder(ship, "colliders").layer = LayerMask.NameToLayer("vehicle");
            Holder(ship, "fittings");
            Configure(copy, visual, mast);
            Quiet(copy.transform.Find("SurfaceEffects/WaterSurface"));
            Quiet(copy.transform.Find("SurfaceEffects/SpeedWake"));
            copy.AddComponent<KwShipBody>();
            copy.AddComponent<KwShipRiders>();
            return copy;
        }

        // The karve's water-surface blob and wake are sized and placed for its own hull: kept (the game's ship effects
        // switch them on and off and need them), but drawn and emitted no more.
        private static void Quiet(Transform? effects)
        {
            if (effects == null)
                return;
            foreach (Renderer renderer in effects.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
            foreach (ParticleSystem particles in effects.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.enabled = false;
            }
        }

        private static GameObject Holder(Transform parent, string name)
        {
            GameObject holder = new GameObject(name) { layer = parent.gameObject.layer };
            holder.transform.SetParent(parent, false);
            return holder;
        }

        // The ship's own references pointed at what is left: no rudder of the karve's (its sail cloth went with its mast), the mast an empty holder.
        private static void Configure(GameObject copy, GameObject visual, GameObject mast)
        {
            Ship ship = copy.GetComponent<Ship>();
            ship.m_rudderObject = null;
            ship.m_mastObject = mast;
            ship.m_sailObject = null;
            ship.m_rudderRotationMax = KwRudder.MaxAngle;
            // no hits from the water: this ship rides the waves on its own float, and the karve's splash triggers sat
            // at its own hull's places (removed above), splashing all the time on ours
            ship.m_minWaterImpactForce = float.MaxValue;
            ship.m_waterImpactDamage = 0f;
            WearNTear? wear = copy.GetComponent<WearNTear>();
            if (wear != null)
            {
                wear.m_new = wear.m_worn = wear.m_broken = visual;
                (wear.m_wet, wear.m_fragmentRoots) = (null, null);
                wear.m_destroyedEffect = KwShipBreakLook.Kept(wear.m_destroyedEffect);
            }
            Piece piece = copy.GetComponent<Piece>();
            piece.m_name = "Broadside ship";
            piece.m_resources = new Piece.Requirement[0];
            Transform? chest = copy.transform.Find("piece_chest");
            if (chest != null)
                chest.gameObject.SetActive(false);
        }
    }
}
