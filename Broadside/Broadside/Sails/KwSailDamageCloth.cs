using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's mast's sails as its damage leaves them (<see cref="KwRigDamageLooks"/>), on the screens that draw
    /// them, on the mast's sail fitting (<see cref="KwShipFittings"/>): each sail's holes cut in its own copy of its
    /// material (<see cref="KwSailDamageHoles"/>), its cloth drooping where it is no longer fastened
    /// (<see cref="KwSailDamageDroop"/>), its yard's lashings and its boom's lacing drawn without the broken rings, and its
    /// boom and gaff notched or broken off (<see cref="KwSailDamageRings"/>). Made again only when the mast's marks change;
    /// mended, each part wears what it wore before.
    /// </summary>
    public sealed class KwSailDamageCloth : MonoBehaviour
    {
        private readonly Dictionary<int, (Renderer Cloth, Material Worn, Material? Holed)> holed = new Dictionary<int, (Renderer, Material, Material?)>();
        private readonly Dictionary<string, (MeshFilter Filter, Mesh Whole)> meshes = new Dictionary<string, (MeshFilter, Mesh)>();
        private KwRigPiece piece = null!;
        private string shown = "";

        /// <summary>Its mast's index in the ship's blueprint.</summary>
        public int Index => piece.Index;

        /// <summary>A mast's sails' damage looks on its sail fitting.</summary>
        public static KwSailDamageCloth Add(Transform fitting, KwRigPiece piece)
        {
            KwSailDamageCloth cloth = fitting.gameObject.AddComponent<KwSailDamageCloth>();
            cloth.piece = piece;
            return cloth;
        }

        /// <summary>The sails as the rig's condition leaves them, made again only when the mast's marks changed.</summary>
        public void Show(KwRigCondition condition)
        {
            string key = KwRigLookKey.Sails(condition, piece);
            if (key == shown)
                return;
            shown = key;
            for (int i = 0; i < piece.Sails.Count; i++)
            {
                KwSailHurt hurt = condition.Sail(piece.Index, i);
                Holes(i, condition.Holes(piece.Index, i));
                Droop(i, hurt);
                if (!piece.Sails[i].Square)
                    KwSailDamageRings.Along(this, piece, i, hurt, condition);
            }
            KwSailDamageRings.Lashings(this, piece, condition);
        }

        // A sail's holes cut in its own material, or its own material again with none.
        private void Holes(int i, List<KwRigMark> holes)
        {
            Renderer? cloth = transform.Find(KwSailDress.Cloth(i))?.GetComponent<Renderer>();
            if (cloth == null)
                return;
            if (!holed.TryGetValue(i, out (Renderer Cloth, Material Worn, Material? Holed) was))
                was = (cloth, cloth.sharedMaterial, null);
            Forget(was.Holed);
            Material? made = holes.Count > 0 ? KwSailDamageHoles.Holed(was.Worn, piece.Sails[i].Spec, holes) : null;
            cloth.sharedMaterial = made ?? was.Worn;
            holed[i] = (cloth, was.Worn, made);
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

        private static void Forget(Material? material)
        {
            if (material == null)
                return;
            Destroy(material.mainTexture);
            Destroy(material);
        }

        // Its own meshes and materials let go with the fitting (the parts' own owners let go what they wear then).
        private void OnDestroy()
        {
            foreach ((MeshFilter filter, Mesh whole) in meshes.Values)
            {
                if (filter != null && whole != null && filter.sharedMesh != whole)
                    Destroy(whole);
            }
            foreach ((Renderer _, Material _, Material? made) in holed.Values)
                Forget(made);
        }
    }
}
