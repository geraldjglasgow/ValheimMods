using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The stone at a charging Greywood Wand's tip (user, 2026-11-02: "when charging the rock gets bigger"), drawn with the
    /// little spell (<see cref="ChargeOrb"/>, from the state the caster shares, so on every machine near): a local copy of
    /// the stone the wand throws (the meshes and materials of <see cref="WandRock.Rock"/>'s visual, nothing else: no
    /// collider, no network view), there from the charge's start, growing with the charge from the size a tap throws to the
    /// size a full charge throws (<see cref="RockShot.Size"/>) and turning slowly; gone with the little spell when the
    /// rock flies or the charge is broken. None for any other wand, or when the rock could not be made.
    /// </summary>
    public static class ChargeStone
    {
        private const float Tumble = 45f;

        private static GameObject template;
        private static bool built;
        private static int greywood;

        /// <summary>A stone at <paramref name="tip"/> when the player holds the Greywood Wand, else null.</summary>
        public static GameObject Make(Player player, Transform tip)
        {
            GameObject made = Holds(player) ? Template() : null;
            if (made == null)
                return null;
            GameObject stone = Object.Instantiate(made, tip, false);
            stone.transform.localPosition = Vector3.zero;
            stone.SetActive(true);
            return stone;
        }

        /// <summary>The stone at the size this charge (0 to 1) would throw, turned a little further.</summary>
        public static void Size(GameObject stone, Transform tip, float charge)
        {
            float size = WandRock.Rock.transform.localScale.x * RockShot.Size(charge);
            stone.transform.localScale = Vector3.one * size / Mathf.Max(0.001f, tip.lossyScale.x);
            float turn = Tumble * Time.deltaTime;
            stone.transform.Rotate(turn, turn * 0.6f, 0f, Space.Self);
        }

        private static bool Holds(Player player)
        {
            if (greywood == 0)
                greywood = CasterGear.Greywood.Prefab.GetStableHashCode();
            return WandRock.Rock != null && player != null && player.m_visEquipment != null
                && player.m_visEquipment.m_rightItem == greywood;
        }

        private static GameObject Template()
        {
            if (built)
                return template;
            built = true;
            template = CasterSafe.Run("charge stone", Build);
            return template;
        }

        /// <summary>An inactive holder of the rock's visual meshes, placed as they sit on the rock, at the rock's scale 1.</summary>
        private static GameObject Build()
        {
            Transform rock = WandRock.Rock.transform;
            Projectile projectile = rock.GetComponent<Projectile>();
            GameObject visual = projectile != null && projectile.m_visual != null ? projectile.m_visual : rock.gameObject;
            var holder = new GameObject("EE_ChargeStone");
            holder.SetActive(false);
            Object.DontDestroyOnLoad(holder);
            foreach (MeshRenderer renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
                Part(holder.transform, rock, renderer);
            return holder;
        }

        private static void Part(Transform holder, Transform rock, MeshRenderer renderer)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return;
            Transform from = renderer.transform;
            var part = new GameObject("stone");
            part.transform.SetParent(holder, false);
            part.transform.localPosition = rock.InverseTransformPoint(from.position);
            part.transform.localRotation = Quaternion.Inverse(rock.rotation) * from.rotation;
            Vector3 scale = from.lossyScale, rockScale = rock.lossyScale;
            part.transform.localScale = new Vector3(scale.x / rockScale.x, scale.y / rockScale.y, scale.z / rockScale.z);
            part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            MeshRenderer drawn = part.AddComponent<MeshRenderer>();
            drawn.sharedMaterials = renderer.sharedMaterials;
            drawn.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
