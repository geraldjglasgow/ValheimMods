using System.Collections.Generic;
using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One weapon's moving parts in the world (<see cref="KwArmMover"/>, <see cref="KwArmCord"/>), found once by name under
    /// it (a launched ship's copy carries them too) and posed on every peer that draws, from the weapon's state in its ZDO
    /// (<see cref="WeaponState"/>: the age of its last shot, its reload), so every screen and a late joiner show the same:
    /// a shot's motion (every part's Fire track over <see cref="KwArmDef.FireTime"/>), then the reload stretched over its
    /// time, then held loaded (the Reload tracks' ends) or empty (the Fire tracks' ends); each cord laid between its points
    /// once the parts have moved. A still pose is drawn once. The gunner's grip points are found here too
    /// (<see cref="KwArmHands"/>).
    /// </summary>
    public sealed class KwArmMotion
    {
        private sealed class Moving
        {
            public KwArmMover Def = null!;
            public Transform Node = null!;
            public Vector3 Rest;
            public Renderer[] Looks = null!;
        }

        private readonly List<Moving> movers = new List<Moving>();
        private readonly List<(Transform Node, Transform From, Transform To)> cords = new List<(Transform Node, Transform From, Transform To)>();
        private int posed = -1;

        public Transform? GripLeft { get; private set; }
        public Transform? GripRight { get; private set; }
        public Transform? ReloadLeft { get; private set; }
        public Transform? ReloadRight { get; private set; }

        /// <summary>Whether the weapon has a grip for the gunner's hands at all.</summary>
        public bool HasGrips => GripLeft != null || GripRight != null || ReloadLeft != null || ReloadRight != null;

        /// <summary>A weapon's moving parts, cords and grips, or null for a weapon with none.</summary>
        public static KwArmMotion? Of(Transform root, KwArmDef def)
        {
            if (def.Movers.Count == 0 && def.Cords.Count == 0 && def.Hands == null && def.ReloadHands == null)
                return null;
            Dictionary<string, Transform> named = new Dictionary<string, Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                named[t.name] = t;
            KwArmMotion motion = new KwArmMotion();
            motion.Find(named, def);
            motion.GripLeft = Named(named, KwArmMotionBuild.GripLeft);
            motion.GripRight = Named(named, KwArmMotionBuild.GripRight);
            motion.ReloadLeft = Named(named, KwArmMotionBuild.ReloadLeft);
            motion.ReloadRight = Named(named, KwArmMotionBuild.ReloadRight);
            return motion;
        }

        /// <summary>The bench's pose, loaded, as the hammer's ghost and icon show the weapon.</summary>
        public static void Rest(Transform root, KwArmDef def) => Of(root, def)?.Apply(true, 1f);

        /// <summary>This frame's pose of the parts: the shot's motion, the reload under way, or held loaded or empty (drawn once).</summary>
        public void Pose(KwArm arm, in WeaponState s)
        {
            if (s.ShotAge < arm.Def.FireTime)
                Apply(false, s.ShotAge / arm.Def.FireTime);
            else if (s.Busy)
                Apply(true, KwArmReload.Progress(arm, s));
            else if ((s.Loaded ? 1 : 0) != posed)
                Apply(s.Loaded, 1f);
            posed = s.ShotAge < arm.Def.FireTime || s.Busy ? -1 : s.Loaded ? 1 : 0;
        }

        /// <summary>A kit turn (degrees about the kit's x, then y, then z, right-handed) as Unity's.</summary>
        public static Quaternion Turn(Vector3 kit) =>
            Quaternion.AngleAxis(-kit.z, Vector3.up) * Quaternion.AngleAxis(kit.y, Vector3.right) * Quaternion.AngleAxis(-kit.x, Vector3.forward);

        private void Find(Dictionary<string, Transform> named, KwArmDef def)
        {
            foreach (KwArmMover mover in def.Movers)
            {
                Transform? node = Named(named, mover.Node);
                KwArmMover? under = KwArmMotionBuild.Find(def, mover.Under);
                if (node != null)
                    movers.Add(new Moving { Def = mover, Node = node, Rest = KwMesh.ToUnity(mover.Pivot - (under?.Pivot ?? Vector3.zero)), Looks = Looks(node) });
            }
            foreach (KwArmCord cord in def.Cords)
            {
                Transform? node = Named(named, cord.Node), from = Named(named, cord.Node + "_a"), to = Named(named, cord.Node + "_b");
                if (node != null && from != null && to != null)
                    cords.Add((node, from, to));
            }
        }

        // The node's own meshes (its children named after it), not those of a part riding on it (a node, with no renderer of its own).
        private static Renderer[] Looks(Transform node)
        {
            List<Renderer> looks = new List<Renderer>();
            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                if (child.name.StartsWith(node.name + "_") && child.TryGetComponent(out Renderer look))
                    looks.Add(look);
            }
            return looks.ToArray();
        }

        private void Apply(bool reload, float t)
        {
            foreach (Moving m in movers)
                Place(m, (reload ? m.Def.Reload : m.Def.Fire).Sample(t));
            foreach ((Transform node, Transform from, Transform to) in cords)
                Lay(node, from, to);
        }

        private static void Place(Moving m, KwKey key)
        {
            m.Node.localPosition = m.Rest + KwMesh.ToUnity(key.Move);
            m.Node.localRotation = Turn(key.Turn);
            m.Node.localScale = new Vector3(key.Scale.y, key.Scale.z, key.Scale.x);
            foreach (Renderer look in m.Looks)
            {
                if (look.enabled != key.Shown)
                    look.enabled = key.Shown;
            }
        }

        // A cord (a unit line along its +z) from one point to the other, in its joint's space.
        private static void Lay(Transform cord, Transform from, Transform to)
        {
            Transform joint = cord.parent;
            Vector3 a = joint.InverseTransformPoint(from.position), run = joint.InverseTransformPoint(to.position) - a;
            cord.localPosition = a;
            cord.localRotation = run.sqrMagnitude > 1e-8f ? Quaternion.LookRotation(run) : Quaternion.identity;
            cord.localScale = new Vector3(1f, 1f, Mathf.Max(0.001f, run.magnitude));
        }

        private static Transform? Named(Dictionary<string, Transform> named, string name) => named.TryGetValue(name, out Transform t) ? t : null;
    }
}
