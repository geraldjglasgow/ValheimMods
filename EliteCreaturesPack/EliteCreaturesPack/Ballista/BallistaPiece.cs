using System;
using System.Linq;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The Bone Ballista piece: a copy of the game's ballista (`piece_turret`), so it keeps its placement, its tab in the
    /// hammer (Misc), the workbench it is built at, its wear, hit and destroy effects and its player-base area. The
    /// game's turret brain goes (it aimed and shot by itself) and the player's hold takes its place
    /// (<see cref="BallistaControl"/>, drawn by <see cref="BallistaLook"/>). Its look, colliders and range marker go;
    /// the bundle's ballista comes in, hung into its turning frame (<see cref="BallistaFrame"/>), dressed in the game
    /// ballista's body material, with two missiles of its own: the one laid in the groove and the one in the holder's
    /// hand. Half the game ballista's health: it is half its size, of bone. Built once, under the bench.
    /// </summary>
    public static class BallistaPiece
    {
        public const string PrefabName = "ECP_BoneBallista";
        public const string Word = "piece_ecp_boneballista";
        public const string GamePiece = "piece_turret";
        private const string BodyRenderer = "Base", Model = "ecp_bone_ballista", Icon = "ecp_bone_ballista_icon", Keep = "PlayerBase";
        private const float Health = 200f;

        public static GameObject? Prefab { get; private set; }

        public static void Build(ZNetScene scene, AssetBundle bundle, GameObject missile)
        {
            GameObject? game = scene.GetPrefab(GamePiece);
            if (game == null || game.GetComponent<Turret>() == null || game.GetComponent<Piece>() == null)
            {
                Log.Error($"Bone ballista not built: the game has no {GamePiece} with its turret.");
                return;
            }
            Material? body = GameMaterials.Borrow(game, BodyRenderer);
            GameObject piece = PrefabBench.Copy(game, PrefabName);
            Object.DestroyImmediate(piece.GetComponent<Turret>());
            Strip(piece);
            GameObject model = Wear(piece, EmbeddedBundle.Prefab(bundle, Model), missile, body);
            Describe(piece.GetComponent<Piece>(), bundle);
            Point(piece.GetComponent<WearNTear>(), model);
            piece.AddComponent<BallistaControl>();
            piece.AddComponent<BallistaLook>();
            Prefab = piece;
        }

        /// <summary>
        /// Everything the game ballista drew or collided with: its children but the player-base area (its models, the
        /// range marker), the root's drawing parts and colliders.
        /// </summary>
        private static void Strip(GameObject piece)
        {
            foreach (Transform child in piece.transform.Cast<Transform>().ToArray())
            {
                if (child.name != Keep)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
            foreach (Type kind in new[] { typeof(LODGroup), typeof(Renderer), typeof(MeshFilter), typeof(Collider) })
            {
                foreach (Component part in piece.GetComponents(kind))
                {
                    Object.DestroyImmediate(part);
                }
            }
        }

        /// <summary>Our ballista in, hung into its frame, with its two missiles, dressed and on the piece's layer.</summary>
        private static GameObject Wear(GameObject piece, GameObject bundleModel, GameObject missile, Material? body)
        {
            GameObject model = Object.Instantiate(bundleModel, piece.transform, false);
            model.name = "ballista";
            foreach (Animator animator in model.GetComponentsInChildren<Animator>(true))
            {
                Object.DestroyImmediate(animator);
            }
            if (BallistaFrame.Assemble(model.transform))
            {
                Missiles(model.transform, missile);
            }
            BallistaDress.Dress(model, body);
            BallistaDress.Layer(model, piece.layer);
            return model;
        }

        /// <summary>The missile laid in the groove (on the seat, along the stock) and the one the holder carries to it.</summary>
        private static void Missiles(Transform model, GameObject missile)
        {
            Transform? seat = GameMaterials.Find(model, BallistaFrame.Seat);
            Transform? pitch = GameMaterials.Find(model, BallistaFrame.Pitch);
            if (seat == null || pitch == null)
            {
                return;
            }
            GameObject laid = Copy(missile, pitch, BallistaParts.LaidMissile);
            (laid.transform.position, laid.transform.rotation) = (seat.position, pitch.rotation);
            Copy(missile, model, BallistaParts.HeldMissile);
        }

        /// <summary>A still copy of the missile's model: no colliders, hidden until drawn.</summary>
        private static GameObject Copy(GameObject missile, Transform parent, string name)
        {
            GameObject copy = Object.Instantiate(missile, parent, false);
            copy.name = name;
            foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }
            copy.SetActive(false);
            return copy;
        }

        private static void Describe(Piece piece, AssetBundle bundle)
        {
            piece.m_name = "$" + Word;
            piece.m_description = "$" + Word + "_description";
            piece.m_enabled = true;
            Sprite? icon = bundle.LoadAsset<Sprite>(Icon);
            if (icon != null)
            {
                piece.m_icon = icon;
            }
            else
            {
                Log.Warn($"Bone ballista: the bundle has no {Icon}; it shows the game ballista's icon.");
            }
        }

        /// <summary>One model for every wear state; no fragment roots (they were the stripped models).</summary>
        private static void Point(WearNTear wear, GameObject model)
        {
            (wear.m_new, wear.m_worn, wear.m_broken, wear.m_wet) = (model, model, model, null);
            wear.m_fragmentRoots = null;
            wear.m_health = Health;
        }
    }
}
