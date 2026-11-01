using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A mast's sails' holes worn (<see cref="KwSailDamageCloth"/>): each holed sail in its own copy of the material it wore
    /// (<see cref="KwSailDamageHoles"/>), its texture's pixels read on the main thread, its holes cut into them off it
    /// (<see cref="KwOffMain"/>: a sail of 256 by 256 texels takes a few milliseconds a hole) and the texture and material
    /// made on it once that is done. A sail is cut again only when its own holes change (the same holes cut the same
    /// texture); holes that change while it is cut are cut after it; mended, it wears the material it wore at once.
    /// </summary>
    internal sealed class KwSailHoling
    {
        private readonly Dictionary<int, Holed> sails = new Dictionary<int, Holed>();

        /// <summary>Whether sail i (its cloth under the fitting) wears these holes now; false while its cut is under way (ask again).</summary>
        public bool Wear(Transform fitting, int i, KwSailSpec spec, List<KwRigMark> holes)
        {
            Renderer? cloth = fitting.Find(KwSailDress.Cloth(i))?.GetComponent<Renderer>();
            if (cloth == null)
                return true;
            if (!sails.TryGetValue(i, out Holed was))
                sails[i] = was = new Holed(cloth.sharedMaterial);
            string key = string.Join(";", holes);
            if (was.Job != null)
                return was.Job.Done && Took(cloth, was, key);
            if (key == was.Key)
                return true;
            if (holes.Count == 0)
                return Plain(cloth, was);
            return Begin(cloth, was, key, spec, holes);
        }

        /// <summary>Every holed copy let go (the fitting gone); a cut still under way is dropped when it ends.</summary>
        public void Forget()
        {
            foreach (Holed was in sails.Values)
                Forget(was.Made);
        }

        // The cloth's pixels read and its holes' cut started off the main thread (false: under way); a cloth whose texture
        // cannot be read wears its own material, as it is.
        private static bool Begin(Renderer cloth, Holed was, string key, KwSailSpec spec, List<KwRigMark> holes)
        {
            if (!KwOffMain.Free)
                return false;
            Color32[]? pixels = KwSailDamageHoles.Pixels(was.Worn);
            if (pixels == null)
            {
                Plain(cloth, was);
                was.Key = key;
                return true;
            }
            (int size, float height, List<KwRigMark> cut) = (KwSailLooks.Size, spec.Height, new List<KwRigMark>(holes));
            KwSailFrame frame = KwSailFrame.Of(spec.Shape, spec.Width, spec.Height);
            (was.Job, was.Cutting) = (KwOffMain.Start(() => KwSailDamageHoles.Cut(pixels, size, frame, height, cut)), key);
            return false;
        }

        // The finished cut worn when it is of the holes wanted now (else the next step cuts those); a throw logged, and the
        // sail left as it is until its holes change.
        private static bool Took(Renderer cloth, Holed was, string key)
        {
            KwOffMainJob<Color32[]> job = was.Job!;
            was.Job = null;
            if (job.Error != null)
            {
                Log.Warn($"Broadside: a sail's holes could not be cut: {job.Error.Message}");
                was.Key = key;
                return true;
            }
            if (was.Cutting != key)
                return false;
            Forget(was.Made);
            (was.Made, was.Key) = (KwSailDamageHoles.Made(was.Worn, job.Result), key);
            cloth.sharedMaterial = was.Made;
            return true;
        }

        // The material the cloth wore worn again, its holed copy let go.
        private static bool Plain(Renderer cloth, Holed was)
        {
            Forget(was.Made);
            (was.Made, was.Key) = (null, "");
            cloth.sharedMaterial = was.Worn;
            return true;
        }

        private static void Forget(Material? material)
        {
            if (material == null)
                return;
            Object.Destroy(material.mainTexture);
            Object.Destroy(material);
        }

        // One sail's: the material it wore, its holed copy worn now and the holes cut in it, and a cut under way.
        private sealed class Holed
        {
            public Holed(Material worn) => Worn = worn;

            public Material Worn { get; }
            public Material? Made;
            public string Key = "";
            public KwOffMainJob<Color32[]>? Job;
            public string Cutting = "";
        }
    }
}
