using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ship's owner taking a door or window edit at sea (<see cref="KwSeaOpenings"/>). Refused unless every planking
    /// part or cabin it touches is only changed (never added or removed at sea), stays where it stood, keeps its tier, still
    /// reads, and differs from what it was in its openings alone. Taken: the covered openings' states numbered again
    /// (<c>kw_ship_open&lt;n&gt;</c>, <see cref="KwShipOpeningPlan.States"/>: each keeps its own state, a new one is shut, the
    /// numbers no longer used cleared), and the holes in the wood an opening cut, changed or filled in takes away or makes new
    /// dropped (<see cref="Cut"/>), with their patches; every other hole, and the part's wear, stays (the part is the same
    /// wood, <see cref="KwSeaOpenings.NewWood"/>). Runs on the ship's owner; the tests are pure.
    /// </summary>
    public static class KwSeaOpeningOwner
    {
        /// <summary>How near a hole's middle is to a wall's face to be in that wall.</summary>
        private const float Face = 0.35f;

        /// <summary>Why an edit's planking and cabin changes are refused; null when they may go ahead.</summary>
        public static string? Check(KwBlueprint blueprint, KwSeaEdit edit)
        {
            foreach (KwSeaPart part in edit.Parts)
            {
                KwBpPiece? old = part.Index >= 0 && part.Index < blueprint.Pieces.Count ? blueprint.Pieces[part.Index] : null;
                bool mine = (old != null && KwSeaOpeningPart.Takes(old)) || (part.Piece != null && KwSeaOpeningPart.Takes(part.Piece));
                string? why = mine ? Changed(old, part.Piece) : null;
                if (why != null)
                    return why;
            }
            return null;
        }

        // Why a planking part's or cabin's change is refused (only its openings may change at sea).
        private static string? Changed(KwBpPiece? old, KwBpPiece? now)
        {
            if (old == null || now == null || !KwSeaOpeningPart.Takes(old) || !KwSeaOpeningPart.Takes(now))
                return "The hull's planking and cabins are built on the dry dock: only their doors and windows change at sea";
            if (old.Prefab != now.Prefab || (old.Position - now.Position).sqrMagnitude > 1e-6f || Mathf.Abs(Quaternion.Dot(old.Rotation, now.Rotation)) < 0.9999f)
                return "Only the part's doors and windows change at sea";
            if (KwBiomeTiers.Of(old.Data).Id != KwBiomeTiers.Of(now.Data).Id)
                return "Only the part's doors and windows change at sea";
            string? a = Rest(KwSeaOpeningPart.ToolOf(old)!, KwShipParts.Saved(old)), b = Rest(KwSeaOpeningPart.ToolOf(now)!, KwShipParts.Saved(now));
            return a == null || b == null ? "That part does not read" : a != b ? "Only the part's doors and windows change at sea" : null;
        }

        // A part's text without its openings, as its reader writes it back (null when it does not read).
        public static string? Rest(string tool, string saved)
        {
            string rest = KwOpeningText.Strip(saved, out _);
            if (tool == "planking")
                return KwSpan.TryParse(rest, out KwSpan span) ? span.ToString() : null;
            return KwCabinSpec.TryParse(rest, out KwCabinSpec spec) ? spec.ToString() : null;
        }

        /// <summary>On the owner, the blueprint changed: the openings' states numbered again and the holes the openings take dropped.</summary>
        public static void Owner(KwSeaShip ship, KwSeaChanges mine, ZDO zdo)
        {
            float waterline = KwShipTuning.Draught(ship.Hull);
            KwShipOpenings? openings = ship.GetComponent<KwShipOpenings>();
            KwShipOpeningPlan? was = openings != null ? openings.Plan : null;
            was ??= KwShipOpeningPlan.Of(Before(ship.Blueprint, mine), waterline);
            States(zdo, was, KwShipOpeningPlan.Of(ship.Blueprint, waterline, openings != null ? openings.Cache : null));
            KwSeaMarks.DropHoles(zdo, hole => Cut(ship.Keels, mine, hole));
        }

        // The blueprint as it was before the changes (a copy).
        private static KwBlueprint Before(KwBlueprint blueprint, KwSeaChanges changes)
        {
            KwBlueprint copy = KwSeaApply.Copy(blueprint);
            foreach (KwSeaChange change in changes.All)
                copy.Pieces[change.Index] = change.Old ?? KwSeaApply.Tombstone(change.New!);
            return copy;
        }

        /// <summary>The covered openings' states moved to their new numbers in the ZDO (each key written only when it changes).</summary>
        public static void States(ZDO zdo, KwShipOpeningPlan was, KwShipOpeningPlan now)
        {
            int[] old = new int[was.Covered];
            for (int n = 0; n < old.Length; n++)
                old[n] = zdo.GetInt(KwShipOpenings.Key(n));
            int[] moved = KwShipOpeningPlan.States(was, old, now);
            for (int n = 0; n < Mathf.Max(moved.Length, old.Length); n++)
            {
                int value = n < moved.Length ? moved[n] : 0;
                if (zdo.GetInt(KwShipOpenings.Key(n)) != value)
                    zdo.Set(KwShipOpenings.Key(n), value);
            }
        }

        /// <summary>
        /// Whether a hole is in wood an edit's openings take away or make new: in a changed part, on the wall of an opening cut,
        /// changed (as it was and as it is) or filled in, its middle within the opening's outline and frame. Pure.
        /// </summary>
        public static bool Cut(KwKeelLines keels, KwSeaChanges changes, KwHullHole hole)
        {
            foreach (KwSeaChange change in changes.All)
            {
                if (change.Index != hole.Part || change.Old == null || change.New == null || !KwSeaOpeningPart.Takes(change.Old))
                    continue;
                foreach ((KwSeaOpeningPart part, KwOpening o) in Touched(keels, change))
                {
                    if (In(part, o, hole.Place))
                        return true;
                }
            }
            return false;
        }

        // The openings a change cuts, changes or fills in, each with the part it is on as it then was (fitted to its wall).
        private static List<(KwSeaOpeningPart Part, KwOpening Opening)> Touched(KwKeelLines keels, KwSeaChange change)
        {
            KwSeaOpeningPart before = new KwSeaOpeningPart(change.Index, change.Old!, KwSeaOpeningPart.ToolOf(change.Old!)!, keels);
            KwSeaOpeningPart after = new KwSeaOpeningPart(change.Index, change.New!, KwSeaOpeningPart.ToolOf(change.New!)!, keels);
            List<(KwSeaOpeningPart, KwOpening)> touched = new List<(KwSeaOpeningPart, KwOpening)>();
            List<KwOpening> was = Fitted(before), now = Fitted(after);
            foreach (KwOpening o in was)
            {
                if (!now.Exists(n => Same(n, o)))
                    touched.Add((before, o));
            }
            foreach (KwOpening o in now)
            {
                if (!was.Exists(n => Same(n, o)))
                    touched.Add((after, o));
            }
            return touched;
        }

        // A part's openings, each fitted to its wall (those that fit one).
        private static List<KwOpening> Fitted(KwSeaOpeningPart part)
        {
            List<KwOpening> all = new List<KwOpening>();
            part.Cut.ForEach(w => all.AddRange(w.Openings));
            return all;
        }

        private static bool Same(KwOpening a, KwOpening b) => a.Id == b.Id && a.Wall == b.Wall && a.Shape == b.Shape && a.Frame == b.Frame
            && Mathf.Abs(a.U - b.U) < 1e-4f && Mathf.Abs(a.Sill - b.Sill) < 1e-4f && Mathf.Abs(a.Width - b.Width) < 1e-4f && Mathf.Abs(a.Height - b.Height) < 1e-4f;

        // Whether a point of the ship's frame lies in an opening's outline and frame, on its wall (near its face).
        private static bool In(KwSeaOpeningPart part, KwOpening o, Vector3 place)
        {
            KwOpeningSite? wall = part.Wall(o.Wall);
            if (wall == null || o.Surface == null)
                return false;
            Vector3 kit = part.Kit(place);
            float u = wall.AlongOf(kit);
            Vector3 outer = o.Surface.Point(u, kit.z, 0f), inner = o.Surface.Point(u, kit.z, -o.Surface.Thick);
            if (Mathf.Min(Vector3.Distance(outer, kit), Vector3.Distance(inner, kit)) > Face)
                return false;
            return KwOpeningAim.Inside(o.Outline(KwOpeningFrames.Width(o.Frame).Wide), new Vector2(u, kit.z));
        }
    }
}
