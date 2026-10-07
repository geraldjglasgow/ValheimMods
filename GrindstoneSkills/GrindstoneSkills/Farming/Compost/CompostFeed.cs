using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Feeding, on the bin's owner every 10 s: each growing, healthy crop within Compost Radius that is not fertilized yet
    /// costs one point and is marked fertilized (<see cref="PlantKeys.SetFed"/>): directly when this machine owns it, else
    /// through <see cref="Keys.RpcFertilize"/> to its owner. A fertilized crop grows Compost Growth Speed percent faster
    /// (<see cref="GrowTime"/>).
    /// </summary>
    public static class CompostFeed
    {
        private const int MaxPerStep = 20;

        private static readonly Collider[] Hits = new Collider[512];
        private static readonly HashSet<Plant> seen = new HashSet<Plant>();

        public static void Step(CompostBin bin)
        {
            int budget = Mathf.Min(MaxPerStep, Mathf.FloorToInt(bin.Points));
            if (budget <= 0)
                return;
            int fed = 0;
            foreach (Plant plant in Hungry(bin.transform.position))
            {
                if (fed >= budget)
                    break;
                Fertilize(plant);
                fed++;
            }
            if (fed > 0)
                bin.SetPoints(bin.Points - fed);
        }

        private static List<Plant> Hungry(Vector3 center)
        {
            seen.Clear();
            List<Plant> hungry = new List<Plant>();
            float radius = Mathf.Max(1f, CompostSettings.Radius.Value);
            int count = Physics.OverlapSphereNonAlloc(center, radius, Hits, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Plant plant = Hits[i] != null ? Hits[i].GetComponentInParent<Plant>() : null;
                if (plant != null && seen.Add(plant) && NeedsFeeding(plant))
                    hungry.Add(plant);
            }
            return hungry;
        }

        private static bool NeedsFeeding(Plant plant)
        {
            ZDO zdo = PlantKeys.Of(plant);
            return zdo != null && !PlantKeys.Fed(zdo) && plant.GetStatus() == Plant.Status.Healthy && CropCatalog.OfPlant(plant) != null;
        }

        private static void Fertilize(Plant plant)
        {
            if (plant.m_nview.IsOwner())
                PlantRpcs.Fertilize(plant);
            else
                plant.m_nview.InvokeRPC(Keys.RpcFertilize);
        }
    }
}
