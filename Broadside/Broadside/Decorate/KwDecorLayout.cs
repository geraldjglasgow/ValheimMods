using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>One step of a layout's work; true once it is done.</summary>
    public interface IKwDecorTask
    {
        bool Step();
    }

    /// <summary>A scene placed in a layout: which, where its middle stands, and the boxes other scenes keep out of.</summary>
    public sealed class KwPlacedScene
    {
        public KwPlacedScene(string id, Vector3 at, List<KwDecorBox> boxes)
        {
            (Id, At, Boxes) = (id, at, boxes);
        }

        public string Id { get; }
        public Vector3 At { get; }
        public List<KwDecorBox> Boxes { get; }
    }

    /// <summary>
    /// A layout for a reading and a seed (DECORATE.md, "Scenes" and "The quay"), worked out over frames as a list of
    /// tasks: first the quay's furniture along the docks' edges (<see cref="KwDecorQuay"/>), then each scene in turn
    /// (<see cref="KwDecorScenes.Order"/>), each searching the area for the place it likes best that it fits whole
    /// (<see cref="KwSiteSearch"/>). Every decoration carries the density that first shows it, so a density shows those up
    /// to it and turning it up or down adds or takes away scenes and fillers without moving the others (<see cref="Shown"/>).
    /// When nothing of the first densities found room, the densities are shifted down so the first always shows something.
    /// Last, each decoration made of a dock's wood takes the wood tier going with where it stands (<see cref="KwDecorTiers"/>).
    /// The same reading and seed give the same layout.
    /// </summary>
    public sealed class KwDecorLayout
    {
        public const int Densities = 5;
        public const string QuayName = "quay furniture";
        private readonly List<IKwDecorTask> tasks = new List<IKwDecorTask>();
        private readonly Dictionary<int, string> names = new Dictionary<int, string>();
        private int current, groups;

        public KwDecorLayout(KwDecorArea area, int seed)
        {
            (Area, Dice) = (area, new System.Random(seed));
            Runs = KwDecorEdges.Runs(area);
            tasks.AddRange(KwDecorQuay.Tasks(this));
            foreach ((KwDecorScene scene, int copy) in KwDecorScenes.Order())
            {
                KwSceneShape shape = new KwSceneShape(scene);
                if (shape.Valid)
                    tasks.Add(new KwSiteSearch(this, shape, copy));
            }
            // last, each decoration of a dock's wood in the tier that goes with where it stands
            tasks.Add(new KwOnceTask(() => KwDecorTiers.Apply(this)));
        }

        public KwDecorArea Area { get; }
        public System.Random Dice { get; }

        /// <summary>The docks' edges, straight run by run (<see cref="KwDecorEdges"/>).</summary>
        public List<KwEdgeRun> Runs { get; }

        public List<KwDecorPlan> Plans { get; } = new List<KwDecorPlan>();
        public List<KwPlacedScene> Scenes { get; } = new List<KwPlacedScene>();
        public bool Done { get; private set; }

        /// <summary>Works on until done or the time (realtime since startup, as a double: a float's 4 ms is lost after hours) runs out.</summary>
        public void Step(double until)
        {
            while (!Done && Time.realtimeSinceStartupAsDouble < until)
                Next();
        }

        private void Next()
        {
            if (current >= tasks.Count)
            {
                Finish();
                return;
            }
            if (tasks[current].Step())
                current++;
        }

        // The first density that shows anything becomes the first.
        private void Finish()
        {
            int first = Densities;
            foreach (KwDecorPlan plan in Plans)
                first = Mathf.Min(first, plan.Level);
            foreach (KwDecorPlan plan in Plans)
                plan.Level -= first - 1;
            Done = true;
        }

        /// <summary>A new group of decorations under a name (a scene's pieces).</summary>
        public int Group(string name)
        {
            names[groups] = name;
            return groups++;
        }

        /// <summary>What a group is called; the quay's own pieces (no group) are the quay furniture.</summary>
        public string NameOf(int group) => group >= 0 && names.TryGetValue(group, out string name) ? name : QuayName;

        /// <summary>The plan added when it fits (<see cref="KwDecorFitCheck"/>).</summary>
        public bool TryAdd(KwDecorPlan plan)
        {
            if (!KwDecorFitCheck.Fits(Area, plan, Plans))
                return false;
            Plans.Add(plan);
            return true;
        }

        /// <summary>The decorations a density shows, in the order they are placed.</summary>
        public List<KwDecorPlan> Shown(int density) => Done ? Plans.FindAll(p => p.Level <= density) : new List<KwDecorPlan>();

        /// <summary>The scenes among some decorations, by name, in order, the quay furniture last.</summary>
        public List<string> Names(List<KwDecorPlan> shown)
        {
            List<string> found = new List<string>();
            foreach (KwDecorPlan plan in shown)
            {
                string name = NameOf(plan.Group);
                if (!found.Contains(name))
                    found.Add(name);
            }
            if (found.Remove(QuayName))
                found.Add(QuayName);
            return found;
        }
    }
}
