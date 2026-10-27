using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The keel and rib tools: a click picks where the part goes, a small window (<see cref="KwForm"/>) takes its sizes, and
    /// <see cref="KwPreview"/> shows it see-through as they change (every keel ends in its post but one joining two
    /// others, and a flat stern ends square with its post just past its end), in the biome tier its "Material" picks
    /// (<see cref="KwBiomeField"/>, the keel's tier: ribs and keels alike); OK builds it there, free, a networked generated
    /// part (<see cref="KwGenerated"/>) whose sizes and tier travel in its ZDO. Keel tool (<see cref="KwKeelPick"/>): click
    /// near a keel's open end, then near another's to join the two (no post and no flat stern, so both stay joined), or near
    /// the same one's end again to carry it on from that end (as thick as it); a click elsewhere on a keel edits it
    /// (<see cref="KwKeelEdit"/>); a keel block a made keel starts from builds its mirror twin; any other keel rest on the
    /// dock starts a keel line there, running along the dock the way the tool's placeholder points (toward its post; the
    /// wheel turns it).
    /// Rib tool: click a
    /// keel; the rib stands on the keel's nearest rib point ("Keel top", every metre), square to it, its sides starting
    /// at that keel's sides; with "How many" over one, that many ribs are spread evenly along the keel line's level part
    /// instead (<see cref="KwRibRow"/>), all shown before they are built; a click on a rib edits it (<see cref="KwRibEdit"/>).
    /// The window opens with the sizes used last.
    /// </summary>
    public static class KwShapeTool
    {
        public const string Keel = "keel", Rib = "rib";
        /// <summary>The rib window's hull shapes, in the order of <see cref="KwRibs.Families"/>.</summary>
        internal static readonly string[] Shapes = { "Viking", "Round", "Rounded", "Galleon", "Flat (barge)", "Deep V", "Wineglass" };

        /// <summary>The keel window's curves, in the order of <see cref="KwKeelCurve"/>; a keel joining two others climbs by a curve, never to a flat stern.</summary>
        internal static readonly string[] Curves = { "Quick", "Oval", "Flat stern" }, JoinCurves = { "Quick", "Oval" };

        private static KwSite? site;
        private static string open = "", saved = "";
        private static KwKeelSpec lastKeel = new KwKeelSpec(4f, 0f, KwKeelCurve.Quick, true, FirstPost, KwKeel.Standard);
        private static int lastShape;
        private static KwFrame lastFrame = KwFrame.Preset(3);
        private static KwBiomeTier tier = KwBiomeTiers.Meadows;
        private static Piece? keel;
        private static int count = 1;
        private static bool joining;

        // The post height the keel window first offers.
        private const float FirstPost = 1f;

        /// <summary>The hull shape the rib tool last used: a run from keel end to keel end with no rib between takes it.</summary>
        public static string LastShape => KwRibs.Families[lastShape];

        /// <summary>The rib size the rib tool last used: the midship frame of a run from keel end to keel end with no rib between.</summary>
        public static KwFrame LastFrame => lastFrame;

        public static void Click(Player player, string tool)
        {
            if (KwForm.IsOpen)
                return;
            KwClick? aim = KwTools.Aim(player);
            if (tool == Keel)
            {
                KwKeelStart? start = aim == null ? null : KwKeelPick.Click(player, aim.Value, lastKeel);
                if (aim == null)
                    Say(player, KwKeelPick.Hint);
                if (start != null)
                {
                    joining = start.Exact;
                    Open(player, Keel, start.Site, aim!.Value.Piece, KeelFields(start), start.Title);
                }
                return;
            }
            KwSite? at = aim == null ? null : KwShapeSites.Rib(aim.Value);
            if (at == null)
                Say(player, "Click a keel where the rib should stand");
            else
                Open(player, Rib, at, aim!.Value.Piece, RibFields(), "New rib");
        }

        private static void Open(Player player, string tool, KwSite at, Piece piece, List<KwField> fields, string title)
        {
            (site, open, keel) = (at, tool, piece);
            fields.Add(KwBiomeField.Field(KwPart.Keel));
            KwForm.Open(title, fields, Changed, values => Done(player, values));
        }

        // The keel's sizes as the click starts them (KwKeelPick); a keel joining two others is measured to the centimetre and
        // climbs by a curve only. Every keel ends in its post (but one joining two others), so there is no choice of one.
        private static List<KwField> KeelFields(KwKeelStart start)
        {
            KwKeelSpec spec = start.Spec;
            KwKeelBody body = spec.Body;
            return new List<KwField>
            {
                KwField.Number("length", "Length", spec.Length, KwKeelSpec.MinLength, KwKeelSpec.MaxLength, start.Exact ? 0.01f : 0.5f),
                KwField.Number("rise", "Rise at its end", spec.Rise, 0f, KwKeelSpec.MaxRise, start.Exact ? 0.01f : 0.1f),
                KwField.Choice("curve", "Curve", start.Exact ? JoinCurves : Curves, (int)spec.Curve),
                KwField.Number("post_height", "Post height", spec.PostAbove, KwKeelSpec.MinPost, KwKeelSpec.MaxPost, 0.25f),
                KwField.Number("width", "Keel width", body.Half * 2f, KwKeelBody.MinWidth, KwKeelBody.MaxWidth, 0.02f),
                KwField.Number("depth", "Keel depth", body.Depth, KwKeelBody.MinDepth, KwKeelBody.MaxDepth, 0.05f),
            };
        }

        private static List<KwField> RibFields() => new List<KwField>
        {
            KwField.Choice("shape", "Shape", Shapes, lastShape),
            KwField.Number("width", "Width", lastFrame.Beam, KwFrame.MinBeam, KwFrame.MaxBeam, 0.25f),
            KwField.Number("height", "Height", lastFrame.Height, KwFrame.MinHeight, KwFrame.MaxHeight, 0.1f),
            KwField.Number("count", "How many (spread evenly)", count, 1f, 40f, 1f, ""),
        };

        // The window's values as the part they make: remembered for next time, saved for building, shown see-through.
        private static void Changed(KwFormValues values)
        {
            if (open == Keel)
            {
                KwKeelBody body = new KwKeelBody(values.Number("width") / 2f, values.Number("depth"));
                float rise = values.Number("rise");
                KwKeelCurve curve = (KwKeelCurve)Mathf.Clamp(values.ChoiceIndex("curve"), 0, (joining ? JoinCurves : Curves).Length - 1);
                lastKeel = new KwKeelSpec(values.Number("length"), rise, curve, !joining, values.Number("post_height"), body);
                saved = lastKeel.ToString();
            }
            else
            {
                lastShape = Mathf.Clamp(values.ChoiceIndex("shape"), 0, KwRibs.Families.Length - 1);
                lastFrame = new KwFrame(values.Number("width"), values.Number("height"), 0, site != null ? site.Body.Half : KwKeel.HalfWidth);
                saved = KwRibs.Save(KwRibs.Families[lastShape], lastFrame);
                count = Mathf.Max(1, Mathf.RoundToInt(values.Number("count")));
            }
            tier = KwBiomeField.Picked(values, KwPart.Keel);
            Preview();
        }

        // Where the part's origin goes: a keel's underside at its start, a rib's foot.
        private static Vector3 Origin(KwSite at) => open == Keel ? at.KeelOrigin(lastKeel.Body.Depth) : at.Point;

        /// <summary>Shows the part the open window would build; nothing while no window of these tools is open.</summary>
        public static void Preview()
        {
            KwSite? at = site;
            if (at == null || !KwForm.IsOpen)
            {
                KwPreview.Hide();
                return;
            }
            string tool = open, text = saved;
            Vector3 origin = Origin(at);
            List<(Vector3 Point, Quaternion Turn)>? row = Row(at);
            if (row != null && row.Count > 0)
                origin = row[0].Point;
            KwPreview.Show(tool + "|" + text + "|" + tier.Id + "|" + origin.ToString("F2") + "|" + (row?.Count ?? 1), origin, at.Turn,
                preview => row != null ? RowMesh(text, origin, at.Turn, row)
                    : KwGenerated.MeshFor(tool, text, null, out _, null, null, new KwSeatAt(KwKeelLines.World(), origin, Vector3.forward)) ?? new Mesh(),
                KwBiomeLooks.For(KwPart.Keel, tier));
        }

        // The even row the rib window asks for (null for one rib or the keel tool).
        private static List<(Vector3 Point, Quaternion Turn)>? Row(KwSite at) =>
            open == Rib && count > 1 && keel != null ? KwRibRow.Spots(keel, at.Turn, count) : null;

        // Every rib of a row in one see-through mesh, placed from the first.
        private static Mesh RowMesh(string text, Vector3 origin, Quaternion turn, List<(Vector3 Point, Quaternion Turn)> row)
        {
            Mesh? one = KwGenerated.MeshFor(Rib, text, null, out _);
            if (one == null)
                return new Mesh();
            Quaternion back = Quaternion.Inverse(turn);
            CombineInstance[] parts = row.ConvertAll(s => new CombineInstance
            {
                mesh = one, transform = Matrix4x4.TRS(back * (s.Point - origin), back * s.Turn, Vector3.one),
            }).ToArray();
            Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(parts, true, true);
            Object.Destroy(one);
            return mesh;
        }

        private static void Done(Player player, KwFormValues? values)
        {
            KwSite? at = site;
            site = null;
            KwPreview.Hide();
            if (values == null || at == null || player == null)
                return;
            site = at;
            Changed(values);
            site = null;
            List<(Vector3 Point, Quaternion Turn)>? row = Row(at);
            if (row == null)
                KwTools.Build(player, open, Origin(at), at.Turn, saved, tier);
            else
                BuildRow(player, row);
        }

        // Each rib of a row, in the tier picked.
        private static void BuildRow(Player player, List<(Vector3 Point, Quaternion Turn)> row)
        {
            foreach ((Vector3 point, Quaternion turn) in row)
                KwTools.Spawn(player, Rib, point, turn, saved, tier);
            if (row.Count > 0)
                Say(player, row.Count == 1 ? "1 rib built" : $"{row.Count} ribs built, spread evenly along the keel");
            else
                Say(player, "Every rib point there already has a rib");
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }
}
