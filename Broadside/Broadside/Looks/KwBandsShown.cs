using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// A planking part's painted bands on the dock (<see cref="KwBandMesh"/>): a "bands" holder under the part's model, one
    /// child per colour in its paint over the part's tier (<see cref="KwPaintLooks"/>), its planks shifted along as the
    /// part's are (<see cref="KwPlankShift"/>). Made again each time the part builds (<see cref="KwGenerated"/>), from the
    /// paint segment of its saved text, which travels in its ZDO; only drawn, so never on a dedicated server
    /// (<see cref="KwShipBody.Drawn"/>). The holder owns its meshes and lets them go with it.
    /// </summary>
    public sealed class KwBandsShown : MonoBehaviour
    {
        private const string Holder = "bands";
        private readonly List<Mesh> meshes = new List<Mesh>();

        /// <summary>The part's bands drawn as its saved text paints them (the old ones gone); none for an unpainted part.</summary>
        public static void Dress(Transform part, string saved, KwDef? def, KwBiomeTier tier)
        {
            Transform? model = part.Find("model");
            KwPaint.Strip(saved, out string paint);
            if (model == null)
                return;
            foreach (KwBandsShown old in model.GetComponentsInChildren<KwBandsShown>(true))
            {
                old.gameObject.name = "bands_gone";
                Destroy(old.gameObject);
            }
            if (!KwShipBody.Drawn || def == null || paint.Length == 0)
                return;
            GameObject holder = new GameObject(Holder) { layer = model.gameObject.layer };
            holder.transform.SetParent(model, false);
            holder.AddComponent<KwBandsShown>().Show(part, def, paint, tier);
        }

        private void Show(Transform part, KwDef def, string paint, KwBiomeTier tier)
        {
            foreach ((int colour, Mesh mesh) in KwBandMesh.Meshes(def, paint))
            {
                meshes.Add(mesh);
                Material? look = KwPaintLooks.For(tier, colour);
                if (look == null)
                    continue;
                KwPlankShift.Apply(mesh, part);
                GameObject band = new GameObject("band_" + KwPaint.Names[colour]) { layer = gameObject.layer };
                band.transform.SetParent(transform, false);
                band.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = band.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = look;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in meshes)
            {
                if (mesh != null)
                    Destroy(mesh);
            }
            meshes.Clear();
        }
    }
}
