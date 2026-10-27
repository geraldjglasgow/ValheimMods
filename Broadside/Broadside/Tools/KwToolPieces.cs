using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The seven tools in the hammer (planking, deck, railing: small boards whose "placing" is a click on a rib or a keel
    /// end, see <see cref="KwTools"/>, the railing also on a cabin's roof, <see cref="KwRoofRailTool"/>; the ladder: one click
    /// on a hatch or an open edge, <see cref="KwLadderTool"/>; the keel and rib tools,
    /// <see cref="KwShapeTool"/>; the sail tool, <see cref="KwSailTool"/>; the bowsprit tool, a click on a stem post's top,
    /// <see cref="KwBowspritTool"/>; the Post cap tool, a click on a post's top, the wheel its style, <see cref="KwCapTool"/>;
    /// the Cabin tool, two clicks on deck sections or a cabin's roof,
    /// <see cref="KwCabinTool"/>) and the generated parts they make (empty prefabs
    /// that build their mesh from what is saved in their ZDO, <see cref="KwGenerated"/>; never in the hammer). A tool's click
    /// on a part it makes edits that part, Shift + click every one of its kind on the dock (<see cref="KwEdit"/>), as each
    /// tool's description says (<see cref="KwToolWords"/>).
    /// </summary>
    public static class KwToolPieces
    {
        public const string ToolPrefix = "KW_tool_", PartPrefix = "KW_gen_";
        /// <summary>The generated parts' tools; "castle" has no tool in the hammer now, its part kept for castles built before it became the Cabin tool.</summary>
        public static readonly string[] Tools = { "planking", "deck", "rail", "ladder", "keel", "rib", "sail", KwBowsprit.Tool, KwHouses.Castle, KwHouses.Cabin, KwCapTool.Tool };

        public static IEnumerable<KwDef> All()
        {
            yield return Tool("planking", Planks);
            yield return Tool("deck", Boards);
            yield return Tool("rail", Rail);
            KwDef ladder = Tool("ladder", b => KwLadder.Shape(b, 1.2f));
            ladder.Pivot = new Vector2(0f, -1.2f);
            yield return ladder;
            KwDef keel = Tool("keel", b => KwKeel.Custom(new KwKeelSpec(2f, 0.5f, KwKeelCurve.Quick, true, 1f, KwKeel.Standard)).Shape!(b));
            keel.Pivot = new Vector2(0f, -KwKeel.Depth);
            yield return keel;
            yield return Tool("rib", b => KwRibs.Custom("round", new KwFrame(1.6f, 1f)).Shape!(b));
            yield return Tool("sail", b => KwSailShapes.Rig(KwMastSpec.Lone(new KwSailSpec(0, 1.6f, 1.2f, 0.8f, ""))).Shape!(b));
            yield return Tool(KwBowsprit.Tool, b => new KwBowsprit(1.6f, 15f, 0, "").Def().Shape!(b));
            yield return KwCapTool.Def();
            yield return Tool(KwHouses.Cabin, KwCabinTool.Mark);
            yield return Tool(KwDoorwayTool.Tool, KwDoorwayTool.Mark);
            yield return Tool(KwWindowTool.Tool, KwWindowTool.Mark);
            foreach (string tool in Tools)
                yield return Part(tool);
        }

        private static KwDef Part(string tool)
        {
            KwDef part = new KwDef(PartPrefix + tool, Label(tool), "gen") { Wood = tool != "planking" };
            if (tool == "ladder")
                part.Setup = prefab => prefab.AddComponent<KwLadderClimb>();
            else if (tool == "sail")
                part.Setup = prefab => prefab.AddComponent<KwSailRig>();
            else if (tool == KwBowsprit.Tool)
                part.Setup = prefab => prefab.AddComponent<KwJibRig>();
            else if (tool == KwCapTool.Tool)
                part.Setup = KwCapTool.Part;
            return part;
        }

        public static string Label(string tool) => tool switch
        {
            "planking" => "Planking",
            "deck" => "Deck",
            "ladder" => "Ladder",
            "keel" => "Keel",
            "rib" => "Rib",
            "sail" => "Mast and sail",
            KwBowsprit.Tool => "Bowsprit",
            KwCapTool.Tool => "Post cap",
            KwHouses.Cabin or KwHouses.Castle => "Cabin",
            KwDoorwayTool.Tool => "Doorway",
            KwWindowTool.Tool => "Window",
            _ => "Railing",
        };

        // A tool is never placed itself. The keel, sail and bowsprit tools turn with the wheel as any piece does: their
        // placeholder points the way a keel started on the dock runs, a mast's sail faces, or a bowsprit runs out along its
        // keel (<see cref="KwShapeSites"/>); the Doorway and Window tools "turn" too, the wheel stepping through their types
        // (<see cref="KwOpeningHover"/>); the other tools never turn, so the wheel zooms the view while one is in the hammer.
        private static KwDef Tool(string tool, System.Action<MeshBuilder> shape) => new KwDef(ToolPrefix + tool, KwToolWords.Name(tool), "tool")
        {
            Description = KwToolWords.Of(tool), Wood = true, Shape = shape, Setup = prefab => prefab.GetComponent<Piece>().m_canRotate = Turns(tool),
        };

        private static bool Turns(string tool) =>
            tool == "keel" || tool == "sail" || tool == KwBowsprit.Tool || tool == KwDoorwayTool.Tool || tool == KwWindowTool.Tool;

        // A few planks bent along a hull's side.
        private static void Planks(MeshBuilder b)
        {
            for (int i = 0; i < 3; i++)
                Solid.Box(b, new Vector3(0f, 0.1f * i, 0.18f * i), new Vector3(1.2f, 0.1f * i + 0.05f, 0.18f * i + 0.16f));
        }

        private static void Boards(MeshBuilder b)
        {
            for (int i = 0; i < 4; i++)
                Solid.Box(b, new Vector3(0f, -0.4f + 0.2f * i, 0f), new Vector3(1.2f, -0.22f + 0.2f * i, 0.05f));
        }

        private static void Rail(MeshBuilder b)
        {
            Solid.Box(b, new Vector3(0f, -0.04f, 0.7f), new Vector3(1.2f, 0.04f, 0.78f));
            Solid.Box(b, new Vector3(0f, -0.06f, 0f), new Vector3(1.2f, 0.06f, 0.12f));
            foreach (float x in new[] { 0.1f, 0.6f, 1.1f })
                Solid.Box(b, new Vector3(x - 0.04f, -0.04f, 0.12f), new Vector3(x + 0.04f, 0.04f, 0.7f));
        }
    }
}
