using System;
using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A generated hull part in the world (planking, a deck or a railing that a tool built between two ribs, or a rib and
    /// a keel end): its <see cref="KwSpan"/> is read from the ZDO and its mesh built on every client, collider included,
    /// planking with its planks shifted by where it stands; planking, a deck, a keel and a rib wear their saved biome tier
    /// (<see cref="KwBiomeDress"/>; railings the plank wood). Nothing is sent: the span and the tier travel with the ZDO.
    /// A part whose ZDO has no span stays empty. A deck is laid as its tier's boards (<see cref="KwDeckBoards"/>; its collider
    /// one whole slab, as before boards), leaves out the openings of the hatches
    /// on it (<see cref="KwDeckHoles"/>) and builds again, on the next frame, when one comes or goes (<see cref="Recut"/>);
    /// a ladder builds from its saved length (<see cref="KwLadder"/>); a cabin (or an old castle) from its saved shape, in
    /// its tier's looks (<see cref="KwHouses"/>); a made keel or rib from its saved sizes, a keel
    /// with its snap points too (<see cref="KwShapeTool"/>). A part ending at a keel end meets the keel as it stands
    /// (<see cref="KwSpanEnds"/>); planking, decks, railings and ribs are seated on the keels loaded (<see cref="KwSeat"/>),
    /// and all of them build again when a made keel near them loads (<see cref="Refit"/>). Planking lays its
    /// hold's floor with it (<see cref="KwSoleDock"/>) and builds again when a deck near it comes or goes (<see cref="Refloor"/>),
    /// since the floor gives way to a low deck. Planking draws its painted bands and a railing lays its shield hooks as they
    /// build (<see cref="KwPartLooks"/>).
    /// </summary>
    public sealed class KwGenerated : MonoBehaviour
    {
        public const string Key = "kw_span";
        public string Tool = "planking";

        private static readonly List<KwGenerated> Hulls = new List<KwGenerated>();
        private static readonly List<KwGenerated> Keels = new List<KwGenerated>();

        /// <summary>The made keels loaded on this peer, which the hull parts round them are seated on (<see cref="KwKeelLines.World"/>).</summary>
        public static IReadOnlyList<KwGenerated> LoadedKeels => Keels;
        private Mesh? mesh, solid;

        // A deck low enough over the planking under it that the holds' floors there give way to it (KwSoleDock.Low).
        private bool lowDeck;

        // Built once; the component then sleeps until a hatch, a keel or a deck near it wakes it to build again. A low deck
        // coming (or going) wakes the planking round it, whose floors give way to it.
        private void Start()
        {
            Guard.Run("Broadside generated part", Build);
            if (Tool == "deck" || Tool == "planking" || Tool == "rail" || Tool == KwShapeTool.Rib)
                Hulls.Add(this);
            if (Tool == KwShapeTool.Keel)
            {
                Keels.Add(this);
                Refit(transform.position, 20f);
            }
            lowDeck = Tool == "deck" && KwSoleDock.Low(GetComponent<ZNetView>()?.GetZDO()?.GetString(Key));
            if (lowDeck)
                Refloor(transform.position, KwSoleDock.Reach);
            enabled = false;
        }

        private void Update()
        {
            enabled = false;
            Guard.Run("Broadside generated part", Build);
        }

        /// <summary>Every deck loaded within reach of a point builds again on the next frame: a hatch came or went there.</summary>
        public static void Recut(Vector3 point, float reach) => Wake(point, reach, "deck");

        /// <summary>Every hull part loaded within reach of a point builds again on the next frame: a keel there came.</summary>
        public static void Refit(Vector3 point, float reach) => Wake(point, reach, null);

        /// <summary>Every planking part loaded within reach of a point builds again on the next frame: a deck there came or went.</summary>
        public static void Refloor(Vector3 point, float reach) => Wake(point, reach, "planking");

        // The hull parts within reach (only one tool's, or every one's) wake to build again.
        private static void Wake(Vector3 point, float reach, string? only)
        {
            foreach (KwGenerated part in Hulls)
            {
                if (part != null && (only == null || part.Tool == only) && (part.transform.position - point).sqrMagnitude < reach * reach)
                    part.enabled = true;
            }
        }

        private void Build()
        {
            ZNetView view = GetComponent<ZNetView>();
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo != null && KwHouses.Build(transform, Tool, zdo))
                return;
            KwDef? def = null;
            Mesh? made = zdo == null ? null : MeshFor(Tool, zdo.GetString(Key), transform, out def, null, KwBiomeTiers.Of(zdo));
            if (made == null)
                return;
            if (Tool == KwShapeTool.Keel && def != null)
                Snaps(def);
            if (Tool == KwSailTool.Tool)
                KwSailDress.Dress(transform, zdo!.GetString(Key));
            else
                KwSailRig.Rerig(transform.position, 30f);
            Wear(made, def?.Boards != null ? KwMesh.Build(def, true) : null);
            if (Tool == "planking" && def != null && KwSpan.TryParse(zdo!.GetString(Key), out KwSpan span))
                KwSoleDock.Lay(transform, new KwSeatAt(KwKeelLines.World(), transform.position, transform.forward).Seated(span), def.Stations, KwBiomeTiers.Of(zdo));
            KwBiomeDress.Apply(gameObject, zdo!, Tool);
            KwPartLooks.Built(gameObject, Tool, zdo!.GetString(Key), def, KwBiomeTiers.Of(zdo));
        }

        // The model takes the new mesh and its collider the same, or a deck's whole slab (hard); the old ones go.
        private void Wear(Mesh made, Mesh? hard)
        {
            Transform model = transform.Find("model");
            model.GetComponent<MeshFilter>().sharedMesh = made;
            model.GetComponent<MeshCollider>().sharedMesh = hard ?? made;
            Let();
            (mesh, solid) = (made, hard);
        }

        /// <summary>
        /// The mesh a generated part builds from what its ZDO saves, and the part's definition; null when it saves nothing
        /// readable. A deck standing at a place (at) leaves out the openings of the hatches on it; a deck with its tier is
        /// laid as that tier's boards (<see cref="KwDeckLayout"/>); planking standing at a place has its planks laid where
        /// it stands along its ship, running on from its neighbours' (<see cref="KwPlankShift"/>). A hull part or a rib is
        /// seated on its ship's keels (<see cref="KwSeat"/>): standing at a place, on the keels loaded; else where seat
        /// says (a blueprint's keels), or not at all.
        /// </summary>
        public static Mesh? MeshFor(string tool, string saved, Transform? at, out KwDef? def, Func<KwDef, List<Rect>>? holes = null, KwBiomeTier? tier = null,
            KwSeatAt? seat = null)
        {
            def = null;
            if (tool == KwLadderTool.Tool || tool == KwCapSpec.Tool)
                return tool == KwLadderTool.Tool ? KwLadder.Build(saved) : Cap(saved);
            if (KwHouses.Owns(tool))
                return KwHouses.Whole(tool, saved, tier ?? KwBiomeTiers.Meadows);
            if (tool == KwShapeTool.Keel)
                def = KwKeelSpec.TryParse(saved, out KwKeelSpec keel) ? KwKeel.Custom(keel) : null;
            else if (tool == KwShapeTool.Rib)
                def = KwRibs.TryRead(saved, out string family, out KwFrame frame) ? KwRibs.Custom(family, frame, Climb(at, seat)) : null;
            else if (tool == KwSailTool.Tool)
                def = KwMastSpec.TryParse(saved, out KwMastSpec mast) ? KwSailShapes.Rig(mast) : null;
            else if (tool == KwBowsprit.Tool)
                def = KwBowsprit.TryParse(saved, out KwBowsprit bowsprit) ? bowsprit.Def() : null;
            else
                def = SpanDef(tool, saved, at, seat);
            if (def != null && tool == "deck")
                Deck(def, at, holes, tier);
            Mesh? mesh = def != null ? KwMesh.Build(def) : null;
            if (mesh != null && at != null && tool == "planking")
                KwPlankShift.Apply(mesh, at);
            return mesh;
        }

        // How far up its keel a rib stands: on the keels loaded round it when it stands at a place, else where seat says.
        private static float Climb(Transform? at, KwSeatAt? seat) =>
            at != null ? KwKeelLines.World().Climb(at.position) : seat?.Climb ?? 0f;

        // A post cap from its saved text (KwCapSpec): a carved head fitted to its post, or the code's cap (KwCapMesh).
        private static Mesh? Cap(string saved) => KwCapSpec.TryParse(saved, out KwCapSpec cap) ? KwCapMesh.Of(cap) : null;

        // A part built from its span (planking, a deck, a hull railing; its keel ends as they stand and seated on the keels
        // loaded, or on seat's), or a roof railing (KwRoofRail).
        private static KwDef? SpanDef(string tool, string saved, Transform? at, KwSeatAt? seat)
        {
            if (tool == KwBiomeCost.Rail && KwRoofRail.TryParse(saved, out KwRoofRail roof))
                return KwRoofRailShape.Def(roof);
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return null;
            return (at != null ? KwSpanEnds.Live(span, at) : seat?.Seated(span) ?? span).Def(tool);
        }

        // A deck standing at a place (or given its openings) leaves out its hatches; one with its tier is laid as boards.
        private static void Deck(KwDef def, Transform? at, Func<KwDef, List<Rect>>? holes, KwBiomeTier? tier)
        {
            if (at != null || holes != null)
                def.Holes.AddRange(at != null ? KwDeckHoles.For(at, def) : holes!(def));
            if (tier != null)
                def.Boards = KwDeckLayout.Of(tier);
        }

        // A made keel's snap points from its sizes (its joints, feet, rib points, rudder point and post top).
        private void Snaps(KwDef def)
        {
            foreach (Transform child in transform)
            {
                if (child.CompareTag("snappoint"))
                    Destroy(child.gameObject);
            }
            KwSnaps.Add(transform, def);
        }

        private void OnDestroy()
        {
            Hulls.Remove(this);
            Keels.Remove(this);
            Let();
            if (lowDeck)
                Refloor(transform.position, KwSoleDock.Reach);
        }

        // The meshes this part made go.
        private void Let()
        {
            if (mesh != null)
                Destroy(mesh);
            if (solid != null)
                Destroy(solid);
        }
    }
}
