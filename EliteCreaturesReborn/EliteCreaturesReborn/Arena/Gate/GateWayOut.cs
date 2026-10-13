using EliteCreaturesReborn.Arena.Hall;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// The way out of Hel's hall (features/arena.md section 2): the wall closing the hall's gate passage, behind the
    /// <c>arrive</c> point, becomes Helgrind's other side - its swirl and light on the wall, facing into the passage, and a
    /// trigger before it (<see cref="WayOutTrigger"/>) that jumps the player back to where they came in. Built with the
    /// hall, as a plain local object under it (no ZNetView), by one call from the hall's builder; on clients only, since
    /// it moves and shows nothing on a dedicated server. The walls bundle has no point on the back wall: its face is
    /// worked out from <c>arrive</c> ((0, 0, 27.9) facing the doors; the passage's back wall at r 29.5, the walls' BRIEF).
    /// The swirl stands as in the gate, an arch-shaped panel 2.44 x 3.22 m with its sill on the passage floor: its centre
    /// and its light's offset are read from the gate's model (<c>portal_center</c> 1.73 m up, <c>light</c> 0.2475 m in
    /// front of it), and the trigger is the gate's opening box (<see cref="GateModel.Opening"/>) on the same centre.
    /// </summary>
    internal static class GateWayOut
    {
        private const string Arrive = "arrive";
        private const string TriggerLayer = "character_trigger";

        /// <summary>From <c>arrive</c> back to the wall's face (29.5 - 27.9).</summary>
        private const float BackWall = 1.6f;

        /// <summary>The swirl's distance off the wall.</summary>
        private const float OffWall = 0.15f;

        /// <summary>The trigger's depth out from the wall: a player standing on <c>arrive</c> (0.9 m in front of it) is
        /// outside, one who walks up to the wall is in.</summary>
        private const float TriggerDepth = 0.7f;

        public static void Build(Transform hall)
        {
            if (HallBuilder.NoScreen || !HallPoints.TryGet(Arrive, out Transform arrive))
            {
                return;
            }
            Vector3 center = GateModel.PointIn(GatePrefab.Prefab, GateModel.Center, GateModel.CenterAsBuilt);
            Vector3 light = GateModel.PointIn(GatePrefab.Prefab, GateModel.LightPoint, GateModel.LightAsBuilt);
            var way = new GameObject("ecr_helgrind_way_out");
            way.transform.SetParent(hall, false);
            way.transform.SetPositionAndRotation(arrive.position - arrive.forward * BackWall, arrive.rotation);
            Trigger(way.transform, center.y);
            var anchor = new GameObject(GateModel.Center);
            anchor.transform.SetParent(way.transform, false);
            anchor.transform.localPosition = new Vector3(0f, center.y, OffWall);
            GateSwirl.Swirl(anchor.transform);
            Vector3 lit = new Vector3(0f, light.y, OffWall + light.z - center.z);
            GateSwirl.Light(way.transform, way.transform.TransformPoint(lit));
        }

        private static void Trigger(Transform way, float height)
        {
            var box = new GameObject("trigger");
            box.layer = LayerMask.NameToLayer(TriggerLayer);
            box.transform.SetParent(way, false);
            BoxCollider collider = box.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(GateModel.Opening.x, GateModel.Opening.y, TriggerDepth);
            collider.center = new Vector3(0f, height, TriggerDepth / 2f);
            box.AddComponent<WayOutTrigger>();
        }
    }
}
