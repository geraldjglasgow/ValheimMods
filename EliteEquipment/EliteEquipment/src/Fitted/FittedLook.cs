using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EliteEquipment.Boots;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// Fitted leggings while Separate Boots is on: the game's own iron and bronze trousers let the skin through when the
    /// legs move (or are only paint), so a player wearing them gets leggings made from their own body instead, in the
    /// look of their set (<see cref="LegStyles"/>, <see cref="FittedBuild"/>). The game attaches its trousers as always;
    /// they are hidden, the fitted leggings are drawn on the body's bones beside the body, and the body draws without the
    /// triangles under them (its model's mesh swapped, so the game keeps it), so no skin can show through. Made again
    /// when the leggings, the boots (their height sets the lower end) or the body model change; everything back when the
    /// leggings come off or the switch goes off. A chest that lay on the game's baggy trousers is drawn in over them while
    /// they are on (<see cref="ChestFit"/>). Players only (armour stands keep the game's look), drawn by every client
    /// from what the game already replicates; nothing on a dedicated server.
    /// </summary>
    public static class FittedLook
    {
        private static readonly bool headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        private static readonly ConditionalWeakTable<VisEquipment, Worn> worn = new ConditionalWeakTable<VisEquipment, Worn>();

        /// <summary>After every visuals update (<see cref="BootsShow"/>): made again when the boots or the body model changed.</summary>
        public static void Check(VisEquipment vis, int bootsHash)
        {
            if (worn.TryGetValue(vis, out Worn state) && (state.Boots != bootsHash || state.Index != vis.m_currentModelIndex))
            {
                Refresh(vis, bootsHash);
                vis.UpdateLodgroup();
            }
        }

        private static void Refresh(VisEquipment vis, int bootsHash)
        {
            Remove(vis);
            LegStyle style = Wanted(vis) ? LegStyles.ByLegsHash(vis.m_currentLegItemHash) : null;
            if (style == null)
                return;
            int index = vis.m_currentModelIndex;
            string[] bones = Array.ConvertAll(vis.m_bodyModel.bones, bone => bone != null ? bone.name : null);
            FittedParts parts = FittedBuild.For(style, vis.m_models[index].m_mesh, bones, BootSets.ByHash(bootsHash));
            Material material = parts != null ? FittedMaterial.Get(style) : null;
            if (material != null)
                Wear(vis, style, parts, material, bootsHash);
        }

        /// <summary>A new chest was attached (<see cref="ChestFit"/>): drawn in over the fitted leggings when they are on.</summary>
        internal static void ChestChanged(VisEquipment vis)
        {
            if (worn.TryGetValue(vis, out Worn state))
                ChestFit.Fit(vis, Surface(vis, state.Body), state.Style.Thickness);
        }

        private static BodySurface Surface(VisEquipment vis, Mesh body) =>
            FittedBuild.Surface(body, Array.ConvertAll(vis.m_bodyModel.bones, bone => bone != null ? bone.name : null));

        private static bool Wanted(VisEquipment vis) =>
            !headless && BootsSettings.On && vis.m_isPlayer && !vis.m_isArmorStand && vis.m_bodyModel != null && vis.m_models != null && vis.m_currentModelIndex >= 0 && vis.m_currentModelIndex < vis.m_models.Length;

        private static void Wear(VisEquipment vis, LegStyle style, FittedParts parts, Material material, int bootsHash)
        {
            int index = vis.m_currentModelIndex;
            var state = new Worn(style, index, bootsHash, vis.m_models[index].m_mesh, parts.Body, Draw(vis.m_bodyModel, parts.Leggings, material));
            Hide(vis.m_legItemInstances, state.Hidden);
            ChestFit.Fit(vis, Surface(vis, state.Body), style.Thickness);
            vis.m_models[index].m_mesh = parts.Body;
            vis.m_bodyModel.sharedMesh = parts.Body;
            worn.Add(vis, state);
        }

        /// <summary>The game's trousers as attached, hidden and remembered, to show again when the fitted ones come off.</summary>
        private static void Hide(List<GameObject> pieces, List<Renderer> hidden)
        {
            if (pieces == null)
                return;
            foreach (GameObject piece in pieces)
            {
                if (piece == null)
                    continue;
                foreach (SkinnedMeshRenderer trousers in piece.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (!trousers.enabled)
                        continue;
                    trousers.enabled = false;
                    hidden.Add(trousers);
                }
            }
        }

        /// <summary>The fitted leggings on the body's bones, beside the body.</summary>
        private static GameObject Draw(SkinnedMeshRenderer body, Mesh mesh, Material material)
        {
            var leggings = new GameObject("EE_FittedLegs") { layer = body.gameObject.layer };
            leggings.transform.SetParent(body.transform.parent, false);
            leggings.transform.localPosition = body.transform.localPosition;
            leggings.transform.localRotation = body.transform.localRotation;
            leggings.transform.localScale = body.transform.localScale;
            SkinnedMeshRenderer renderer = leggings.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.bones = body.bones;
            renderer.rootBone = body.rootBone;
            renderer.localBounds = body.localBounds;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = body.shadowCastingMode;
            return leggings;
        }

        private static void Remove(VisEquipment vis)
        {
            if (!worn.TryGetValue(vis, out Worn state))
                return;
            worn.Remove(vis);
            ChestFit.Unfit(vis);
            if (vis.m_models != null && state.Index < vis.m_models.Length && vis.m_models[state.Index].m_mesh == state.Trimmed)
                vis.m_models[state.Index].m_mesh = state.Body;
            if (vis.m_bodyModel != null && vis.m_bodyModel.sharedMesh == state.Trimmed)
                vis.m_bodyModel.sharedMesh = state.Body;
            foreach (Renderer trousers in state.Hidden)
            {
                if (trousers != null)
                    trousers.enabled = true;
            }
            if (state.Leggings != null)
                Object.Destroy(state.Leggings);
        }

        /// <summary>After the game attached new leggings (or none): fitted ones made for them, or taken off.</summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLegEquipped))]
        private static class LegsPatch
        {
            [HarmonyPostfix]
            private static void Postfix(VisEquipment __instance, bool __result)
            {
                if (__result)
                    Refresh(__instance, BootsShow.Attached(__instance));
            }
        }

        private sealed class Worn
        {
            public Worn(LegStyle style, int index, int boots, Mesh body, Mesh trimmed, GameObject leggings)
            {
                Style = style;
                Index = index;
                Boots = boots;
                Body = body;
                Trimmed = trimmed;
                Leggings = leggings;
            }

            public LegStyle Style { get; }
            public int Index { get; }
            public int Boots { get; }

            /// <summary>The model's own mesh, and the one without the covered triangles drawn instead.</summary>
            public Mesh Body { get; }
            public Mesh Trimmed { get; }
            public GameObject Leggings { get; }
            public List<Renderer> Hidden { get; } = new List<Renderer>();
        }
    }
}
