using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon's naval truck carriage (<see cref="KwArmDef.Ship"/>), in kit axes from the cannon's origin on the deck
    /// under the middle of the carriage, halfway between its axletrees (x forward toward the muzzle, y left, z up, metres),
    /// drawn to the proportions of a naval gun on its truck carriage measured side on from a photograph (CANNONS.md), in its
    /// front trucks' diameter (<see cref="Size"/>): long and low, two brackets (across, the gun's own cheeks,
    /// <see cref="KwCheeks"/>) with a long head at the trunnions and <see cref="Steps"/> steps down to the rear, on an
    /// underside cut up in an arch between a front and a rear axletree whose arms carry four solid trucks just outside them,
    /// out near the brackets' ends, the rear pair a little smaller; a stool bed between the brackets behind the trunnions,
    /// on which a quoin slides under the breech as the gun elevates (<see cref="KwQuoin"/>: its thin end, its length and
    /// travel come from where the breech's underside is at the gun's lowest and highest elevation and its
    /// <see cref="Slope"/>); and the breeching (<see cref="KwBreeching"/>): a ringbolt on a deck plate ahead of each bracket
    /// and out beyond its trucks, an eyebolt on each bracket's step over the breech and a ring on each side of the breech.
    /// Its looks, mass, how far it recoils, how far the gun elevates, where the gunner stands (close behind the breech's
    /// T-handle and a little to its right as he aims and reloads; out beside the carriage, out of the recoil's way, as he
    /// fires). Hauled about a deck it is slow and heavy: by its rear at a walk or less with all four trucks down, turning
    /// on the spot about its middle (<see cref="KwDeckHaul"/>).
    /// </summary>
    public sealed class KwShipSpec
    {
        /// <summary>The quoin's thin front end's height.</summary>
        public const float Tip = 0.03f;

        // The photograph's proportions, in front truck diameters: the rear trucks' diameter; the axles apart; the front
        // axle ahead of the trunnions; the brackets' front ahead of the front axle and their rear end behind the rear
        // axle; the head's length behind the trunnions and how far its top lies under them (the trunnions bedded in it);
        // the rear step's top over the deck; the arch's rise between the axletrees; the head's front corner taken off;
        // the axletrees' top above the hubs, their underside below them and half their depth fore and aft.
        private const float RearSize = 0.92f, Apart = 2.75f, LeadBy = 0.3f, Nose = 0.42f, Tail = 0.6f, HeadBy = 1.15f, Seat = 0.1f;
        private const float Low = 1.1f, Arched = 0.13f, Corner = 0.12f, TreeUp = 0.1f, TreeDown = 0.16f, TreeDeep = 0.2f;

        // The lengths the arch under the brackets is drawn in; a hair either side of a cut, to read a step's top.
        private const int ArchCuts = 6;
        private const float Hair = 1e-4f;

        // The deck ringbolts: how far ahead of the brackets, how far out beyond the trucks, how high their rings.
        private const float Ahead = 0.25f, Wide = 0.3f, RingHeight = 0.045f;

        // How far behind the T-handle the gunner stands to aim and reload, and how far to its right (where the breech door
        // opens); how far out beside the trucks he steps as he fires; how far behind the brackets the hauler's hips are.
        private const float Back = 0.38f, Right = 0.14f, Clear = 0.26f, Hauled = 0.38f;

        public KwShipSpec(KwLook wood, KwLook metal)
        {
            (Wood, Metal) = (wood, metal);
        }

        /// <summary>Its timber's and its metal's looks.</summary>
        public KwLook Wood { get; }
        public KwLook Metal { get; }

        /// <summary>The front trucks' diameter, which the whole carriage is in proportion to; the trunnions' height above the deck; every truck's width.</summary>
        public float Size { get; set; }
        public float Height { get; set; }
        public float Width { get; set; }

        /// <summary>The steps down from the head to the brackets' rear ends; the breeching rope's radius; the quoin's slope (its rise a metre back).</summary>
        public int Steps { get; set; } = 4;
        public float Rope { get; set; }
        public float Slope { get; set; } = 0.6f;

        /// <summary>How far the carriage rolls back at a shot, metres (the breeching rope taut there).</summary>
        public float Recoil { get; set; }

        /// <summary>
        /// The points of the breech's underside the quoin bears on, on the pitch part, each how far behind the trunnions
        /// (negative) and the barrel's radius there: first the ring it bears on at the gun's lowest and highest elevation
        /// (<see cref="Under"/>, which its shape and travel come from), then the breech's back edges.
        /// </summary>
        public Vector2[] Bears { get; set; } = { new Vector2(-0.4f, 0.16f) };

        /// <summary>The ring of the breech's underside the quoin bears on; the breeching's lugs stand out of its sides.</summary>
        public Vector2 Under => Bears[0];

        /// <summary>How far the gun depresses and elevates, degrees (the quoin is shaped to them).</summary>
        public float Down { get; set; }
        public float Up { get; set; }

        /// <summary>The gun's own cheeks across: the brackets' inner and outer faces, its straps (set as the gun's shape draws the carriage).</summary>
        public KwCheeks Cheeks { get; set; }

        public float Inner => Cheeks.Inner;
        public float Outer => Cheeks.Outer;

        /// <summary>The front and rear trucks' radii.</summary>
        public float Front => Size / 2f;
        public float Rear => Size * RearSize / 2f;

        /// <summary>The axletrees' top above the hubs (where the brackets stand on them), their underside below, half their depth fore and aft.</summary>
        public float TreeTop => Size * TreeUp;
        public float TreeBottom => Size * TreeDown;
        public float TreeHalf => Size * TreeDeep;

        /// <summary>Where the trunnions are, from the origin; the front and rear axles; the brackets' front and rear ends.</summary>
        public float TrunnionX => Size * (Apart / 2f - LeadBy);
        public Vector3 Trunnions => new Vector3(TrunnionX, 0f, Height);
        public float FrontAxle => Size * Apart / 2f;
        public float RearAxle => -FrontAxle;
        public float BracketFront => FrontAxle + Size * Nose;
        public float BracketRear => RearAxle - Size * Tail;

        /// <summary>The trucks' inner and outer faces across, and their middles.</summary>
        public float TruckIn => Outer + 0.015f;
        public float TruckOut => TruckIn + Width;
        public float TruckMid => TruckIn + Width / 2f;

        /// <summary>Where the head ends and its steps begin; the head's top, a little under the trunnions (bedded in it); each step's length and drop; the rear step's top.</summary>
        public float HeadFrom => TrunnionX - Size * HeadBy;
        public float HeadTop => Height - Size * Seat;
        public float StepLength => (HeadFrom - BracketRear) / Steps;
        public float Drop => (HeadTop - RearTop) / Steps;
        public float RearTop => Size * Low;

        /// <summary>The brackets' top at x along them: the head (its front corner taken off), then each step down to the rear.</summary>
        public float TopAt(float x)
        {
            float corner = BracketFront - Size * Corner;
            if (x > corner)
                return HeadTop - (x - corner);
            if (x >= HeadFrom)
                return HeadTop;
            int step = Mathf.CeilToInt((HeadFrom - x) / StepLength - Hair);
            return HeadTop - Mathf.Clamp(step, 1, Steps) * Drop;
        }

        /// <summary>The brackets' underside at x along them: on the rear axletree's top, cut up in an arch between the axletrees, on the front one's (higher, for the larger trucks).</summary>
        public float Bottom(float x)
        {
            float rear = RearAxle + TreeHalf, front = FrontAxle - TreeHalf;
            float t = Mathf.Clamp01((x - rear) / (front - rear));
            return Mathf.Lerp(Rear + TreeTop, Front + TreeTop, t) + Size * Arched * Mathf.Sin(Mathf.PI * t);
        }

        /// <summary>Where the brackets are cut into lengths, rear to front: their ends, each step, the head's corner and the arch in even lengths.</summary>
        public IReadOnlyList<float> Cuts()
        {
            List<float> cuts = new List<float> { BracketRear, HeadFrom, BracketFront - Size * Corner, BracketFront };
            for (int i = 1; i < Steps; i++)
                cuts.Add(HeadFrom - i * StepLength);
            for (int i = 0; i <= ArchCuts; i++)
                cuts.Add(Mathf.Lerp(RearAxle + TreeHalf, FrontAxle - TreeHalf, (float)i / ArchCuts));
            cuts.Sort();
            cuts.RemoveAll(x => x < BracketRear || x > BracketFront);
            for (int i = cuts.Count - 1; i > 0; i--)
            {
                if (cuts[i] - cuts[i - 1] < 0.005f)
                    cuts.RemoveAt(i == cuts.Count - 1 ? i - 1 : i);
            }
            return cuts;
        }

        /// <summary>A length of bracket's top at its rear end and front end (just inside it, so a step's riser belongs to the higher one).</summary>
        public (float Rear, float Front) TopOver(float x0, float x1) => (TopAt(x0 + Hair), TopAt(x1 - Hair));

        /// <summary>Where the breech's underside is at an elevation (degrees up), on the carriage (the yaw part, from its pivot at the origin).</summary>
        public Vector3 Contact(float pitch)
        {
            float a = pitch * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector3(TrunnionX + Under.x * c + Under.y * s, 0f, Height + Under.x * s - Under.y * c);
        }

        /// <summary>The stool bed's top: the breech's underside at full elevation on the quoin's thin end.</summary>
        public float BedTop => Contact(Up).z - Tip;

        /// <summary>The quoin's length (its thick end at full depression bears where its thin end does at full elevation); its front end pushed right in and drawn right out.</summary>
        public float QuoinLength => (Contact(Down).z - Contact(Up).z) / Slope;
        public float QuoinIn => Contact(Down).x + QuoinLength;
        public float QuoinOut => Contact(Up).x;

        /// <summary>The breeching on one side (left +1, right -1): the deck ringbolt's ring, the bracket's eyebolt (on the carriage, on the step beside the ring), the breech's ring (on the pitch part).</summary>
        public Vector3 Anchor(float side) => new Vector3(BracketFront + Ahead, side * (TruckOut + Wide), RingHeight);
        public Vector3 Eye(float side) => new Vector3(TrunnionX + Under.x + 0.02f, side * (Outer + 0.035f), TopAt(TrunnionX + Under.x + 0.02f) - 0.05f);
        public Vector3 Ring(float side) => new Vector3(Under.x, side * (Under.y + 0.045f), 0f);

        /// <summary>The gunner's stand point as he aims and reloads: close behind the breech's T-handle and a little to its right, the side its door opens to, in reach of the T-handle, the breech and the door's handle.</summary>
        public Vector3 Behind(KwBreech breech) => new Vector3(TrunnionX + breech.Tee.x - Back, -Right, 0f);

        /// <summary>Where he steps to from it as he fires: out to the right beside the carriage, clear of its recoil.</summary>
        public Vector3 Aside(Vector3 stand) => new Vector3(stand.x + 0.1f, -(TruckOut + Clear), 0f);

        /// <summary>Where the hauler's fists hold, on the brackets' rear ends (left +1, right -1).</summary>
        public Vector3 Grip(float side) => new Vector3(BracketRear + 0.03f, side * (Inner + Outer) / 2f, RearTop + 0.01f);

        /// <summary>Each truck: its node's name, its hub and its radius.</summary>
        public IEnumerable<(string Name, Vector3 Hub, float Radius)> Trucks()
        {
            yield return (Truck("fl"), new Vector3(FrontAxle, TruckMid, Front), Front);
            yield return (Truck("fr"), new Vector3(FrontAxle, -TruckMid, Front), Front);
            yield return (Truck("rl"), new Vector3(RearAxle, TruckMid, Rear), Rear);
            yield return (Truck("rr"), new Vector3(RearAxle, -TruckMid, Rear), Rear);
        }

        /// <summary>Where it is hauled from: behind the brackets' rear ends, where the hauler's feet go (the haul leaves it at its own height: all four trucks stay down).</summary>
        public Vector3 Handle => new Vector3(BracketRear - Hauled, 0f, 0.35f);

        /// <summary>Hauled slowly: its top speed (metres a second), its turn on the spot (degrees a second) and the hauler's pace.</summary>
        public float Top => 1.2f;
        public float Spin => 20f;
        public float Pace => 1f;

        /// <summary>Its trucks, each turned as it rolls: the truck's node (a path under the cannon) and its radius.</summary>
        public IReadOnlyList<(string Path, float Radius)> Wheels =>
            new[] { (Path("fl"), Front), (Path("fr"), Front), (Path("rl"), Rear), (Path("rr"), Rear) };

        /// <summary>A truck's node name, and its path under the cannon (the trucks hang on the carriage, the yaw part).</summary>
        public static string Truck(string at) => "truck_" + at;

        private static string Path(string at) => KwArm.YawName + "/" + Truck(at);
    }

    /// <summary>
    /// The cheeks a ship cannon's gun sits in, its carriage's brackets, the gun's own (<see cref="KwSmallCannonShape"/> and
    /// the others): their inner and outer faces across, how far ahead the trunnions sit in them, and the straps over them.
    /// </summary>
    public readonly struct KwCheeks
    {
        public KwCheeks(float inner, float outer, float ahead, float strap, float tube)
        {
            (Inner, Outer, Ahead, Strap, Tube) = (inner, outer, ahead, strap, tube);
        }

        public float Inner { get; }
        public float Outer { get; }
        public float Ahead { get; }
        public float Strap { get; }
        public float Tube { get; }
    }
}
