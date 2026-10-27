using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>One part a cabin stack moves (<see cref="KwCabinLift"/>): where it is to stand and its new saved text, or taken away (no text).</summary>
    public sealed class KwStackMove
    {
        public KwStackMove(KwStackPart part, Vector3 position, string? text)
        {
            Part = part;
            Position = position;
            Text = text;
        }

        public KwStackPart Part { get; }
        public Vector3 Position { get; }
        public string? Text { get; }

        public bool Taken => Text == null;

        /// <summary>How far it moves from where it stands.</summary>
        public Vector3 Shift => Position - Part.Position;

        public static KwStackMove Taking(KwStackPart part) => new KwStackMove(part, part.Position, null);
    }

    /// <summary>
    /// A cabin stack carried with an edit of its cabins (<see cref="KwCabinFollow"/>): bottom up, every cabin standing on a
    /// cabin that changes is set on that cabin's new roof (<see cref="KwCabinRebase"/>), so a third tier follows the second;
    /// a cabin no longer buildable there is taken away with everything on it; then what stands on each cabin changed goes
    /// with its roof (<see cref="KwStackRiders"/>). Also what a cabin's removal takes with it (<see cref="Removal"/>). Pure
    /// numbers on parts read once (<see cref="KwStackPart"/>), so it is checked offline as it runs in the game.
    /// </summary>
    public sealed class KwCabinLift
    {
        private static readonly List<string> None = new List<string>();

        private readonly Dictionary<KwStackPart, KwCabinAt> now = new Dictionary<KwStackPart, KwCabinAt>();
        private readonly HashSet<KwStackPart> gone = new HashSet<KwStackPart>();

        private KwCabinLift(List<KwStackPart> parts)
        {
            Parts = parts;
            Cabins = parts.FindAll(part => part.Cabin != null);
            Cabins.Sort((a, b) => a.At.Floor.CompareTo(b.At.Floor));
        }

        public List<KwStackPart> Parts { get; }

        /// <summary>The cabins among the parts, lowest deck first.</summary>
        public List<KwStackPart> Cabins { get; }

        /// <summary>Every part that moves, is built again or is taken away, each once (an edited cabin only when it is carried).</summary>
        public List<KwStackMove> Moves { get; } = new List<KwStackMove>();

        /// <summary>The cabins carried on a changed roof (not the edit's own as planned), and those whose walls moved to fit it.</summary>
        public List<KwStackPart> Carried { get; } = new List<KwStackPart>();
        public List<KwStackPart> Refitted { get; } = new List<KwStackPart>();

        /// <summary>The changed cabins as they will stand, the edit's own included.</summary>
        public IReadOnlyDictionary<KwStackPart, KwCabinAt> Now => now;

        public bool Gone(KwStackPart cabin) => gone.Contains(cabin);

        /// <summary>
        /// The stack carried with an edit: planned, each cabin the edit changes as it plans it (fitted); typed, the keys the
        /// player changed in the window (kept as typed on an edited cabin standing on another one changed).
        /// </summary>
        public static KwCabinLift Edit(List<KwStackPart> parts, Dictionary<KwStackPart, KwCabinSpec> planned, ICollection<string> typed)
        {
            KwCabinLift lift = new KwCabinLift(parts);
            foreach (KwStackPart cabin in lift.Cabins)
                lift.Carry(cabin, planned, typed);
            KwStackRiders.Follow(lift);
            return lift;
        }

        // One cabin, its lower one done before it: gone with a lower one gone; set on its lower one's new roof when that
        // changed (gone when nothing can be built there); as planned when the edit changes it; else left as it is.
        private void Carry(KwStackPart cabin, Dictionary<KwStackPart, KwCabinSpec> planned, ICollection<string> typed)
        {
            KwStackPart? under = KwCabinStack.Under(cabin, Cabins);
            bool mine = planned.TryGetValue(cabin, out KwCabinSpec plan);
            KwCabinAt was = cabin.At;
            if (under != null && gone.Contains(under))
                Take(cabin);
            else if (under != null && now.TryGetValue(under, out KwCabinAt below))
                Set(cabin, KwCabinRebase.On(was, KwCabinSizes.Of(mine ? plan : was.Spec), mine ? typed : None, under.At, below, out bool moved), moved);
            else if (mine && !new KwCabinAt(plan, was.Frame).Same(was))
                now[cabin] = new KwCabinAt(plan, was.Frame);
        }

        // A cabin set on its lower one's new roof: carried there, or taken when nothing can be built there; left when the same.
        private void Set(KwStackPart cabin, KwCabinAt set, bool moved)
        {
            if (set.Same(cabin.At))
                return;
            if (set.Spec.Unbuilt != null)
            {
                Take(cabin);
                return;
            }
            now[cabin] = set;
            Carried.Add(cabin);
            if (moved)
                Refitted.Add(cabin);
            Moves.Add(new KwStackMove(cabin, set.Frame.Origin, set.Spec.ToString()));
        }

        private void Take(KwStackPart cabin)
        {
            gone.Add(cabin);
            Moves.Add(KwStackMove.Taking(cabin));
        }

        /// <summary>A part's move added, once (a part two rules find is moved by the first).</summary>
        internal void Add(KwStackMove? move)
        {
            if (move != null && !Moves.Exists(m => m.Part == move.Part))
                Moves.Add(move);
        }

        /// <summary>
        /// What goes with a cabin taken away (the hammer, an undo): the cabins standing on it all the way up, the roof railings
        /// on it and on them, the pieces standing on those roofs and the ladders standing on any of them; ladders against them
        /// from the deck stay, as they reach it. The cabin itself is not among them.
        /// </summary>
        public static List<KwStackPart> Removal(List<KwStackPart> parts, KwStackPart cabin)
        {
            KwCabinLift lift = new KwCabinLift(parts);
            lift.gone.Add(cabin);
            foreach (KwStackPart upper in lift.Cabins)
            {
                KwStackPart? under = KwCabinStack.Under(upper, lift.Cabins);
                if (under != null && lift.gone.Contains(under))
                    lift.Take(upper);
            }
            KwStackRiders.Remove(lift);
            return lift.Moves.ConvertAll(move => move.Part);
        }
    }
}
