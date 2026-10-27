using System;
using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// What a Doorway or Window tool's window would do at OK (<see cref="KwDoorwayPlan"/>, <see cref="KwWindowPlan"/>): every
    /// part it changes (<see cref="KwPartCut"/>) with its openings as they would be, how many openings it cuts, changes,
    /// fills in, skips (a fill's places that do not fit, <see cref="KwOpeningFill"/>) and refuses, the first reason given, and lines for the window's note.
    /// Pure.
    /// </summary>
    public sealed class KwOpeningPlan
    {
        public List<KwPartCut> Parts { get; } = new List<KwPartCut>();
        public int Made { get; set; }
        public int Changed { get; set; }
        public int Filled { get; set; }
        public int Skipped { get; private set; }
        public int Refused { get; private set; }
        public string Why { get; private set; } = "";
        public string SkipWhy { get; private set; } = "";
        public List<string> Notes { get; } = new List<string>();

        /// <summary>Whether it would change anything at all.</summary>
        public bool Any => Parts.Exists(p => p.Changed);

        /// <summary>The part a site is on, as this plan changes it (added the first time).</summary>
        public KwPartCut Part(KwOpeningSite site)
        {
            KwPartCut? cut = Parts.Find(p => p.Same(site));
            if (cut == null)
                Parts.Add(cut = new KwPartCut(site));
            return cut;
        }

        /// <summary>An opening that cannot be cut or changed, and why (the first reason kept).</summary>
        public void Refuse(string why)
        {
            if (Refused++ == 0)
                Why = why;
        }

        /// <summary>A place of a fill left out, and why (the first reason kept).</summary>
        public void Skip(string why)
        {
            if (Skipped++ == 0)
                SkipWhy = why;
        }

        /// <summary>Everything planned so far dropped (an opening refused that another depends on).</summary>
        public void Drop()
        {
            Parts.Clear();
            (Made, Changed, Filled) = (0, 0, 0);
        }
    }

    /// <summary>A part a plan changes: its site, its openings as they would be, and the openings new or changed among them (with their walls).</summary>
    public sealed class KwPartCut
    {
        public KwPartCut(KwOpeningSite site)
        {
            Site = site;
            Openings = new List<KwOpening>(site.Openings);
        }

        public KwOpeningSite Site { get; }
        public List<KwOpening> Openings { get; }

        /// <summary>The openings cut or changed, each with the site of its own wall: what the preview draws.</summary>
        public List<(KwOpeningSite Site, KwOpening Opening)> Shown { get; } = new List<(KwOpeningSite, KwOpening)>();

        public int Filled { get; private set; }

        public bool Changed => Shown.Count > 0 || Filled > 0;

        /// <summary>Whether a site is on this part (the same part in the world; in a test, the same <see cref="KwOpeningSite.Of"/>).</summary>
        public bool Same(KwOpeningSite site) => ReferenceEquals(site.Of ?? site, Site.Of ?? Site);

        /// <summary>A new opening on a wall of the part.</summary>
        public void Add(KwOpeningSite on, KwOpening o)
        {
            Openings.Add(o);
            Shown.Add((on, o));
        }

        /// <summary>An opening of the part changed (by its number).</summary>
        public void Put(KwOpeningSite on, KwOpening now)
        {
            int at = Openings.FindIndex(o => o.Id == now.Id);
            if (at >= 0)
                Openings[at] = now;
            else
                Openings.Add(now);
            Shown.Add((on, now));
        }

        /// <summary>An opening of the part filled in (the wall whole again).</summary>
        public void Fill(KwOpening was)
        {
            if (Openings.RemoveAll(o => o.Id == was.Id) > 0)
                Filled++;
        }
    }

    /// <summary>
    /// Another part in line with the part clicked (the same keel line, the same side of the ship), as its own site, and how
    /// a place on the part clicked reads on it: along (u there = A + B·u here, B ±1 as it runs either way) and up (z there
    /// = z here + Dz, every part standing upright). Pure.
    /// </summary>
    public sealed class KwPartMap
    {
        public KwPartMap(KwOpeningSite site, float a, float b, float dz)
        {
            (Site, A, B, Dz) = (site, a, b, dz);
        }

        public KwOpeningSite Site { get; }
        public float A { get; }
        public float B { get; }
        public float Dz { get; }

        public float U(float u) => A + B * u;

        /// <summary>A place along this part as the part clicked reads it.</summary>
        public float Back(float u) => (u - A) / B;

        public float Z(float z) => z + Dz;

        /// <summary>The part clicked itself.</summary>
        public static KwPartMap Same(KwOpeningSite site) => new KwPartMap(site, 0f, 1f, 0f);
    }

    /// <summary>What a plan asks of the world: whether an opening on planking cuts through a deck inside, and whether its sill is under the waterline (none: never).</summary>
    public sealed class KwOpeningWorld
    {
        public Func<KwOpeningSite, KwOpening, bool>? Crosses { get; set; }
        public Func<KwOpeningSite, KwOpening, bool>? Below { get; set; }
    }

    /// <summary>A window an edit changes: its site (its part and wall), the opening, the deck inside under it (none: null), and its own values.</summary>
    public sealed class KwOpeningTarget
    {
        public KwOpeningTarget(KwOpeningSite site, KwOpening opening, float? floor)
        {
            (Site, Opening, Floor) = (site, opening, floor);
            Values = KwOpeningAsk.Of(opening, floor).Values(opening.Sill - (floor ?? 0f));
        }

        public KwOpeningSite Site { get; }
        public KwOpening Opening { get; }
        public float? Floor { get; }
        public KwPartValues Values { get; }
    }
}
