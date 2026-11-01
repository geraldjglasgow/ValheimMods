using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's mast's sails as its damage leaves them (<see cref="KwRigDamageLooks"/>), on the screens that draw
    /// them, on the mast's sail fitting (<see cref="KwShipFittings"/>): each sail's holes cut in its own copy of its
    /// material (<see cref="KwSailHoling"/>, the cut off the main thread), its cloth drooping where it is no longer fastened
    /// (<see cref="KwSailDamageDroop"/>), its yard's lashings and its boom's lacing drawn without the broken rings, and its
    /// boom and gaff notched or broken off (<see cref="KwSailDamageRings"/>). Made again only when the mast's marks change,
    /// a sail a step (<see cref="Step"/>), and a sail's holes only when its own changed; mended, each part wears what it
    /// wore before.
    /// </summary>
    public sealed class KwSailDamageCloth : MonoBehaviour
    {
        private readonly KwSailHoling holing = new KwSailHoling();
        private readonly Dictionary<string, (MeshFilter Filter, Mesh Whole)> meshes = new Dictionary<string, (MeshFilter, Mesh)>();
        private KwRigPiece piece = null!;

        // The mast's marks all its sails show (null while a pass shows them, sail by sail, the marks it is for).
        private string? shown = "";
        private string showing = "";
        private int next;

        /// <summary>Its mast's index in the ship's blueprint.</summary>
        public int Index => piece.Index;

        /// <summary>A mast's sails' damage looks on its sail fitting.</summary>
        public static KwSailDamageCloth Add(Transform fitting, KwRigPiece piece)
        {
            KwSailDamageCloth cloth = fitting.gameObject.AddComponent<KwSailDamageCloth>();
            cloth.piece = piece;
            return cloth;
        }

        /// <summary>
        /// One step of showing the sails as the rig's condition leaves them: a sail (its holes, which may take a few steps
        /// while they are cut, its droop and its fastenings along the ship), then the lashings; true once all are shown, at
        /// once when the mast's marks are the ones shown. Marks that change on the way start it again from the first sail
        /// (a sail whose holes did not change keeps its texture).
        /// </summary>
        public bool Step(KwRigCondition condition)
        {
            string key = KwRigLookKey.Sails(condition, piece);
            if (key == shown)
                return true;
            if (shown != null || key != showing)
                (shown, showing, next) = (null, key, 0);
            if (next < piece.Sails.Count)
            {
                if (Sail(next, condition))
                    next++;
                return false;
            }
            KwSailDamageRings.Lashings(this, piece, condition);
            shown = showing;
            return true;
        }

        // Sail i as the condition leaves it: its holes (false while they are cut), its droop and its fastenings along the ship.
        private bool Sail(int i, KwRigCondition condition)
        {
            if (!holing.Wear(transform, i, piece.Sails[i].Spec, condition.Holes(piece.Index, i)))
                return false;
            KwSailHurt hurt = condition.Sail(piece.Index, i);
            Droop(i, hurt);
            if (!piece.Sails[i].Square)
                KwSailDamageRings.Along(this, piece, i, hurt, condition);
            return true;
        }

        // A sail drooping where it is no longer fastened, or set as the wind has it.
        private void Droop(int i, KwSailHurt hurt)
        {
            KwSailMotion? motion = transform.Find(KwSailDress.Cloth(i))?.GetComponent<KwSailMotion>();
            if (motion != null)
                motion.Bend = hurt.Bent ? new KwSailDamageDroop(piece.Sails[i], hurt) : null;
        }

        /// <summary>A part's mesh in place of its own (null: its own again); the one made before it let go.</summary>
        public void Wear(Transform? part, Mesh? mesh)
        {
            MeshFilter? filter = part != null ? part.GetComponent<MeshFilter>() : null;
            if (filter == null)
                return;
            string key = part!.GetInstanceID().ToString();
            if (!meshes.TryGetValue(key, out (MeshFilter Filter, Mesh Whole) was))
                meshes[key] = was = (filter, filter.sharedMesh);
            if (filter.sharedMesh != was.Whole)
                Destroy(filter.sharedMesh);
            filter.sharedMesh = mesh != null ? mesh : was.Whole;
        }

        // Its own meshes and materials let go with the fitting (the parts' own owners let go what they wear then).
        private void OnDestroy()
        {
            foreach ((MeshFilter filter, Mesh whole) in meshes.Values)
            {
                if (filter != null && whole != null && filter.sharedMesh != whole)
                    Destroy(whole);
            }
            holing.Forget();
        }
    }
}
