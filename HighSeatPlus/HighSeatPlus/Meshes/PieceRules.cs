using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>
    /// Which pieces may be combined at all, and the per-piece facts the checks compare against. A kind of piece
    /// qualifies (checked once on its prefab) when nothing on it moves it or changes how it looks on its own: it is made
    /// only of transforms, mesh filters and mesh renderers, LOD groups, colliders and the game scripts in
    /// <see cref="SafeScripts"/> (none of them moves the piece or switches its renderers after Awake). Anything else
    /// keeps the kind with the game: doors, chests, fires, lights, animators, particles, cloth, rigidbodies, ships and
    /// carts, signs, stations, item and armour stands, beds, wards, portals, the Ashlands stone's per-piece random tint
    /// (RandomMaterialValues), material swaps (MaterialVariation) and every script of another mod. The wet drip particles
    /// under WearNTear.m_wet are their own renderer, never combined, and allowed. A live piece must also be a scene root
    /// (a piece parented to a ship or a mod's raft moves with it), flagged static by its WearNTear and not the
    /// placement ghost. A piece's look also changes through MaterialMan, which puts per-piece shader values on every
    /// renderer of the piece: the build highlight (_Color, _EmissionColor), the burn and lava glow
    /// (_TakingAshlandsDamage) and snow (_SnowLevel). A piece with any such value is drawn by the game until it is gone;
    /// a burn glow that has faded back to 0 does not count. A property block that MaterialMan did not set belongs to
    /// another mod and keeps the piece with the game too.
    /// </summary>
    internal static class PieceRules
    {
        private static readonly List<Component> Components = new List<Component>();
        private static readonly HashSet<Type> SafeScripts = new HashSet<Type>
        {
            typeof(ZNetView), typeof(Piece), typeof(WearNTear), typeof(Chair), typeof(Ladder), typeof(GuidePoint),
            typeof(StationExtension), typeof(EffectArea), typeof(HoverText), typeof(DisableInPlacementGhost),
            typeof(RandomPieceRotation), typeof(LodFadeInOut), typeof(AutoJumpLedge), typeof(Ledge)
        };
        private static bool _resolved;

        /// <summary>The checks on a live piece that its prefab can not answer.</summary>
        public static Verdict CheckPiece(WearNTear piece)
        {
            if (piece.transform.parent != null || !piece.m_staticPosition || Player.IsPlacementGhost(piece.gameObject))
            {
                return Verdict.Never;
            }
            return LooksChanged(piece) ? Verdict.Later : Verdict.Combine;
        }

        /// <summary>Whether this piece kind is made only of parts that keep still and keep their look.</summary>
        public static bool OnlySafeComponents(GameObject root)
        {
            Resolve();
            WearNTear piece = root.GetComponent<WearNTear>();
            Transform wet = piece != null && piece.m_wet != null ? piece.m_wet.transform : null;
            root.GetComponentsInChildren(true, Components);
            bool safe = piece != null;
            for (int i = 0; i < Components.Count && safe; i++)
            {
                Component component = Components[i];
                safe = component != null && (Safe(component) || (wet != null && component.transform.IsChildOf(wet)));
            }
            Components.Clear();
            return safe;
        }

        /// <summary>A snow overlay, the wet drips or a renderer the piece itself marks as not solid: left to the game.</summary>
        public static bool IsOverlay(WearNTear piece, MeshRenderer renderer)
        {
            if (renderer == piece.m_snow || renderer == piece.m_snowWorn || renderer == piece.m_snowBroken)
            {
                return true;
            }
            if (piece.m_wet != null && renderer.transform.IsChildOf(piece.m_wet.transform))
            {
                return true;
            }
            return piece.m_nonSolidRenderers != null && piece.m_nonSolidRenderers.Contains(renderer);
        }

        /// <summary>Which of the new, worn and broken looks are switched on.</summary>
        public static int StateBits(WearNTear piece)
        {
            int bits = 0;
            if (piece.m_new != null && piece.m_new.activeSelf)
            {
                bits |= 1;
            }
            if (piece.m_worn != null && piece.m_worn.activeSelf)
            {
                bits |= 2;
            }
            if (piece.m_broken != null && piece.m_broken.activeSelf)
            {
                bits |= 4;
            }
            return bits;
        }

        /// <summary>True while MaterialMan holds a per-piece shader value that changes how the piece looks.</summary>
        public static bool LooksChanged(WearNTear piece)
        {
            MaterialMan man = MaterialMan.instance;
            if (man == null || !man.m_blocks.TryGetValue(piece.gameObject.GetInstanceID(), out MaterialMan.PropertyContainer block))
            {
                return false;
            }
            foreach (KeyValuePair<int, MaterialMan.ShaderPropertyBase> property in block.m_shaderProperties)
            {
                if (property.Key != WearNTear.s_AshlandsDamageShaderID || !(property.Value is MaterialMan.ShaderProperty<float> value) || value.Get() != 0f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>A property block on this renderer that MaterialMan did not put there.</summary>
        public static bool ForeignBlock(WearNTear piece, Renderer renderer)
        {
            if (!renderer.HasPropertyBlock())
            {
                return false;
            }
            MaterialMan man = MaterialMan.instance;
            return man == null || !man.m_blocks.ContainsKey(piece.gameObject.GetInstanceID());
        }

        private static bool Safe(Component component)
        {
            if (component is Transform || component is MeshFilter || component is MeshRenderer || component is LODGroup || component is Collider)
            {
                return true;
            }
            return SafeScripts.Contains(component.GetType());
        }

        // The editor's mesh combining helper sits on the switched-off children Iron Gate combined pieces from; it has no
        // Awake, Start or Update, so it never runs in the game.
        private static void Resolve()
        {
            if (_resolved)
            {
                return;
            }
            _resolved = true;
            Type helper = Type.GetType("SimpleMeshCombine, assembly_simplemeshcombine", false);
            if (helper != null)
            {
                SafeScripts.Add(helper);
            }
        }
    }
}
