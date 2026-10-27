using static Broadside.KwToolText;

namespace Broadside
{
    /// <summary>
    /// Each tool's name and its description in the hammer's panel (<see cref="KwToolText"/>): what each click, the wheel and
    /// the keys do with it, one line each.
    /// </summary>
    public static class KwToolWords
    {
        /// <summary>A tool's name in the hammer: "Sail tool".</summary>
        public static string Name(string tool) => tool switch
        {
            "rail" => "Railing tool",
            "sail" => "Sail tool",
            _ when tool == KwCapTool.Tool => "Post cap tool",
            _ => KwToolPieces.Label(tool) + " tool",
        };

        /// <summary>A tool's lines in the hammer, one action a line; last, every tool's: a click on another tool's part switches to it.</summary>
        public static string Of(string tool) => Own(tool) + Sea(tool) + "\n" + OtherParts;

        // The tools that work on a launched ship too (KwSeaTools): one line saying so.
        private static string Sea(string tool)
        {
            foreach (KwSeaKind kind in KwSeaKinds.All)
            {
                if (kind.Serves(tool))
                    return "\n" + Lines(("At sea", KwSeaWords.Panel));
            }
            return "";
        }

        private static string Own(string tool) => tool switch
        {
            "planking" or "deck" or "rail" => Span(tool),
            "ladder" or "keel" or "rib" => Frame(tool),
            "sail" or KwBowsprit.Tool => Rig(tool),
            _ when tool == KwCapTool.Tool => Cap(),
            _ => Houses(tool),
        };

        // The tools laid from rib to rib.
        private static string Span(string tool) => tool switch
        {
            "planking" => WithEdits("a planking part", "planking part",
                (Click, "a rib, then another rib or a keel end"),
                (Click, "both ends of a keel line: plank all of it")),
            "deck" => WithEdits("a deck away from its ends", "deck",
                (Click, "a rib, then another rib or a keel end"),
                ("Shift + second click", "lay the deck sloping")),
            _ => WithEdits("a railing, or the hull or roof under it", "railing",
                (Click, "a rib or the hull's planking, then another (or a keel end)"),
                (Click, "a cabin's roof: rail its edges")),
        };

        // The keel, its ribs and the ladders.
        private static string Frame(string tool) => tool switch
        {
            "ladder" => WithEdits("a ladder", "ladder",
                (Click, "a hatch, or the open edge of a deck or a cabin's roof: a ladder down to the floor")),
            "keel" => WithEdits("a keel's middle", "keel",
                (Wheel, "turn the way a new keel runs"),
                (Click, "a keel block, or near a keel's open end: a new keel"),
                (Click, "a keel's end, then another keel's end: join them")),
            _ => WithEdits("a rib", "rib",
                (Click, "a keel: a new rib, then type its shape and size")),
        };

        // The masts and the bowsprit.
        private static string Rig(string tool) => tool == "sail"
            ? WithEdits("a mast", "mast",
                (Wheel, "turn the way the sail faces"),
                (Click, "a deck, a cabin's roof or a keel: a new mast and its sails"))
            : WithEdits("a bowsprit", "bowsprit",
                (Wheel, "turn it to the keel's other end"),
                (Click, "the top of the bow's stem post: a bowsprit and its jibs"));

        private static string Cap() => WithEdits("a post cap", "post cap",
            (Wheel, "the cap's style"),
            (ShiftWheel, "longer or shorter"),
            ("Ctrl + wheel", "lean the neck"),
            (Click, "a stem or stern post's top: cap it"));

        // The Cabin tool and the openings cut in walls and planking.
        private static string Houses(string tool)
        {
            if (tool == KwDoorwayTool.Tool)
                return Openings("a doorway", "a cabin's wall, its raised side or the hull's planking: cut the doorway shown");
            if (tool == KwWindowTool.Tool)
                return Openings("a window", "the hull's planking or a cabin's wall: cut the window shown");
            return WithEdits("a cabin's wall", "cabin",
                (Click, "a deck section, then the cabin's last one along the deck (or the same again): a new cabin"),
                (Click, "a cabin's roof twice: a cabin on top of it"));
        }

        private static string Openings(string one, string click) => Lines(
            (Wheel, "the type"),
            (Click, click),
            ("Shift", "row of evenly spaced (wheel: how many, click: place)"),
            (Click, $"{one}: change it or fill it in"),
            (ShiftClick, $"{one}: change its row (number, type)"));
    }
}
