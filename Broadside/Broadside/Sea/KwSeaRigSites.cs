using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>A keel's post top on a launched ship, in its frame: where a post cap or a bowsprit stands.</summary>
    public sealed class KwSeaPost
    {
        public KwSeaPost(int keel, Vector3 top, Vector3 axis, KwKeelBody body, KwBiomeTier tier) =>
            (Keel, Top, Axis, Body, Tier) = (keel, top, axis, body, tier);

        /// <summary>The keel's index in the blueprint.</summary>
        public int Keel { get; }

        public Vector3 Top { get; }

        /// <summary>The keel's way along: its piece's forward.</summary>
        public Vector3 Axis { get; }

        public KwKeelBody Body { get; }
        public KwBiomeTier Tier { get; }

        /// <summary>The post's section across the ship and along the keel.</summary>
        public float Width => Body.PostHalf * 2f;
        public float Depth => Body.PostDepth;
    }

    /// <summary>
    /// Where the rig's parts stand on a launched ship, from its blueprint alone (plain maths, the same on every peer): each
    /// keel's post top (<see cref="Posts"/>, placed as the keel's "Post top" snap point is on the dock), the post a ray aims
    /// at, the cap or bowsprit already on a post, and the faces a mast stands on (a deck's, a hold floor's, a keel's or a
    /// cabin roof's top).
    /// </summary>
    public static class KwSeaRigSites
    {
        /// <summary>A piece on a post stands within this of its top; a ray passing within <see cref="Ray"/> of a top aims at it.</summary>
        public const float Near = 0.2f, Ray = 0.6f;

        private const string PostTop = "Post top";

        // The last blueprint's posts (asked each frame while the Post cap tool aims at a ship); keels never change at sea.
        private static (KwBlueprint? Blueprint, int Count, List<KwSeaPost> Posts) last = (null, -1, new List<KwSeaPost>());

        /// <summary>Every keel's post top in a blueprint, in its frame.</summary>
        public static List<KwSeaPost> Posts(KwBlueprint blueprint)
        {
            if (last.Blueprint == blueprint && last.Count == blueprint.Pieces.Count)
                return last.Posts;
            List<KwSeaPost> posts = new List<KwSeaPost>();
            for (int i = 0; i < blueprint.Pieces.Count; i++)
            {
                KwSeaPost? post = Post(blueprint.Pieces[i], i);
                if (post != null)
                    posts.Add(post);
            }
            last = (blueprint, blueprint.Pieces.Count, posts);
            return posts;
        }

        // A keel's post top, placed as KwSnaps places the snap point on the piece; null for any other piece, or a keel with no post.
        private static KwSeaPost? Post(KwBpPiece piece, int index)
        {
            if (piece.Prefab != KwKeel.MadeName || !KwKeelSpec.TryParse(KwShipParts.Saved(piece), out KwKeelSpec spec))
                return null;
            KwDef def = KwKeel.Custom(spec);
            Vector3 pivot = new Vector3(def.Pivot.x, 0f, def.Pivot.y);
            foreach ((string name, Vector3 kit) in def.Snaps)
            {
                if (name == PostTop)
                    return new KwSeaPost(index, piece.Position + piece.Rotation * KwMesh.ToUnity(kit - pivot), piece.Rotation * Vector3.forward, spec.Body, KwBiomeTiers.Of(piece.Data));
            }
            return null;
        }

        /// <summary>
        /// The post top a ray (from, a unit way) passes nearest, within <see cref="Ray"/> of it, ahead and not behind what the
        /// ray met first (hit: how far along that is); null for none.
        /// </summary>
        public static KwSeaPost? Aimed(KwBlueprint blueprint, Vector3 from, Vector3 way, float hit)
        {
            KwSeaPost? best = null;
            float bestOff = Ray;
            foreach (KwSeaPost post in Posts(blueprint))
            {
                float t = Vector3.Dot(post.Top - from, way), off = (from + way * t - post.Top).magnitude;
                if (t > 0f && t < hit + 1f && off < bestOff)
                    (best, bestOff) = (post, off);
            }
            return best;
        }

        /// <summary>The post top a piece stands on (within <see cref="Near"/> of its place); null for none.</summary>
        public static KwSeaPost? Under(KwBlueprint blueprint, Vector3 place)
        {
            foreach (KwSeaPost post in Posts(blueprint))
            {
                if ((post.Top - place).sqrMagnitude < Near * Near)
                    return post;
            }
            return null;
        }

        /// <summary>The index of a piece of a kind standing on a post's top (its place within <see cref="Near"/>), but one; -1 for none.</summary>
        public static int On(KwBlueprint blueprint, Vector3 top, Func<string, bool> kind, int but = -1)
        {
            for (int i = 0; i < blueprint.Pieces.Count; i++)
            {
                KwBpPiece piece = blueprint.Pieces[i];
                if (i != but && kind(piece.Prefab) && (piece.Position - top).sqrMagnitude < Near * Near)
                    return i;
            }
            return -1;
        }

        /// <summary>Whether a mast may stand where the aim met the ship: on the top of a deck, a hold floor, a keel or a cabin's roof.</summary>
        public static bool Deck(string name, Vector3 normal) =>
            (name == KwShipColliders.DeckName || name == KwShipHouses.RoofBox) && normal.y > 0.7f;
    }
}
