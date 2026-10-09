using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// A leggings' worn skin (its <c>attach_skin</c> child) cut into trousers and boots by the workshop's triangles
    /// (<see cref="NativeSplit"/>): each skinned mesh maps to its two parts, [0] the trousers and [1] the boots, both in
    /// the game's own materials since the renderers stay the game's. Empty for leggings with no mesh (the painted cloth
    /// sets), when the game's mesh is not what the split was made for (a game update; logged) and on a dedicated server,
    /// which draws nothing.
    /// </summary>
    internal static class SkinSplit
    {
        public const string Skin = "attach_skin";

        private static readonly bool headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        public static Dictionary<Mesh, Mesh[]> Cut(BootSet set, GameObject legs)
        {
            var cuts = new Dictionary<Mesh, Mesh[]>();
            SplitData data = NativeSplit.For(set.Key);
            Transform skin = legs.transform.Find(Skin);
            if (headless || data?.Boots == null || skin == null)
                return cuts;
            foreach (SkinnedMeshRenderer renderer in skin.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh != null && !cuts.ContainsKey(mesh))
                {
                    Mesh[] parts = Guarded(set, mesh, data);
                    if (parts != null)
                        cuts[mesh] = parts;
                }
            }
            return cuts;
        }

        private static Mesh[] Guarded(BootSet set, Mesh mesh, SplitData data)
        {
            if (!MeshCut.Fits(mesh, data.Boots.Length))
            {
                Plugin.Log.LogWarning($"EliteEquipment: the game's {set.Legs} mesh {mesh.name} is not the one the boots were cut from; those leggings keep their whole look");
                return null;
            }
            try
            {
                return MeshCut.Cut(mesh, data.Boots, data.Shared);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the {set.Key} leggings could not be cut into trousers and boots: {e.Message}");
                return null;
            }
        }
    }
}
