using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The mast the sail tool's window is making (<see cref="KwSailForm"/>): the mast's height and its sails as the player
    /// asked for them, in the mast's order (the square sails from the course up, then the one along the ship), the one
    /// picked, and the mast they make once placed to fit (<see cref="Fitted"/>). Adding puts a square sail on top of the
    /// stack, a little narrower and shorter than the one below it, when the mast has a metre of room for one, else a gaff
    /// sail behind the mast when it has none, in the pattern of the sail it is made like and its cloth when new work may pick
    /// it (else raw hide, <see cref="KwSailCloth.ForNew"/>); removing takes off the
    /// picked sail, a mast keeping one at least.
    /// </summary>
    internal sealed class KwSailDraft
    {
        private const float Narrower = 0.85f, Shorter = 0.8f, Step = 0.25f;

        public KwSailDraft(float mast, List<KwSailSpec> sails)
        {
            Mast = mast;
            Sails = sails;
        }

        public float Mast { get; set; }
        public List<KwSailSpec> Sails { get; }
        public int Picked { get; set; }
        public KwSailSpec Pick => Sails[Picked];

        /// <summary>The mast's crow's nest, fighting top and pennant as the window's second page has them.</summary>
        public KwMastTops Tops { get; set; }

        /// <summary>The mast these sails make, placed to fit, with its platforms and pennant.</summary>
        public KwMastSpec Fitted => KwMastSpec.Fit(Mast, Sails).With(Tops);

        /// <summary>The same mast, sails, platforms and pennant, the first sail picked.</summary>
        public KwSailDraft Copy() => new KwSailDraft(Mast, new List<KwSailSpec>(Sails)) { Tops = Tops };

        /// <summary>
        /// The sails taken as the mast places them, in its order and cut to fit (a cut height down to the window's step, a
        /// raised mast up to its step), the picked sail still picked: what the window shows when it is built again.
        /// </summary>
        public void Settle()
        {
            int picked = Moved(Picked);
            KwMastSpec mast = Fitted;
            Mast = mast.Mast > Mast + 0.001f ? Mathf.Ceil(mast.Mast * 2f - 0.001f) / 2f : Mast;
            Sails.Clear();
            foreach (KwSailSpec sail in mast.Sails)
                Sails.Add(sail.With(sail.Shape, sail.Width, OnStep(sail.Height)));
            Picked = Mathf.Clamp(picked, 0, Sails.Count - 1);
            Tops = mast.Tops;
        }

        // A height on the window's step, rounded down when it is not (a cut height), never under the least a sail is.
        private static float OnStep(float height)
        {
            float steps = height / Step;
            return Mathf.Abs(steps - Mathf.Round(steps)) < 0.01f ? height : Mathf.Max(Mathf.Floor(steps) * Step, KwSailSpec.MinSize);
        }

        // Where the sail at index i goes in the mast's order: among the square sails by those before it, or after them all.
        private int Moved(int i)
        {
            int before = 0, squares = 0;
            for (int j = 0; j < Sails.Count; j++)
            {
                squares += Sails[j].Square ? 1 : 0;
                before += j < i && Sails[j].Square ? 1 : 0;
            }
            return Sails[i].Square ? before : Mathf.Min(squares, KwMastSpec.MostSquare);
        }

        /// <summary>The shapes the picked sail may take: square while the stack has room for it, along the ship while no other sail is.</summary>
        public List<int> Shapes()
        {
            int squares = 0;
            bool along = false;
            for (int j = 0; j < Sails.Count; j++)
            {
                squares += j != Picked && Sails[j].Square ? 1 : 0;
                along |= j != Picked && !Sails[j].Square;
            }
            List<int> shapes = new List<int>();
            if (squares < KwMastSpec.MostSquare)
                shapes.Add(0);
            if (!along)
                shapes.AddRange(new[] { 1, 2 });
            return shapes;
        }

        /// <summary>Adds a sail and picks it: a square sail on top of the stack, else a gaff sail; what was done, or why not.</summary>
        public string Add()
        {
            KwMastSpec mast = Fitted;
            float room = KwSailFit.Room(mast);
            if (mast.Squares < KwMastSpec.MostSquare && room >= KwSailSpec.MinSize)
                return AddSquare(mast, room);
            float under = KwSailFit.AlongRoom(mast);
            if (mast.Sails.Count == mast.Squares && under >= KwSailSpec.MinSize)
                return AddAlong(mast, under);
            if (mast.Squares >= KwMastSpec.MostSquare && mast.Sails.Count > mast.Squares)
                return "This mast carries all the sails it can: four square sails and one along the ship.";
            return "No room on the mast for another sail: raise the mast or shorten the sails.";
        }

        // A square sail on top of the stack: a little narrower and shorter than the one below it, as tall as the room left.
        private string AddSquare(KwMastSpec mast, float room)
        {
            int n = mast.Squares;
            KwSailSpec like = n > 0 ? mast.Sails[n - 1] : mast.Sails[0];
            float width = Snap(like.Width * (n > 0 ? Narrower : 1f));
            float most = Mathf.Floor(room / Step + 0.001f) * Step, height = Mathf.Min(Snap(like.Height * (n > 0 ? Shorter : 1f)), most);
            Sails.Insert(n, new KwSailSpec(0, KwSailSpec.MaxMast, width, height, KwSailCloth.ForNew(like.Cloth), 0f, 0, like.Pattern));
            Picked = n;
            string where = n > 0 ? $"on the {KwMastSpec.SquareName(n - 1).ToLowerInvariant()}'s yard" : "at the masthead";
            return height >= most - 0.001f ? $"{KwMastSpec.SquareName(n)} added {where}, {height:0.##} m: all the room the mast has left." : $"{KwMastSpec.SquareName(n)} added {where}.";
        }

        // A gaff sail behind the mast, as wide as most of the course (or the sail there), as tall as fits under it.
        private string AddAlong(KwMastSpec mast, float under)
        {
            KwSailSpec like = mast.Sails[0];
            float width = Snap(like.Width * 0.7f), height = Mathf.Min(Snap(like.Height), Mathf.Floor(under / Step + 0.001f) * Step);
            Sails.Add(new KwSailSpec(2, KwSailSpec.MaxMast, width, height, KwSailCloth.ForNew(like.Cloth), 0f, 0, like.Pattern));
            Picked = Sails.Count - 1;
            return mast.Squares >= KwMastSpec.MostSquare ? "The stack is full: gaff sail added behind the mast." : "No room for another square sail: gaff sail added behind the mast.";
        }

        /// <summary>Takes the picked sail off (a mast keeps one at least) and picks the one below it; what was done.</summary>
        public string Remove()
        {
            if (Sails.Count <= 1)
                return "A mast keeps one sail at least.";
            string name = Fitted.Name(Picked);
            Sails.RemoveAt(Picked);
            Picked = Mathf.Max(0, Picked - 1);
            return $"{name} taken off.";
        }

        // A size on the window's step, within a sail's sizes.
        private static float Snap(float size) => Mathf.Clamp(Mathf.Round(size / Step) * Step, KwSailSpec.MinSize, KwSailSpec.MaxSize);
    }
}
