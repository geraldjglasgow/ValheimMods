using System.Collections.Generic;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// Plans a draft against the terrain: builds the centre line, checks its length and slope, finds the whole-metre
    /// vertices it changes with their weights and paint, reads their current and generated heights, and checks the
    /// size, loaded ground and height limits. Reads only this machine's copy of the terrain; writes nothing.
    /// </summary>
    public static class PathPlanner
    {
        private static readonly VertexSampler sampler = new VertexSampler();

        public static PathPlan Plan(PathDraft draft)
        {
            PathPlan plan = new PathPlan(draft, draft.BuildLine());
            plan.MaxSlope = plan.Line.MaxSlope();
            plan.MeanSlope = plan.Line.MeanSlope();
            PathChecks.CheckLine(plan);
            if (plan.TooShort || plan.TooLong)
                return plan;
            List<PlannedVertex> vertices = Weigh(plan);
            plan.PointCount = vertices.Count;
            if (vertices.Count <= PathSettings.MaxPoints.Value)
                ReadTerrain(vertices, plan);
            PathChecks.CheckVertices(plan);
            return plan;
        }

        /// <summary>Every vertex the shape reaches with a visible weight, its target height and paint; terrain not read yet.</summary>
        private static List<PlannedVertex> Weigh(PathPlan plan)
        {
            PathShape shape = plan.Draft.Shape;
            List<PlannedVertex> result = new List<PlannedVertex>();
            foreach (VertexSample sample in LineProjector.Sample(plan.Line, shape.Reach, shape.EndReach))
            {
                float weight = ShoulderWeights.Weight(sample, shape);
                if (weight < ShoulderWeights.MinWeight)
                    continue;
                PaintOp paint = ShoulderWeights.Painted(sample, shape) ? plan.Draft.Paint : PaintOp.None;
                result.Add(new PlannedVertex { X = sample.X, Z = sample.Z, Target = sample.Height, Weight = weight, Paint = paint });
            }
            return result;
        }

        private static void ReadTerrain(List<PlannedVertex> vertices, PathPlan plan)
        {
            sampler.Begin();
            foreach (PlannedVertex planned in vertices)
            {
                PlannedVertex vertex = planned;
                if (sampler.TryVertex(new Vector3(vertex.X, 0f, vertex.Z), out VertexInfo info))
                {
                    vertex.Loaded = true;
                    vertex.Current = info.Current;
                    vertex.Base = info.Base;
                }
                else
                {
                    plan.Unloaded++;
                }
                plan.Vertices.Add(vertex);
            }
        }
    }
}
