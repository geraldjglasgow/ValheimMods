using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A damaged mast's fastenings and loose spars on its sail fitting (<see cref="KwSailDamageCloth"/>), drawn as
    /// <see cref="KwSailShapes"/> draws them whole but for what is broken: the rope rings lashing each square sail's head
    /// to its yard, those broken left out (<see cref="Lashings"/>); a sail along the ship's lacing to its boom, those broken
    /// left out, its boom notched or drawn only to where it broke, and its gaff (a metre long, stretched as it is hoisted)
    /// notched or drawn only to its break (<see cref="Along"/>). Whole again, each wears its own mesh.
    /// </summary>
    public static class KwSailDamageRings
    {
        /// <summary>Every square sail's lashings, the broken ones left out; the mast's own lashings when none is broken.</summary>
        public static void Lashings(KwSailDamageCloth cloth, KwRigPiece piece, KwRigCondition condition)
        {
            bool any = false;
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            for (int i = 0; i < piece.Sails.Count; i++)
            {
                KwSailLayout s = piece.Sails[i];
                if (!s.Square)
                    continue;
                bool[] gone = condition.Sail(piece.Index, i).Lashings;
                any |= System.Array.IndexOf(gone, true) >= 0;
                Bind(b, s, gone);
            }
            cloth.Wear(cloth.transform.Find(KwSailDress.LashingsName), any ? b.ToMesh(KwSailTool.Made + "_lashings_hurt") : null);
        }

        /// <summary>A square sail's lashing rings, as <see cref="KwSailShapes"/> binds them, but for those gone.</summary>
        public static void Bind(MeshBuilder b, KwSailLayout s, bool[] gone)
        {
            int count = Mathf.Max(3, Mathf.RoundToInt(s.Width / 0.45f));
            for (int k = 0; k < count; k++)
            {
                if (k < gone.Length && gone[k])
                    continue;
                float y = -s.Width / 2f + (k + 0.5f) * s.Width / count;
                KwRound.Ring(b, new Vector3(s.YardX, y, s.At - s.Spar * 0.35f), Vector3.left, s.Spar + 0.035f, 0.013f, 8, 4);
            }
        }

        /// <summary>A sail along the ship's lacing, boom and gaff as its damage leaves them.</summary>
        public static void Along(KwSailDamageCloth cloth, KwRigPiece piece, int i, KwSailHurt hurt, KwRigCondition condition)
        {
            KwSailLayout s = piece.Sails[i];
            Transform? boom = cloth.transform.Find("boom" + i);
            bool laced = System.Array.IndexOf(hurt.Lacing, true) >= 0;
            cloth.Wear(boom?.Find("lacing"), laced ? Lace(s, hurt.Lacing) : null);
            List<KwRigMark> booms = condition.Notches(piece.Index, KwRigSpar.Boom + i);
            KwRigSpar spar = KwRigPiece.BoomOf(s, i);
            float to = hurt.BoomBreak >= 0f ? hurt.BoomBreak : spar.Length;
            cloth.Wear(boom, booms.Count > 0 ? KwRigDamageSpar.Notched(spar, booms, 0f, to, false, hurt.BoomBreak >= 0f, i + 31) : null);
            if (s.Spec.Shape == 2)
                Gaff(cloth, piece, i, hurt, condition.Notches(piece.Index, KwRigSpar.Gaff + i));
        }

        // The boom's lacing rings, as KwSailShapes laces them, but for those gone.
        private static Mesh Lace(KwSailLayout s, bool[] gone)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            int k = 0;
            for (float x = -0.45f; x > -s.Width; x -= 0.5f, k++)
            {
                if (k >= gone.Length || !gone[k])
                    KwRound.Ring(b, new Vector3(x, 0f, s.At + s.Spar * 0.35f), Vector3.right, s.Spar + 0.03f, 0.013f, 8, 4);
            }
            return b.ToMesh(KwSailTool.Made + "_lacing_hurt");
        }

        // The gaff's metre-long mesh notched (its notches' places as shares of its length set full), or drawn to its break.
        private static void Gaff(KwSailDamageCloth cloth, KwRigPiece piece, int i, KwSailHurt hurt, List<KwRigMark> notches)
        {
            KwSailLayout s = piece.Sails[i];
            float length = Mathf.Max(KwRigPiece.GaffOf(s, i, 1f).Length, 0.1f);
            if (notches.Count == 0 && hurt.GaffBreak < 0f)
            {
                cloth.Wear(cloth.transform.Find(KwSailDress.GaffName + i), null);
                return;
            }
            KwRigSpar unit = new KwRigSpar(KwRigSpar.Gaff + i, Vector3.zero, new Vector3(1f, 0f, 0f), s.Spar * 0.9f, s.Spar * 0.6f);
            List<KwRigMark> shares = notches.ConvertAll(n => n.With(n.A / length, n.B, n.C, n.D / length));
            float to = hurt.GaffBreak >= 0f ? hurt.GaffBreak : 1f;
            cloth.Wear(cloth.transform.Find(KwSailDress.GaffName + i), KwRigDamageSpar.Notched(unit, shares, 0f, to, false, hurt.GaffBreak >= 0f, i + 57));
        }
    }
}
