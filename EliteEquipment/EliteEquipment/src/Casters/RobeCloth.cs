using System;
using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A seer's robe skirt worn as real cloth (user, 2026-11-02: "the back foot glitches through the robe bottom, also when
    /// running the legs and feet completely glitch through the robe bottom"; no skin weights avoid both the crease at the
    /// knee and the shins through the back). On the robe's worn instance of every player who wears one (<see cref="Watch"/>,
    /// from <see cref="RobeBody"/>'s <c>VisEquipment.AttachArmor</c> postfix), on every client that draws them; never on an
    /// armour stand, a ragdoll or a machine without graphics. Four times a second it checks: while the casters' switch is
    /// on and the wearer is within <see cref="RobeClothBuild.Reach"/> of the camera, standing still and upright for half a
    /// second (the cloth keeps the lengths of the pose it is built in), the skirt is moved to a renderer of its own
    /// (<see cref="RobeClothMesh"/>) and the cloth is built on it once (<see cref="RobeClothBuild"/>) and then kept: past the reach or off screen the game's cloth rests and the plain skinned skirt is drawn; lying,
    /// riding, at a ship's mast or bench or swimming, the skirt is pinned to its skinned shape and starts again from
    /// it after. A robe mesh whose cloth will not build keeps its skinned skirt (logged once). Purely visual: nothing is sent
    /// or saved. Destroyed with the robe's instance, with its colliders on the bones.
    /// </summary>
    public sealed class RobeCloth : MonoBehaviour
    {
        private const float Every = 0.25f;
        private const float Still = 0.1f;
        private const int StillChecks = 2;

        private static readonly HashSet<Mesh> broken = new HashSet<Mesh>();
        private static readonly HashSet<Mesh> working = new HashSet<Mesh>();

        private readonly List<GameObject> legs = new List<GameObject>();
        private Player wearer;
        private VisEquipment vis;
        private Animator animator;
        private SkinnedMeshRenderer robe;
        private SkinnedMeshRenderer skirt;
        private Mesh source;
        private MagicaCloth cloth;
        private Action tick;
        private Action<MagicaCloth, bool> built;
        private float next;
        private Vector3 last;
        private int still;
        private bool pinned;
        private int wornLegs;

        /// <summary>Starts the cloth's watch on a player's robe instance <paramref name="worn"/>, for its renderer named <paramref name="mesh"/>.</summary>
        public static void Watch(VisEquipment vis, Transform worn, string mesh)
        {
            if (!vis.m_isPlayer || vis.m_isArmorStand || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            Player wearer = vis.GetComponent<Player>();
            Transform part = BundlePrefabs.GameMaterials.Find(worn, mesh);
            SkinnedMeshRenderer robe = part != null ? part.GetComponent<SkinnedMeshRenderer>() : null;
            if (wearer == null || robe == null || robe.sharedMesh == null || worn.GetComponent<RobeCloth>() != null)
                return;
            RobeCloth watch = worn.gameObject.AddComponent<RobeCloth>();
            watch.wearer = wearer;
            watch.vis = vis;
            watch.animator = wearer.m_animator;
            watch.robe = robe;
            watch.source = robe.sharedMesh;
            watch.last = wearer.transform.position;
        }

        private void Awake()
        {
            tick = Tick;
            built = Built;
        }

        private void Update()
        {
            if (Time.time < next)
                return;
            next = Time.time + Every;
            CasterSafe.Run("robe cloth", tick);
        }

        /// <summary>Other leggings put on under the robe drop its cloth, built again round them when the wearer next stands still.</summary>
        private void Tick()
        {
            if (!CastersSettings.On || wearer == null || robe == null)
                Drop();
            else if (cloth == null)
                Wait();
            else if (vis != null && vis.m_currentLegItemHash != wornLegs)
                Drop();
            else if (cloth.IsValid())
                Pin(RobeClothPose.Pinned(animator));
        }

        /// <summary>Builds the cloth once the wearer is near the camera and has stood still and upright for two checks.</summary>
        private void Wait()
        {
            if (animator == null)
                animator = wearer.m_animator;
            Vector3 at = wearer.transform.position;
            bool calm = (at - last).sqrMagnitude < Still * Still && RobeClothPose.Upright(animator);
            still = calm ? still + 1 : 0;
            last = at;
            if (still >= StillChecks && !broken.Contains(source) && Near(at) && robe.gameObject.activeInHierarchy)
                Build();
        }

        private static bool Near(Vector3 at)
        {
            Camera camera = Camera.main;
            return camera != null && (camera.transform.position - at).sqrMagnitude < RobeClothBuild.Reach * RobeClothBuild.Reach;
        }

        private void Build()
        {
            try
            {
                VertexAttribute[] attributes = RobeClothFixed.Of(robe, source);
                if (attributes == null)
                {
                    Broken("no part of it can move");
                    return;
                }
                RobeClothMesh parts = RobeClothMesh.Of(source, attributes);
                RobeClothWorn worn = RobeClothWorn.Of(robe, vis.m_bodyModel, wearer.transform.position.y);
                List<ColliderComponent> colliders = RobeClothColliders.Make(robe, parts.Skirt, parts.Attributes, worn, legs);
                wornLegs = vis.m_currentLegItemHash;
                skirt = parts.Wear(robe);
                RobeClothMesh.Detail(vis);
                Run(RobeClothBuild.Make(transform, skirt, parts, colliders));
            }
            catch (Exception e)
            {
                Broken(e.Message);
            }
        }

        private void Run(MagicaCloth made)
        {
            cloth = made;
            made.OnBuildComplete += built;
            pinned = false;
            if (!made.BuildAndRun())
                Broken(Why(made));
        }

        /// <summary>
        /// The end of the cloth's build, started on another thread by BuildAndRun. Only an error fails it: the cloth's own
        /// first frame (MagicaCloth's Start, its automatic build) also calls this with false, meaning only "not built
        /// automatically", while the build started here is still running.
        /// </summary>
        private void Built(MagicaCloth made, bool success)
        {
            if (made == null || made != cloth)
                return;
            if (success && working.Add(source))
                Plugin.Log.LogInfo($"EliteEquipment: {source.name}'s skirt moves as cloth");
            else if (!success && made.Process.Result.IsError())
                Broken(Why(made));
        }

        private static string Why(MagicaCloth made) =>
            $"the cloth simulation did not build ({made.Process.Result.GetResultString()})";

        /// <summary>Holds the skirt to its skinned shape (blend 0) or lets it move again from that shape.</summary>
        private void Pin(bool on)
        {
            if (on == pinned)
                return;
            pinned = on;
            if (!on)
                cloth.ResetCloth();
            cloth.SerializeData.blendWeight = on ? 0f : 1f;
            cloth.SetParameterChange();
        }

        private void Broken(string why)
        {
            if (broken.Add(source))
                Plugin.Log.LogWarning($"EliteEquipment: {source.name} keeps its skinned skirt without cloth: {why}");
            Drop();
        }

        /// <summary>The cloth, the skirt's own renderer and the colliders gone; the robe draws its whole skinned mesh again.</summary>
        private void Drop()
        {
            if (cloth != null)
                Destroy(cloth.gameObject);
            cloth = null;
            if (skirt != null || (robe != null && robe.sharedMesh != source))
                RobeClothMesh.TakeOff(robe, source, skirt, vis);
            skirt = null;
            foreach (GameObject leg in legs)
            {
                if (leg != null)
                    Destroy(leg);
            }
            legs.Clear();
            still = 0;
            pinned = false;
        }

        /// <summary>Going with the robe's instance: the game refreshes the wearer's level of detail itself after.</summary>
        private void OnDestroy()
        {
            vis = null;
            Drop();
        }
    }
}
