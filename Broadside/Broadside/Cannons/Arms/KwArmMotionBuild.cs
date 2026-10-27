using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A weapon's moving parts on the bench (<see cref="KwArmBuild"/>, after the joints' own parts): each moving part a node
    /// at its pivot, <c>mv_&lt;name&gt;</c>, on its joint or on the part it rides on, with one mesh a look or raw material
    /// under it (<c>mv_&lt;name&gt;_&lt;look&gt;</c>, as the joints' own meshes are named, so a weapon's <see cref="KwArmDef.Finish"/> finds them too; no collider), drawn in the joint's kit axes from its pivot; each cord a
    /// node <c>cd_&lt;name&gt;</c> under its joint with a unit line of its look, and its two points (<c>cd_&lt;name&gt;_a</c>,
    /// <c>_b</c>) on whatever holds them; the gunner's grip points (<c>grip_l</c>, <c>grip_r</c>, <c>grip_reload_l</c>,
    /// <c>grip_reload_r</c>). Then all posed loaded (<see cref="KwArmMotion.Rest"/>). A part or point whose holder is
    /// missing is left out, logged.
    /// </summary>
    public static class KwArmMotionBuild
    {
        public const string GripLeft = "grip_l", GripRight = "grip_r", ReloadLeft = "grip_reload_l", ReloadRight = "grip_reload_r";

        public static void Add(Transform root, KwArmDef arm)
        {
            foreach (KwArmMover mover in arm.Movers)
                Mover(root, arm, mover);
            foreach (KwArmCord cord in arm.Cords)
                Cord(root, arm, cord);
            Grip(root, arm, arm.Hands, GripLeft, GripRight);
            Grip(root, arm, arm.ReloadHands, ReloadLeft, ReloadRight);
            KwArmMotion.Rest(root, arm);
        }

        /// <summary>A joint's transform under a weapon's root (the root itself for the foot); null when it has none.</summary>
        public static Transform? Joint(Transform root, KwJoint joint)
        {
            if (joint == KwJoint.Foot)
                return root;
            Transform? yaw = root.Find(KwArm.YawName);
            return joint == KwJoint.Yaw || yaw == null ? yaw : yaw.Find(KwArm.PitchName);
        }

        /// <summary>A weapon's moving part by its name; null for none (or an empty name).</summary>
        public static KwArmMover? Find(KwArmDef arm, string name)
        {
            if (name.Length == 0)
                return null;
            foreach (KwArmMover mover in arm.Movers)
            {
                if (mover.Name == name)
                    return mover;
            }
            return null;
        }

        private static void Mover(Transform root, KwArmDef arm, KwArmMover mover)
        {
            KwArmMover? under = Find(arm, mover.Under);
            Transform? holder = under != null ? Descendant(root, under.Node) : Joint(root, mover.Joint);
            if (holder == null)
            {
                Log.Warn($"Broadside cannons: {arm.Label}'s moving part {mover.Name} has nothing to ride on; it is left out.");
                return;
            }
            Transform node = Node(holder, mover.Node, mover.Pivot - (under?.Pivot ?? Vector3.zero));
            foreach (IGrouping<KwLook, (KwLook Look, System.Action<MeshBuilder> Shape)> group in mover.Parts.GroupBy(p => p.Look))
                Mesh(node, mover.Pivot, group.Key, group.Select(p => p.Shape));
        }

        private static void Cord(Transform root, KwArmDef arm, KwArmCord cord)
        {
            Transform? joint = Joint(root, cord.From.Joint);
            Transform? from = Point(root, arm, cord.From, cord.Node + "_a"), to = Point(root, arm, cord.To, cord.Node + "_b");
            if (joint == null || from == null || to == null)
            {
                Log.Warn($"Broadside cannons: {arm.Label}'s cord {cord.Name} has a point with nothing to hold it; it is left out.");
                return;
            }
            Transform node = Node(joint, cord.Node, Vector3.zero);
            float radius = cord.Radius;
            Mesh(node, Vector3.zero, cord.Look, new System.Action<MeshBuilder>[] { b => Solid.Log(b, Vector3.zero, new Vector3(1f, 0f, 0f), radius, 6) });
        }

        private static void Grip(Transform root, KwArmDef arm, KwArmGrip? grip, string left, string right)
        {
            if (grip == null)
                return;
            if (Point(root, arm, grip.Left, left) == null || Point(root, arm, grip.Right, right) == null)
                Log.Warn($"Broadside cannons: {arm.Label}'s grip has a point with nothing to hold it.");
        }

        // A point as a node on its holder: its moving part's node, or its joint.
        private static Transform? Point(Transform root, KwArmDef arm, KwArmPoint point, string name)
        {
            KwArmMover? mover = Find(arm, point.Mover);
            Transform? holder = mover != null ? Descendant(root, mover.Node) : Joint(root, point.Joint);
            return holder != null ? Node(holder, name, point.At - (mover?.Pivot ?? Vector3.zero)) : null;
        }

        // One mesh of a look under a node, drawn in kit axes from the node's pivot.
        private static void Mesh(Transform node, Vector3 pivot, KwLook look, IEnumerable<System.Action<MeshBuilder>> shapes)
        {
            GameObject part = new GameObject(node.name + "_" + look.ToString().ToLowerInvariant()) { layer = node.gameObject.layer };
            part.transform.SetParent(node, false);
            MeshBuilder builder = new MeshBuilder(pivot);
            foreach (System.Action<MeshBuilder> shape in shapes)
                shape(builder);
            Mesh mesh = builder.ToMesh(node.root.name + "_" + part.name);
            KwRudderWindows.Fit(mesh, look);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            Material? material = KwTierMaterials.Of(look);
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
        }

        private static Transform Node(Transform parent, string name, Vector3 kit)
        {
            Transform node = new GameObject(name) { layer = parent.gameObject.layer }.transform;
            node.SetParent(parent, false);
            node.localPosition = KwMesh.ToUnity(kit);
            return node;
        }

        private static Transform? Descendant(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
            return null;
        }
    }
}
