using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A keel block's cap: a notched cap on a crib of stacked timbers. Every second and a half, while the local player is
    /// near, it looks straight up for a keel piece within reach, turns to that keel's heading and raises or lowers its cap
    /// so the notch meets the keel's underside, the crib growing or shrinking
    /// under it; with no keel above it goes back to its own height and turn. Each player's game works it out from the
    /// same keels, so all see the same; nothing is sent. The crib is made here, one shared mesh for every block.
    /// </summary>
    public sealed class KwSupport : MonoBehaviour
    {
        private const float Every = 1.5f, Reach = 5f, Near = 60f;
        private static Mesh? cribMesh;
        private Quaternion home;
        private Vector3 foot;
        private Transform? crib;
        private float next;
        private ZNetView? view;

        private void Start()
        {
            home = transform.localRotation;
            foot = transform.localPosition - Vector3.up * KwDock.CapLift;
            next = Time.time + Random.value * Every;
            view = GetComponentInParent<ZNetView>();
            crib = MakeCrib();
            Lift(KwDock.CapLift);
        }

        private void Update()
        {
            if (Time.time < next)
                return;
            next = Time.time + Every;
            Guard.Run("Broadside keel block", Adapt);
        }

        private void Adapt()
        {
            Player player = Player.m_localPlayer;
            if (player == null || view == null || !view.IsValid() || (player.transform.position - transform.position).sqrMagnitude > Near * Near)
                return;
            Vector3 footWorld = transform.parent.TransformPoint(foot);
            if (!KeelAbove(footWorld, out RaycastHit hit, out Transform keel))
            {
                transform.localRotation = home;
                Lift(KwDock.CapLift);
                return;
            }
            transform.rotation = Quaternion.Euler(0f, keel.eulerAngles.y, 0f);
            Lift(Mathf.Clamp(hit.point.y - footWorld.y - (KwDock.CapHeight - KwDock.NotchDepth), 0.05f, Reach - 0.5f));
        }

        // The cap stands lift above the foot; the crib fills the gap beneath it.
        private void Lift(float lift)
        {
            transform.localPosition = foot + Vector3.up * lift;
            if (crib != null)
                crib.localScale = new Vector3(1f, lift, 1f);
        }

        private Transform MakeCrib()
        {
            GameObject go = new GameObject("crib") { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            if (cribMesh == null)
            {
                MeshBuilder builder = new MeshBuilder(Vector3.zero);
                KwDock.CribShape(builder);
                cribMesh = builder.ToMesh("KW_keelblock_crib");
            }
            go.AddComponent<MeshFilter>().sharedMesh = cribMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = GetComponent<MeshRenderer>()?.sharedMaterial;
            return go.transform;
        }

        // The nearest keel piece straight above the block's foot, and the point on its underside.
        private static bool KeelAbove(Vector3 from, out RaycastHit found, out Transform keel)
        {
            found = default;
            keel = null!;
            float best = float.MaxValue;
            foreach (RaycastHit hit in Physics.RaycastAll(from + Vector3.up * 0.05f, Vector3.up, Reach))
            {
                Piece? piece = hit.collider.GetComponentInParent<Piece>();
                if (piece == null || hit.distance >= best || !KwKeel.IsKeel(piece))
                    continue;
                best = hit.distance;
                found = hit;
                keel = piece.transform;
            }
            return best < float.MaxValue;
        }
    }
}
