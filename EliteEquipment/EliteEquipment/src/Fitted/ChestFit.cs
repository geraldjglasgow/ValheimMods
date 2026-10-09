using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// Chests whose lower part was shaped to lie on the game's baggy trousers (<see cref="ChestShape"/>: the Protector
    /// breastplate's plates and tabards) wear a copy with that part drawn in (<see cref="ChestPull"/>) while the player
    /// wears fitted leggings, so nothing hangs in the air over them, and their front hanging cloth moves as fabric beside it
    /// (<see cref="ChestClothSim"/>); their own mesh again when the leggings come off. Each copy is made once per chest
    /// mesh, body and thickness. Players only, every client, from what the game replicates (<see cref="FittedLook"/> calls it).
    /// </summary>
    public static class ChestFit
    {
        private static readonly Dictionary<(Mesh, Mesh, int), ChestParts> made = new Dictionary<(Mesh, Mesh, int), ChestParts>();
        private static readonly Dictionary<Mesh, BodyEnvelope> envelopes = new Dictionary<Mesh, BodyEnvelope>();
        private static readonly ConditionalWeakTable<VisEquipment, List<Swap>> worn = new ConditionalWeakTable<VisEquipment, List<Swap>>();

        /// <summary>Fitted leggings of this thickness are on: a chest that lay on baggy trousers is drawn in over them.</summary>
        internal static void Fit(VisEquipment vis, BodySurface body, float thickness)
        {
            Unfit(vis);
            ChestShape shape = ChestShape.Of(vis.m_currentChestItemHash);
            if (body == null || shape == null || vis.m_chestItemInstances == null)
                return;
            var swaps = new List<Swap>();
            foreach (GameObject piece in vis.m_chestItemInstances)
            {
                if (piece == null)
                    continue;
                foreach (SkinnedMeshRenderer renderer in piece.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    ChestParts parts = Get(renderer, body, thickness, shape);
                    if (parts != null)
                        swaps.Add(Wear(vis, renderer, parts));
                }
            }
            worn.Add(vis, swaps);
        }

        /// <summary>The chest's own meshes back and its cloth taken off (the fitted leggings came off).</summary>
        internal static void Unfit(VisEquipment vis)
        {
            if (!worn.TryGetValue(vis, out List<Swap> swaps))
                return;
            worn.Remove(vis);
            foreach (Swap swap in swaps)
            {
                if (swap.Renderer != null && swap.Renderer.sharedMesh == swap.DrawnIn)
                    swap.Renderer.sharedMesh = swap.Own;
                ChestClothSim.Detach(swap.Cloth);
            }
        }

        private static Swap Wear(VisEquipment vis, SkinnedMeshRenderer renderer, ChestParts parts)
        {
            var swap = new Swap(renderer, renderer.sharedMesh, parts.Chest);
            renderer.sharedMesh = parts.Chest;
            if (parts.Cloth != null)
                swap.Cloth = ChestClothSim.Attach(vis, renderer, parts.Cloth);
            return swap;
        }

        private static ChestParts Get(SkinnedMeshRenderer renderer, BodySurface body, float thickness, ChestShape shape)
        {
            Mesh own = renderer.sharedMesh;
            if (own == null || own.subMeshCount != 1 || renderer.bones == null)
                return null;
            var key = (own, body.Mesh, Mathf.RoundToInt(thickness * 1000f));
            if (!made.TryGetValue(key, out ChestParts parts))
                made[key] = parts = Guarded(own, Array.ConvertAll(renderer.bones, bone => bone != null ? bone.name : null), body, thickness, shape);
            return parts;
        }

        private static ChestParts Guarded(Mesh own, string[] bones, BodySurface body, float thickness, ChestShape shape)
        {
            try
            {
                if (!envelopes.TryGetValue(body.Mesh, out BodyEnvelope envelope))
                    envelopes[body.Mesh] = envelope = new BodyEnvelope(body);
                return ChestPull.Make(own, bones, body, envelope, thickness, shape);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the chest {own.name} could not be drawn in over fitted leggings: {e.Message}");
                return null;
            }
        }

        /// <summary>A new chest was attached: drawn in at once when fitted leggings are on.</summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestEquipped))]
        private static class ChestPatch
        {
            [HarmonyPostfix]
            private static void Postfix(VisEquipment __instance, bool __result)
            {
                if (__result)
                    FittedLook.ChestChanged(__instance);
            }
        }

        private sealed class Swap
        {
            public Swap(SkinnedMeshRenderer renderer, Mesh own, Mesh drawnIn)
            {
                Renderer = renderer;
                Own = own;
                DrawnIn = drawnIn;
            }

            public SkinnedMeshRenderer Renderer { get; }
            public Mesh Own { get; }
            public Mesh DrawnIn { get; }

            /// <summary>The cloth drawn beside the chest, destroyed when it comes off.</summary>
            public GameObject Cloth { get; set; }
        }
    }
}
