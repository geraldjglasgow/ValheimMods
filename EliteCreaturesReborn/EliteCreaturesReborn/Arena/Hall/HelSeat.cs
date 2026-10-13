using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// Hel on her throne (features/arena.md section 1, "Hel"): the bundle's <c>ecr_hel</c> (the player's skeleton at
    /// 3.1 times its size on <c>Visual</c>) placed with her root on the throne's <c>seat_root</c>, facing its +Z. Not a
    /// creature: a plain scene object, still, on every peer. Her <c>Visual</c> gets the game player's own animator
    /// controller and avatar (from ZNetScene's <c>Player</c> prefab) with <c>attach_throne</c> and <c>onGround</c> set,
    /// so she plays the player's throne loop (Sit_Throne_Idle), as tried live through DevBridge on 2026-10-12. Her spear
    /// (<c>ecr_hel_spear</c>, at her hand's scale) leans from <c>spear_rest</c> on the dais to <c>spear_top</c> on the
    /// throne's arm. A box round her seated body (static_solid) is what the player looks at to hover her
    /// (<see cref="HelHover"/>). The animator, the materials, the hover and the spear are clients' only.
    /// </summary>
    internal static class HelSeat
    {
        private const string SeatRoot = "seat_root";
        private const string SpearRest = "spear_rest";
        private const string SpearTop = "spear_top";
        private const string Visual = "Visual";
        private const string SpearButt = "butt";
        private const string PlayerPrefab = "Player";

        // Her seated body about her root, from her seated mesh (ValheimAssets ecp_hel/out/poses/seated_throne.json):
        // 3.2 m between the throne's arms, from the seat's back to past her knees, from the dais to over her crown.
        private static readonly Vector3 BodyCentre = new Vector3(0f, 2.6f, 0.9f);
        private static readonly Vector3 BodySize = new Vector3(3f, 5.2f, 2.6f);

        private static readonly int AttachThrone = Animator.StringToHash("attach_throne");
        private static readonly int OnGround = Animator.StringToHash("onGround");

        public static void Seat(Transform hall, int layer, bool visuals)
        {
            GameObject? prefab = HallBundles.Prefab(HallBundles.Hel, HallBundles.Hel);
            if (prefab == null || !HallPoints.TryGet(SeatRoot, out Transform seat))
            {
                Log.Warn("Hel's hall: Hel or her seat is missing; the throne stands empty.");
                return;
            }
            GameObject hel = Object.Instantiate(prefab, seat.position, seat.rotation, hall);
            hel.name = "ecr_hel";
            HallBuilder.SetLayer(hel.transform, layer);
            BoxCollider body = hel.AddComponent<BoxCollider>();
            body.center = BodyCentre;
            body.size = BodySize;
            if (visuals)
            {
                HallMaterials.DressCreature(hel);
                Animate(hel.transform.Find(Visual));
                hel.AddComponent<HelHover>();
                Spear(hall, layer, hel.transform.Find(Visual));
            }
        }

        private static void Animate(Transform? visual)
        {
            Animator? animator = visual == null ? null : visual.GetComponent<Animator>();
            GameObject? player = ZNetScene.instance.GetPrefab(PlayerPrefab);
            Animator? source = player == null ? null : player.GetComponentInChildren<Animator>(true);
            if (animator == null || source == null)
            {
                Log.Warn("Hel's hall: Hel's or the player's animator is missing; she keeps her rest pose.");
                return;
            }
            animator.avatar = source.avatar;
            animator.runtimeAnimatorController = source.runtimeAnimatorController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.SetBool(AttachThrone, true);
            animator.SetBool(OnGround, true);
        }

        // The spear's butt point on spear_rest, its shaft (+Z) along to spear_top: 7.47 m at her hand's scale.
        private static void Spear(Transform hall, int layer, Transform? visual)
        {
            GameObject? prefab = HallBundles.Prefab(HallBundles.Hel, HallBundles.Spear);
            if (prefab == null || visual == null || !HallPoints.TryGet(SpearRest, out Transform rest)
                || !HallPoints.TryGet(SpearTop, out Transform top))
            {
                return;
            }
            GameObject spear = Object.Instantiate(prefab, hall, false);
            spear.name = HallBundles.Spear;
            spear.transform.localScale = visual.localScale;
            spear.transform.rotation = Quaternion.LookRotation(top.position - rest.position, Vector3.forward);
            Transform? butt = spear.transform.Find(SpearButt);
            Vector3 offset = butt == null ? Vector3.zero : butt.position - spear.transform.position;
            spear.transform.position = rest.position - offset;
            HallBuilder.SetLayer(spear.transform, layer);
            HallMaterials.DressCreature(spear);
        }
    }
}
