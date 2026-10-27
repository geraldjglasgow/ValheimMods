using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rudder in the world, its blade amidships unless a captain's wheel steers it. A placement ghost does nothing. A
    /// rudder on a post other than the standard one is built again to it (<see cref="KwRudderFit"/>) once its keel has
    /// loaded (looked for every two seconds for a while after it appears). How it is steered (<see cref="KwRudderLay"/>)
    /// is measured from the stern by the rudder's owner a little after it appears, and again whenever the pieces round it
    /// change (a wheel, deck, hatch, ladder, mast, keel or cabin placed, removed or loaded, <see cref="KwRudderGearDock.Signature"/>,
    /// looked at every two seconds) and saved in its ZDO: with a captain's wheel on the ship, the steering gear to it
    /// (<see cref="KwRudderGearDock"/>, saved beside the lay, <see cref="KwRudderGear"/>), else the tiller or deck tiller
    /// the stern allows; and the ship's design waterline at the rudder (<see cref="KwRudderWater"/>, from the ship on the
    /// dock), which its blade reaches up to. Every peer draws the lay and the gear the ZDO holds, with its linkage (<see cref="KwRudderLinkage"/>)
    /// and its shafts (<see cref="KwRudderGearLook"/>), so all see the same and nothing else is sent; a rudder linked to a
    /// wheel turns its blade with that wheel as drawn (<see cref="KwWheelHelm.Shown"/>, from the wheel's ZDO), so the gear
    /// between them meshes.
    /// </summary>
    public sealed class KwRudderBlade : MonoBehaviour, Hoverable
    {
        // How often it looks again; for how long it waits for its keel; when its owner first measures; how fast the blade
        // goes back amidships (helm a second) and how near the gear's place the wheel must be found.
        private const float Every = 2f, FitFor = 20f, MeasureAfter = 5f, Follow = 1.7f, WheelNear = 0.6f;

        private ZNetView? view;
        private Transform? blade;
        private KwWheelHelm? wheel;
        private KwRudderTier tier = KwRudderTiers.All[0];
        private KwRudderFit fit = KwRudderFit.Standard;
        private KwRudderLay lay = KwRudderLay.Plain;
        private KwRudderGear? gear;
        private float next, fitUntil, measureAt, turn;
        private int measured, seen;
        private bool fitted, broken, doubted;

        private void Start()
        {
            view = GetComponent<ZNetView>();
            blade = transform.Find(KwRudder.BladeName);
            tier = KwRudderTiers.Of(gameObject) ?? tier;
            if (view == null || view.GetZDO() == null || blade == null)
                enabled = false;
            next = Time.time + Random.value;
            fitUntil = Time.time + FitFor;
            measureAt = Time.time + MeasureAfter;
            lay = KwRudderLay.Of(view != null ? view.GetZDO() : null) ?? lay;
            gear = KwRudderGear.Of(view != null ? view.GetZDO() : null);
            fit = fit.WithWater(KwRudderWater.Of(view != null ? view.GetZDO() : null));
        }

        // Once its keel is loaded (looked for a while after it appears): built to that post, lay and gear.
        private void Fit()
        {
            KwRudderFit? found = KwRudderFit.At(transform.position);
            if (found == null && Time.time < fitUntil)
                return;
            fitted = true;
            fit = (found ?? fit).WithWater(fit.Water);
            KwRudder.Refit(gameObject, fit, lay);
            KwRudderGearLook.Set(gameObject, gear);
        }

        // The owner measures the stern now and then and saves what it finds; every peer draws what the ZDO holds.
        private void Lay()
        {
            ZDO zdo = view!.GetZDO();
            if (view.IsOwner() && Time.time >= measureAt)
                Watch(zdo);
            KwRudderLay saved = KwRudderLay.Of(zdo) ?? KwRudderLay.Plain;
            KwRudderGear? linked = KwRudderGear.Of(zdo);
            float water = KwRudderWater.Of(zdo);
            wheel = linked != null && linked.Linked ? KwWheelHelm.Nearest(transform.TransformPoint(KwMesh.ToUnity(linked.Wheel)), WheelNear) : null;
            if (saved.Same(lay) && KwRudderGear.Same(linked, gear) && KwRudderWater.Same(water, fit.Water))
                return;
            (lay, gear, fit) = (saved, linked, fit.WithWater(water));
            KwRudder.Refit(gameObject, fit, lay);
            KwRudderGearLook.Set(gameObject, gear);
        }

        // On the owner: measured again once the pieces round the rudder have changed and then stayed so for a look.
        private void Watch(ZDO zdo)
        {
            int now = KwRudderGearDock.Signature(transform);
            if (now != measured && now == seen)
                measured = Measure(zdo) ? now : 0;
            seen = now;
        }

        // The steering gear to a wheel when the ship has one that can steer the rudder, else the stern's own lay. A helm the
        // stern no longer seems to have is let go only when measured so twice running: its deck may not have loaded yet.
        private bool Measure(ZDO zdo)
        {
            KwRudderFit wet = Wet(zdo);
            KwRudderGear? found = KwRudderGearDock.Measure(transform, tier, wet, out KwRudderLay geared);
            KwRudderLay pick = found != null && found.Linked ? geared : KwRudderLayFind.Find(transform, tier, wet);
            bool had = KwRudderLay.Of(zdo) is KwRudderLay old && (old.Steered || old.Geared);
            doubted = had && !pick.Steered && !pick.Geared && !doubted;
            if (doubted)
                return false;
            pick.Save(zdo);
            KwRudderGear.Save(zdo, found);
            return true;
        }

        // The ship's design waterline at the rudder measured from the ship on the dock and saved (the blade is built to it on
        // every peer from there); the one saved before kept while none is found (the dock's pieces not loaded yet).
        private KwRudderFit Wet(ZDO zdo)
        {
            float water = KwRudderWater.OnDock(transform.position);
            if (float.IsNaN(water))
                return fit;
            KwRudderWater.Save(zdo, water);
            return fit.WithWater(water);
        }

        // A throw is reported once and stops this rudder, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside rudder", static r => r.Tick(), this);
            broken = false;
        }

        private void Tick()
        {
            Steer();
            if (Time.time < next)
                return;
            next = Time.time + Every;
            if (!fitted)
                Fit();
            else
                Lay();
        }

        // The blade with the wheel its gear is linked to, as that wheel is drawn; back amidships with none.
        private void Steer()
        {
            float target = wheel != null ? wheel.Shown : 0f;
            if (blade == null || Mathf.Approximately(turn, target))
                return;
            turn = wheel != null ? target : Mathf.MoveTowards(turn, target, Follow * Time.deltaTime);
            // the trailing edge swings to the side the ship turns toward, as the game's ships turn theirs
            blade.localRotation = Quaternion.Euler(0f, -turn * KwRudder.MaxAngle, 0f);
        }

        public string GetHoverText() => tier.Name + HelmLine();

        // How it is steered, as the stern it hangs on allows.
        private string HelmLine() => lay.Kind switch
        {
            KwRudderHelm.Tiller => $"\nA tiller over the post to the helm on the deck, {lay.Reach:0.0} m forward",
            KwRudderHelm.Lines => $"\nA yoke and {(tier.Rod.HasValue ? "rods" : "lines")} to a deck tiller {lay.Reach:0.0} m forward",
            KwRudderHelm.Yoke => "\n<color=#888888>A yoke, and no deck before the post for a deck tiller: lay a deck there</color>",
            KwRudderHelm.Gear => "\nA quadrant on its stock, turned by the captain's wheel through the steering shaft under the deck: "
                + KwWheelHelm.Describe(turn * KwRudder.MaxAngle),
            _ => "",
        };

        public string GetHoverName() => tier.Name;

        public float GetHoverOffset() => 0f;
    }
}
