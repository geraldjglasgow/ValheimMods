using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A mast and the sails it carries, as the sail tool made it, saved in its piece's ZDO (<see cref="KwGenerated.Key"/>)
    /// as "mast|height|sail|sail|...", each sail "shape,width,height,cloth" (<see cref="KwSailSpec.Part"/>): square sails
    /// stacked one above another, each on its own yard (the course lowest, then the topsail, the topgallant and the royal,
    /// at most <see cref="MostSquare"/>), and at most one sail along the ship (a triangle or a gaff sail) behind the mast.
    /// The sails are always placed to fit the mast (<see cref="KwSailFit"/>), the same on every peer from the text alone. A
    /// sail saved alone before masts carried several ("sail|shape|mast|width|height|cloth") reads as a mast with that one
    /// sail, where it was. After the sails come what else the mast carries (<see cref="KwMastTops"/>: "nest,height",
    /// "top,square,height", "pennant,colour"); a mast saved before them carries none.
    /// </summary>
    public sealed class KwMastSpec
    {
        public const int MostSquare = 4;
        private const string Tag = "mast";
        private static readonly string[] SquareNames = { "Course", "Topsail", "Topgallant", "Royal" };

        internal KwMastSpec(float mast, List<KwSailSpec> sails, string note, KwMastTops tops = default)
        {
            Mast = mast;
            Sails = sails;
            Note = note;
            Tops = tops;
        }

        public float Mast { get; }

        /// <summary>The sails, placed: the square ones from the course up, then the one along the ship.</summary>
        public IReadOnlyList<KwSailSpec> Sails { get; }

        /// <summary>What placing the sails had to change to make them fit (a sail cut, the mast raised); empty when nothing.</summary>
        public string Note { get; }

        /// <summary>Its crow's nest, fighting top and pennant (<see cref="KwMastTops"/>), their heights kept on the mast.</summary>
        public KwMastTops Tops { get; }

        /// <summary>The same mast and sails carrying these platforms and pennant, their heights kept on the mast.</summary>
        public KwMastSpec With(KwMastTops tops) => new KwMastSpec(Mast, new List<KwSailSpec>(Sails), Note, tops.Fitted(Mast));

        /// <summary>How many square sails the mast carries.</summary>
        public int Squares
        {
            get
            {
                int count = 0;
                foreach (KwSailSpec sail in Sails)
                    count += sail.Square ? 1 : 0;
                return count;
            }
        }

        /// <summary>Every sail's power together: area times cloth drive, for the ship's push (<see cref="KwBpBow.Pull"/>).</summary>
        public float Power
        {
            get
            {
                float power = 0f;
                foreach (KwSailSpec sail in Sails)
                    power += sail.Power;
                return power;
            }
        }

        /// <summary>The sails placed to fit a mast of that height (it may be raised to carry them).</summary>
        public static KwMastSpec Fit(float mast, IEnumerable<KwSailSpec> asked) => KwSailFit.Fit(mast, asked);

        /// <summary>A mast carrying one sail, where a lone sail hangs.</summary>
        public static KwMastSpec Lone(KwSailSpec sail) => Fit(sail.Mast, new[] { sail });

        /// <summary>A sail's name on its mast: the square ones by their place in the stack, the others by their shape.</summary>
        public string Name(int index)
        {
            KwSailSpec sail = Sails[index];
            return sail.Square ? SquareName(sail.Tier) : sail.Shape == 1 ? "Triangle sail" : "Gaff sail";
        }

        /// <summary>A square sail's name by its place in the stack: the course, the topsail, the topgallant, the royal.</summary>
        public static string SquareName(int tier) => SquareNames[Mathf.Clamp(tier, 0, SquareNames.Length - 1)];

        /// <summary>A sail set above the lowest: a square sail over the course, set only at full sail on a ship.</summary>
        public bool Upper(int index) => Sails[index].Square && Sails[index].Tier > 0;

        public override string ToString()
        {
            List<string> parts = new List<string> { Tag, KwSailSpec.F(Mast) };
            foreach (KwSailSpec sail in Sails)
                parts.Add(sail.Part());
            parts.AddRange(Tops.Parts());
            return string.Join("|", parts);
        }

        /// <summary>A mast from its saved text, or from a lone sail's saved before masts carried several; false when neither reads.</summary>
        public static bool TryParse(string text, out KwMastSpec mast)
        {
            mast = null!;
            if (KwSailSpec.TryParse(text, out KwSailSpec lone))
            {
                mast = Lone(lone);
                return true;
            }
            string[] p = (text ?? "").Split('|');
            if (p.Length < 3 || p[0] != Tag || !KwSailSpec.Num(p[1], out float height) || height < KwSailSpec.MinMast - 0.01f || height > KwSailSpec.MaxMast + 0.01f)
                return false;
            List<KwSailSpec> sails = new List<KwSailSpec>();
            KwMastTops tops = default;
            for (int i = 2; i < p.Length; i++)
            {
                if (KwMastTops.TryRead(p[i], ref tops))
                    continue;
                if (!KwSailSpec.TryPart(p[i], out KwSailSpec sail))
                    return false;
                sails.Add(sail);
            }
            mast = sails.Count > 0 ? Fit(height, sails).With(tops) : null!;
            return sails.Count > 0;
        }
    }
}
