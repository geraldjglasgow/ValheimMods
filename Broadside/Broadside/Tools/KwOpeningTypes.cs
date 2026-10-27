using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One opening type the Doorway or Window tool places with a click (<see cref="KwOpeningTypes"/>): its name as the
    /// placement message gives it, what it cuts (<see cref="KwOpeningAsk"/>: shape, width, height, frame, cover, and the
    /// sill's height over the deck inside that the ghost snaps to), and how far apart a side's openings stand, centre to
    /// centre, when the side gives no spacing of its own (<see cref="KwOpeningSpacing"/>). Pure.
    /// </summary>
    public sealed class KwOpeningType
    {
        public KwOpeningType(string name, KwOpeningAsk ask, float spacing)
        {
            (Name, Ask, Spacing) = (name, ask, spacing);
        }

        public string Name { get; }
        public KwOpeningAsk Ask { get; }
        public float Spacing { get; }

        public bool Doorway => Ask.Kind == KwOpening.Door;
    }

    /// <summary>
    /// The types the scroll wheel steps through (the user's request, 2026-10-25: "an easy way to cycle through the window
    /// types"; doors "limited to a set width, with a double door that is twice as wide"; openings with no door "any size,
    /// even huge"). Windows: square gunports 0.7 m with a lid flapping down or up, a round porthole 0.45 m with its lid, a
    /// small open one 0.3 m, a rectangle and an arched window 0.6 by 0.9 m with side shutters, a plain rectangle opening;
    /// all in plain timber. Doorways: a single door 1.3 m wide, a double door 2.2 m wide, an open doorway with no door; 2 m high
    /// (lower where the wall has less room). A type of the player's own (an edited opening that is none of these) is named
    /// from its values (<see cref="Describe"/>). The doors' widths and an open doorway's least sizes. Pure.
    /// </summary>
    public static class KwOpeningTypes
    {
        /// <summary>
        /// A single door's width, a double door's, and a door's height before the wall's room lowers it. The player is 0.98 m
        /// across (the game's capsule), so a door's frame must leave more than that (the user, 2026-10-26: "I am still not
        /// able to walk through doors into cabins").
        /// </summary>
        public const float SingleWide = 1.3f, DoubleWide = 2.2f, DoorTall = 2f;

        /// <summary>An open doorway's (no door) least width and height; its most is what the wall has room for.</summary>
        public const float OpenLeastWide = 1.2f, OpenLeastHigh = 1f;

        /// <summary>The spacing a side starts with: gunports, other windows, doorways (centre to centre).</summary>
        public const float GunportSpacing = 2f, WindowSpacing = 1.5f, DoorSpacing = 3f;

        public static readonly KwOpeningType[] Windows =
        {
            Window("Square gunport, lid flaps down", KwOpening.Square, 0.7f, 0.7f, KwOpening.Down, 0.5f, GunportSpacing),
            Window("Square gunport, lid flaps up", KwOpening.Square, 0.7f, 0.7f, KwOpening.Up, 0.5f, GunportSpacing),
            Window("Round porthole, round lid", KwOpening.Round, 0.45f, 0.45f, KwOpening.Lid, 1.1f, WindowSpacing),
            Window("Small round porthole, open", KwOpening.Round, 0.3f, 0.3f, KwOpening.None, 1.2f, WindowSpacing),
            Window("Rectangle window, shutters", KwOpening.Rect, 0.6f, 0.9f, KwOpening.Shutters, 0.8f, WindowSpacing),
            Window("Arched window, shutters", KwOpening.Arch, 0.6f, 0.9f, KwOpening.Shutters, 0.8f, WindowSpacing),
            Window("Plain rectangle opening", KwOpening.Rect, 0.6f, 0.9f, KwOpening.None, 0.8f, WindowSpacing),
        };

        public static readonly KwOpeningType[] Doors =
        {
            Door("Single door", SingleWide, KwOpening.Leaf),
            Door("Double door", DoubleWide, KwOpening.DoubleDoor),
            Door("Open doorway, no door", SingleWide, KwOpening.None),
        };

        private static KwOpeningType Window(string name, string shape, float wide, float tall, string cover, float above, float spacing) =>
            new KwOpeningType(name, new KwOpeningAsk { Kind = shape, Wide = wide, Tall = tall, Covered = cover, Above = above }, spacing);

        private static KwOpeningType Door(string name, float wide, string cover) =>
            new KwOpeningType(name, new KwOpeningAsk { Kind = KwOpening.Door, Wide = wide, Tall = DoorTall, Covered = cover, Above = 0f }, DoorSpacing);

        /// <summary>A door's fixed width by its cover (single or double); an open doorway keeps the width given.</summary>
        public static float DoorWidth(string cover, float open) =>
            cover == KwOpening.DoubleDoor ? DoubleWide : cover == KwOpening.Leaf ? SingleWide : open;

        /// <summary>Whether two answers cut the same opening: shape, sizes within a centimetre, frame and cover.</summary>
        public static bool Same(KwOpeningAsk a, KwOpeningAsk b) =>
            a.Kind == b.Kind && a.Framed == b.Framed && a.Covered == b.Covered && Mathf.Abs(a.Wide - b.Wide) < 0.01f
            && (a.Kind == KwOpening.Door || Mathf.Abs(a.High - b.High) < 0.01f);

        /// <summary>A type of the player's own: "Square 0.8 m, heavy timber, lid flaps down"; "Open doorway 3.2 m wide".</summary>
        public static KwOpeningType Own(KwOpeningAsk ask) =>
            new KwOpeningType(Describe(ask), ask.Copy(), ask.Kind == KwOpening.Door ? DoorSpacing : ask.Kind == KwOpening.Square ? GunportSpacing : WindowSpacing);

        /// <summary>An opening's name: the fixed type it matches, else what it cuts in words.</summary>
        public static string Name(KwOpeningAsk ask)
        {
            KwOpeningType? type = System.Array.Find(ask.Kind == KwOpening.Door ? Doors : Windows, t => Same(t.Ask, ask));
            return type != null ? type.Name : Describe(ask);
        }

        /// <summary>What an opening's answers cut, in words.</summary>
        public static string Describe(KwOpeningAsk ask)
        {
            string frame = ask.Framed == KwOpening.Heavy ? ", heavy timber" : ask.Framed == KwOpening.Iron ? ", iron-banded" : "";
            if (ask.Kind == KwOpening.Door)
                return ask.Covered == KwOpening.None ? $"Open doorway {ask.Wide:0.0#} m wide{frame}" : $"{(ask.Covered == KwOpening.DoubleDoor ? "Double" : "Single")} door{frame}";
            string size = ask.Kind == KwOpening.Round || ask.Kind == KwOpening.Square ? $"{ask.Wide:0.0#} m" : $"{ask.Wide:0.0#} x {ask.Tall:0.0#} m";
            return $"{Shape(ask.Kind)} {size}{frame}, {Cover(ask.Covered)}";
        }

        private static string Shape(string shape) => shape switch
        {
            KwOpening.Round => "Round porthole",
            KwOpening.Square => "Square port",
            KwOpening.Arch => "Arched window",
            _ => "Rectangle window",
        };

        private static string Cover(string cover) => cover switch
        {
            KwOpening.Down => "lid flaps down",
            KwOpening.Up => "lid flaps up",
            KwOpening.Lid => "round lid",
            KwOpening.Shutters => "shutters",
            _ => "open",
        };
    }
}
