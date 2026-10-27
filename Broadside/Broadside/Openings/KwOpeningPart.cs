using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A part's openings on the dock (planking or a cabin, <see cref="KwGenerated"/>), made on every peer as the part builds:
    /// under a child <see cref="Holder"/>, the frames (drawn only where there is a screen, <see cref="KwShipBody.Drawn"/>)
    /// and each cover's or door's leaves on their hinges (<see cref="KwOpeningLeaf"/>, with a box to bump into while shut).
    /// Whether an opening is open lives in the part's ZDO under its number (<see cref="Key"/>: 0 shut, 1 or -1 open one way
    /// or the other), written only by the ZDO's owner: E asks the owner by RPC (<see cref="Rpc"/>, claiming the part first
    /// when nobody owns it, as a hatch does) and every peer swings the leaves toward the ZDO's state. A part built again (an
    /// edit, an opening cut or filled) is a new part: its states come with its other ZDO values, an opening's under its
    /// number.
    /// </summary>
    public sealed class KwOpeningPart : MonoBehaviour
    {
        public const string Rpc = "KW_OpeningUse", Holder = "kw_openings";

        private ZNetView? view;
        private bool registered;

        /// <summary>The ZDO key an opening's state is kept under on its part: "kw_open3".</summary>
        public static int Key(int id) => ("kw_open" + id).GetStableHashCode();

        /// <summary>The part's openings made again from its saved text (nothing for a part with none).</summary>
        public static void Fit(GameObject part, string tool, string saved, KwBiomeTier tier)
        {
            Transform? old = part.transform.Find(Holder);
            if (old != null)
                Destroy(old.gameObject);
            KwSeatAt seat = KwSeatAt.World(part.transform);
            System.Collections.Generic.List<KwOpeningWall> walls = KwOpeningSurfaces.Of(tool, saved, seat);
            if (walls.Count == 0)
                return;
            KwOpeningPart? owner = part.GetComponent<KwOpeningPart>();
            if (owner == null)
                owner = part.AddComponent<KwOpeningPart>();
            owner.Register();
            Transform holder = new GameObject(Holder) { layer = part.layer }.transform;
            holder.SetParent(part.transform, false);
            if (KwShipBody.Drawn)
                Frames(holder, tool, saved, tier, seat);
            foreach (KwLeafPlan leaf in KwOpeningLeaves.Of(walls))
                KwOpeningLeaf.Hang(holder, leaf, owner, tier);
        }

        // The frames' wood and iron, each its own drawn object (planking seated where it stands, as its mesh is).
        private static void Frames(Transform holder, string tool, string saved, KwBiomeTier tier, KwSeatAt seat)
        {
            foreach ((bool iron, Mesh mesh) in KwOpeningLooks.Of(tool, saved, false, seat))
                Drawn(holder, iron ? "iron" : "frames", mesh, iron ? KwOpeningLooks.Iron : KwOpeningLooks.Wood(tier));
        }

        /// <summary>A drawn child holding a mesh of its own (let go with it) in a material.</summary>
        public static GameObject Drawn(Transform holder, string name, Mesh mesh, Material? material)
        {
            GameObject look = new GameObject(name) { layer = holder.gameObject.layer };
            look.transform.SetParent(holder, false);
            look.AddComponent<MeshFilter>().sharedMesh = mesh;
            look.AddComponent<MeshRenderer>().sharedMaterial = material;
            look.AddComponent<KwMeshOwner>();
            return look;
        }

        private void Register()
        {
            view ??= GetComponent<ZNetView>();
            if (registered || view == null || view.GetZDO() == null)
                return;
            view.Register<int, int>(Rpc, (sender, id, way) => Guard.Run("Broadside opening", () => Use(id, way)));
            registered = true;
        }

        /// <summary>An opening's state: 0 shut, 1 or -1 open.</summary>
        public int State(int id) => view != null && view.IsValid() ? view.GetZDO().GetInt(Key(id)) : 0;

        /// <summary>Asks the part's owner to open the opening (the way given) or shut it.</summary>
        public void Ask(int id, int way)
        {
            if (view == null || !view.IsValid())
                return;
            if (!view.HasOwner())
                view.ClaimOwnership();
            view.InvokeRPC(Rpc, id, way);
        }

        // On the owner: a shut opening opens the way asked, an open one shuts.
        private void Use(int id, int way)
        {
            if (view != null && view.IsValid() && view.IsOwner())
                view.GetZDO().Set(Key(id), State(id) != 0 ? 0 : (way < 0 ? -1 : 1));
        }
    }
}
