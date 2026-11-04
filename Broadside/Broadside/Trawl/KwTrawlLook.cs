using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A net winch drawn, on a screen only (never on a dedicated server), from its step and the time in it
    /// (<see cref="KwTrawlTune"/>) and the net's place in the water (<see cref="KwTrawlTow"/>): nothing is sent or kept. The
    /// drum and its crank turn as rope pays out or comes in, the roll of net on it shrinks as the net goes out and grows
    /// as it is wound back, the sheave turns with the rope, the davit swings in over the deck to land the catch and back
    /// out; the rope runs from the drum over the lead block to the sheave (<see cref="KwTrawlLines"/>), and from the sheave
    /// to the net's tow eye, sagging when the ship is slow. The net (<see cref="KwTrawlNet"/>, a copy in the world, no
    /// parent) is placed by its tow eye, its mouth toward the ship, bunched up while it hangs at the davit, its turn eased so
    /// it never jitters; landing, it hangs bag down under the swung sheave, shakes as it opens and goes onto the drum.
    /// </summary>
    public sealed class KwTrawlLook
    {
        private const float TurnEase = 0.25f, Wind = 6f, Emptied = 0.85f;
        private readonly Transform winch, drum, roll, davit, wheel;
        private readonly Transform? heel;
        private readonly LineRenderer? drumLine, towLine;
        private readonly Vector3[] lead = new Vector3[3], tow = new Vector3[KwTrawlLines.Points];
        private GameObject? net;
        private Quaternion turned = Quaternion.identity;
        private float lastRope = -1f, lastRoll = -1f, spin, sheave;
        private bool shown;

        public KwTrawlLook(Transform winch)
        {
            this.winch = winch;
            drum = winch.Find(KwTrawlBuild.Drum);
            roll = drum.Find(KwTrawlBuild.Roll);
            davit = winch.Find(KwTrawlBuild.Davit);
            wheel = davit.Find(KwTrawlBuild.Sheave);
            heel = winch.Find(KwTrawlBuild.Model + "/" + KwTrawlBuild.Heel);
            drumLine = KwCargoModel.Line(winch, "bs_trawl_lead", ZNetScene.instance);
            towLine = KwCargoModel.Line(winch, "bs_trawl_tow", ZNetScene.instance);
        }

        public void Draw(KwTrawlWinch owner, KwTrawlStep step, float t, float dt)
        {
            float rope = KwTrawlTune.Rope(step, t);
            Turn(step, t, rope);
            Lead();
            float size = KwTrawlTune.Shown(step, t);
            GameObject? made = size > 0f ? Net() : null;
            if (made == null)
            {
                Hide();
                return;
            }
            (Vector3 eye, Quaternion turn, Vector3 scale) = step == KwTrawlStep.Landing ? Hung(t, size) : Towed(owner.Tow, rope);
            Place(made, eye, turn, scale, dt);
            Tow(eye, rope, owner.Speed);
        }

        public void Destroy()
        {
            if (net != null)
                Object.Destroy(net);
        }

        // The davit's swing, the roll's size, and the drum, crank and sheave turned by the rope paid out or wound in.
        private void Turn(KwTrawlStep step, float t, float rope)
        {
            float rolled = KwTrawlTune.Roll(step, t), radius = Mathf.Lerp(KwTrawlTune.RollEmpty, KwTrawlTune.RollFull, rolled);
            davit.localRotation = Quaternion.Euler(0f, KwTrawlTune.Swing(step, t), 0f);
            roll.localScale = new Vector3(1f, radius, radius);
            if (lastRope >= 0f)
            {
                spin += ((rope - lastRope) - (rolled - lastRoll) * Wind) / radius * Mathf.Rad2Deg;
                sheave += (rope - lastRope) / KwTrawlShapes.Sheave * Mathf.Rad2Deg;
            }
            (lastRope, lastRoll) = (rope, rolled);
            drum.localRotation = Quaternion.Euler(Mathf.Repeat(spin, 360f), 0f, 0f);
            wheel.localRotation = Quaternion.Euler(Mathf.Repeat(sheave, 360f), 0f, 0f);
        }

        // The rope from the top of the drum's roll over the lead block to the top of the sheave.
        private void Lead()
        {
            if (drumLine == null)
                return;
            lead[0] = drum.position + winch.up * roll.localScale.y;
            lead[1] = heel != null ? heel.position : davit.position;
            lead[2] = wheel.position + davit.up * KwTrawlShapes.Sheave;
            KwTrawlLines.Through(drumLine, lead);
        }

        // Towed: by its tow eye where the water has it, the mouth toward the ship, the bag drooping, bunched by the rope out.
        private static (Vector3 Eye, Quaternion Turn, Vector3 Scale) Towed(KwTrawlTow tow, float rope) =>
            (tow.At, Quaternion.LookRotation(tow.Way, Vector3.up) * Quaternion.Euler(tow.Pitch, 0f, 0f), KwTrawlTune.Bunch(rope));

        // Landing: bunched, bag down under the swung sheave, shaking as it opens, lighter once emptied, wound onto the drum.
        private (Vector3 Eye, Quaternion Turn, Vector3 Scale) Hung(float t, float size)
        {
            Vector3 way = Vector3.ProjectOnPlane(davit.forward, Vector3.up);
            Quaternion turn = Quaternion.LookRotation(way.sqrMagnitude > 1e-4f ? way.normalized : winch.forward, Vector3.up) * Quaternion.Euler(90f + KwTrawlTune.Wobble(t), 0f, 0f);
            Vector3 scale = KwTrawlTune.Bunch(KwTrawlTune.RopeHang) * size;
            if (t >= KwTrawlTune.DropAt)
                scale.z *= Emptied;
            return (wheel.position + Vector3.down * (KwTrawlShapes.Sheave + KwTrawlTune.RopeHang * size), turn, scale);
        }

        private void Place(GameObject made, Vector3 eye, Quaternion turn, Vector3 scale, float dt)
        {
            turned = shown ? Quaternion.Slerp(turned, turn, 1f - Mathf.Exp(-dt / TurnEase)) : turn;
            Transform placed = made.transform;
            placed.localScale = scale;
            placed.SetPositionAndRotation(eye - turned * Vector3.Scale(KwTrawlNet.TowLocal, scale), turned);
            if (!shown)
                made.SetActive(true);
            shown = true;
        }

        // The tow rope from the sheave to the net's eye, sagging the more the slower the ship and the longer the rope.
        private void Tow(Vector3 eye, float rope, float speed)
        {
            if (towLine == null)
                return;
            float sag = Mathf.Lerp(1f, 0.1f, KwTrawlTune.Ease(speed / 4f)) * Mathf.Clamp01((rope - 1f) / 8f);
            KwTrawlLines.Sagged(towLine, tow, wheel.position, eye, sag);
        }

        // No net out: the net hidden, the rope's end hanging a little under the sheave.
        private void Hide()
        {
            if (net != null && shown)
                net.SetActive(false);
            shown = false;
            if (towLine != null)
                KwTrawlLines.Sagged(towLine, tow, wheel.position, wheel.position + Vector3.down * 0.35f, 0f);
        }

        // The net, made the first time it shows; none without its model.
        private GameObject? Net()
        {
            if (net == null)
            {
                net = KwTrawlNet.Make();
                net?.SetActive(false);
            }
            return net;
        }
    }
}
