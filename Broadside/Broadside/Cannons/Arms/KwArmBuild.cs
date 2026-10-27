using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon's piece from its definition: a Broadside piece (<see cref="KwDef"/>) whose model is the two ringbolts
    /// its breeching is made fast to (<see cref="KwShipCarriage.Model"/>), with no snap point. On the bench, its joints as
    /// children ("yaw" at the yaw pivot, "pitch" under it at the pitch pivot, the "muzzle" at the barrel's end, the
    /// gunner's stand point "attach" on the yaw part, behind the breech), each joint's parts drawn as one mesh a look in
    /// that look's game material, none with a collider (<see cref="KwCannonBody"/> makes them on the piece, so the
    /// carriage can recoil and be levered round without them), its moving parts, cords and grips
    /// (<see cref="KwArmMotionBuild"/>), the <see cref="KwArm"/> that mans, aims and fires it, and its trucks, quoin,
    /// breeching, colliders and rear (<see cref="KwCannonBody"/>).
    /// </summary>
    public static class KwArmBuild
    {
        public static KwDef Def(KwArmDef arm) =>
            new KwDef(arm.Prefab, arm.Label, "arm") { Shape = b => KwShipCarriage.Model(b, arm.Ship), Wood = true, Setup = prefab => Fit(prefab, arm), Description = arm.Description };

        private static void Fit(GameObject prefab, KwArmDef arm)
        {
            Transform root = prefab.transform;
            KwArmMeshes.Wear(root.Find("model"), arm.Ship.Metal);
            Transform yaw = Joint(root, KwArm.YawName, arm.YawPivot);
            Transform pitch = Joint(yaw, KwArm.PitchName, arm.PitchPivot);
            Joint(pitch, KwArm.MuzzleName, arm.Muzzle);
            Joint(yaw, KwArm.AttachName, arm.Stand - arm.YawPivot);
            Draw(root, arm, KwJoint.Foot);
            Draw(yaw, arm, KwJoint.Yaw);
            Draw(pitch, arm, KwJoint.Pitch);
            KwArmMotionBuild.Add(root, arm);
            arm.Finish?.Invoke(prefab);
            prefab.AddComponent<KwArm>().Id = arm.Id;
            KwCannonBody.Fit(prefab, arm, arm.Ship);
            Describe(prefab, arm);
        }

        private static Transform Joint(Transform parent, string name, Vector3 kit)
        {
            Transform joint = new GameObject(name) { layer = parent.gameObject.layer }.transform;
            joint.SetParent(parent, false);
            joint.localPosition = KwMesh.ToUnity(kit);
            return joint;
        }

        // One mesh a look for a joint's parts, under the joint.
        private static void Draw(Transform joint, KwArmDef arm, KwJoint which)
        {
            foreach (IGrouping<KwLook, KwArmPart> group in arm.Parts.Where(p => p.Joint == which).GroupBy(p => p.Look))
            {
                string name = which.ToString().ToLowerInvariant() + "_" + group.Key.ToString().ToLowerInvariant();
                KwArmMeshes.Child(joint, name, b => Shapes(b, group), group.Key);
            }
        }

        private static void Shapes(MeshBuilder b, IGrouping<KwLook, KwArmPart> group)
        {
            foreach (KwArmPart part in group)
                part.Shape(b);
        }

        private static void Describe(GameObject prefab, KwArmDef arm)
        {
            WearNTear? wear = prefab.GetComponent<WearNTear>();
            if (wear != null)
                wear.m_health = arm.Health;
            string ammo = arm.Primary.Ammo.Count == 0 ? "" : $" Fires {string.Join(" or ", arm.Primary.Ammo.Select(KwArmory.Label))}.";
            const string where = "Built on a launched ship's deck on its four trucks; E mans it, Shift+E hauls it about the deck.";
            prefab.GetComponent<Piece>().m_description = $"{arm.Description} {where}{ammo}".Trim();
        }
    }
}
