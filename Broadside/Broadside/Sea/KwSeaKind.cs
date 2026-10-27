using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One kind of part edited on a launched ship where it floats (<see cref="KwSeaShip"/>): railings, doors and windows,
    /// ladders, the rig. It says which blueprint pieces are its own (<see cref="Owns"/>: only those may be added, changed
    /// and removed at sea), takes its hammer tools' clicks and hover on a launched ship (<see cref="Click"/>,
    /// <see cref="Hover"/>, through <see cref="KwSeaTools"/>), picks its part for the hammer's remove (<see cref="Pick"/>,
    /// <see cref="Removal"/>, through <see cref="KwSeaRemove"/>), checks and keeps its own ZDO state on the owner
    /// (<see cref="Check"/>, <see cref="Owner"/>), and redraws only its own layers on every peer (<see cref="Redraw"/>).
    /// One instance each in <see cref="KwSeaKinds.All"/>.
    /// </summary>
    public abstract class KwSeaKind
    {
        /// <summary>"rail", "opening", "ladder", "rig".</summary>
        public abstract string Name { get; }

        /// <summary>A piece this kind adds, changes, removes and redraws at sea.</summary>
        public abstract bool Owns(KwBpPiece piece);

        /// <summary>A hammer tool (its name in <see cref="KwToolPieces"/>) this kind takes on a launched ship.</summary>
        public abstract bool Serves(string tool);

        /// <summary>Whether a piece is left out of the ship's combined look, this kind drawing it alone (<see cref="Build"/>, <see cref="Redraw"/>).</summary>
        public virtual bool DrawsOwn(KwBpPiece piece) => false;

        /// <summary>Once, on every peer, when the ship has been built (its look made where there is a screen).</summary>
        public virtual void Build(KwSeaShip ship)
        {
        }

        /// <summary>The tool clicked aiming at a launched ship; true when taken (something said or opened).</summary>
        public abstract bool Click(Player player, string tool, KwSeaAim aim);

        /// <summary>Each frame the tool is held and aims at a launched ship: its ghost and words; true when it shows something.</summary>
        public virtual bool Hover(Player player, string tool, KwSeaAim aim) => false;

        /// <summary>The tool no longer aims at a launched ship, or is put away: its ghost and words cleared.</summary>
        public virtual void Unhover()
        {
        }

        /// <summary>Its part nearest the aim for the hammer's remove, with how far the aim is from it; null for none near.</summary>
        public virtual KwSeaPick? Pick(KwSeaAim aim) => null;

        /// <summary>The edit that removes a picked part.</summary>
        public virtual KwSeaEdit? Removal(KwSeaShip ship, KwSeaPick pick) => null;

        /// <summary>On the owner, before an edit with its pieces is applied: why not, or null.</summary>
        public virtual string? Check(KwSeaShip ship, KwSeaEdit edit) => null;

        /// <summary>Whether a changed or removed piece of its own is new wood, its index's marks dropped by the core (<see cref="KwSeaMarks"/>).</summary>
        public virtual bool NewWood(KwSeaChange change) => true;

        /// <summary>On the owner, the blueprint changed and not yet written: its own ZDO state kept right (numbering among the kind).</summary>
        public virtual void Owner(KwSeaShip ship, KwSeaChanges mine, ZDO zdo)
        {
        }

        /// <summary>
        /// The cost of an edit with its changes, as the whole blueprint's difference has it (<see cref="KwSeaCost.Delta"/>),
        /// set right where that is not what it costs (a fallen mast gone over the side gives nothing back): on the owner,
        /// before it answers, and on the asker's copy, before it sends. Nothing by default.
        /// </summary>
        public virtual void Cost(KwSeaShip ship, KwSeaChanges mine, List<(string Item, int Amount)> delta)
        {
        }

        /// <summary>On every peer: its layers for its changed pieces drawn and boxed again, nothing else.</summary>
        public abstract void Redraw(KwSeaShip ship, KwSeaChanges mine);

        /// <summary>On every peer, after every kind redrew: what it draws that hangs on other kinds' changes.</summary>
        public virtual void After(KwSeaShip ship, KwSeaChanges all)
        {
        }
    }

    /// <summary>A kind's part picked for removal: its index, a part of it (a railing's side or edge), how far the aim is from it.</summary>
    public sealed class KwSeaPick
    {
        public KwSeaPick(KwSeaKind kind, int index, int part, float distance, string what, Vector3 point) =>
            (Kind, Index, Part, Distance, What, Point) = (kind, index, part, distance, what, point);

        public KwSeaKind Kind { get; }
        public int Index { get; }
        public int Part { get; }

        /// <summary>From the aim's point to the part, metres (0: its own collider was hit).</summary>
        public float Distance { get; }

        /// <summary>What it is, as the player reads it: "railing".</summary>
        public string What { get; }

        /// <summary>Where on it (the ship's frame).</summary>
        public Vector3 Point { get; }
    }

    /// <summary>Every kind of part edited at sea, one line each.</summary>
    public static class KwSeaKinds
    {
        public static readonly IReadOnlyList<KwSeaKind> All = new KwSeaKind[]
        {
            new KwSeaRails(),
            new KwSeaOpenings(),
            new KwSeaLadders(),
            new KwSeaRig(),
        };

        /// <summary>The kind that owns a piece; null for a piece no kind edits at sea.</summary>
        public static KwSeaKind? Of(KwBpPiece piece)
        {
            foreach (KwSeaKind kind in All)
            {
                if (kind.Owns(piece))
                    return kind;
            }
            return null;
        }

        public static bool Owned(KwBpPiece piece) => Of(piece) != null;

        /// <summary>Whether a kind draws a piece alone, left out of the ship's combined look.</summary>
        public static bool DrawsOwn(KwBpPiece piece)
        {
            foreach (KwSeaKind kind in All)
            {
                if (kind.DrawsOwn(piece))
                    return true;
            }
            return false;
        }

        /// <summary>A kind by its name; null for none.</summary>
        public static KwSeaKind? Named(string name)
        {
            foreach (KwSeaKind kind in All)
            {
                if (kind.Name == name)
                    return kind;
            }
            return null;
        }
    }
}
