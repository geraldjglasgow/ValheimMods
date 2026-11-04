using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One reading of an area (DECORATE.md, "Reading the area"), spread over frames so even a 64 m area never stalls one:
    /// the cells' rays 128 at a time (<see cref="KwDecorRays"/>), then the walls' (<see cref="KwDecorWalls"/>), the floors
    /// (<see cref="KwDecorDecks"/>), the ground and water (<see cref="KwDecorGround"/>), the pieces round the area 48 at a
    /// time (<see cref="KwDecorKeepClear"/>), the zones they keep clear marked (<see cref="KwDecorClear"/>) and last the
    /// fields the layout asks (<see cref="KwDecorFields"/>), each pass its own step. Local to the player using the tool;
    /// nothing is sent or saved.
    /// </summary>
    public sealed class KwDecorRead
    {
        private const int Batch = 128, PieceBatch = 48;
        private const int Rays = 0, Walls = 1, Decks = 2, Ground = 3, Gather = 4, Mark = 5, Finished = 7;
        private int phase, next;
        private KwDecorKeepClear? gather;

        public KwDecorRead(Vector3 centre, float yaw, float half)
        {
            Area = new KwDecorArea(new KwDecorGrid(centre, yaw, half));
            KwDecorSurface.Forget();
        }

        public KwDecorArea Area { get; }
        public KwDecorGrid Grid => Area.Grid;
        public bool Done => phase >= Finished;

        /// <summary>How far along, 0 to 1 (the rays are most of it).</summary>
        public float Progress => phase >= Decks ? 1f : (phase * Grid.Cells.Length + next) / (2f * Grid.Cells.Length);

        /// <summary>Works on until done or the time (realtime since startup, as a double) runs out.</summary>
        public void Step(double until)
        {
            while (!Done && Time.realtimeSinceStartupAsDouble < until)
                Work();
        }

        private void Work()
        {
            switch (phase)
            {
                case Rays:
                case Walls:
                    Cells();
                    return;
                case Decks: KwDecorDecks.Read(Grid, Area.Entries); break;
                case Ground: KwDecorGround.Read(Area); break;
                case Gather:
                    if (!Pieces())
                        return;
                    break;
                case Mark: KwDecorClear.Ways(Area); KwDecorClear.Mark(Grid, Area.Kept); break;
                default: Area.Fields = new KwDecorFields(Area); break;
            }
            phase++;
        }

        // The rays down, then the walls', a batch of cells at a time.
        private void Cells()
        {
            int to = Mathf.Min(next + Batch, Grid.Cells.Length);
            if (phase == Rays)
                KwDecorRays.Read(Grid, next, to);
            else
                KwDecorWalls.Read(Grid, next, to, Area.Walls);
            next = to;
            if (next >= Grid.Cells.Length)
                (phase, next) = (phase + 1, 0);
        }

        // The pieces round the area, a batch a step; true once all are read.
        private bool Pieces()
        {
            gather ??= new KwDecorKeepClear(Area);
            if (!gather.Step(Area, PieceBatch))
                return false;
            gather = null;
            return true;
        }
    }
}
