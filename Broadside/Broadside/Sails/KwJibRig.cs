using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A bowsprit's jibs on the dock (<see cref="KwJibDress"/>), on the bowsprit piece. Every two seconds it looks for its
    /// foremast among the masts loaded (<see cref="KwJibStay.OnDock"/>) and makes its jibs again when that mast, or where its
    /// stay comes, has changed: a mast built after the bowsprit takes its jibs, one taken away leaves the bowsprit bare. The
    /// jibs are set and furled with the foremast's sails, following the hoist its ZDO holds (<see cref="KwSailRig.Drive"/>)
    /// and easing there as the sails do; so nothing of the jibs is saved or sent: every peer finds the same foremast from
    /// the pieces' saved texts and reads the same hoist. Only drawn, so never on a dedicated server; a ghost does nothing.
    /// </summary>
    public sealed class KwJibRig : MonoBehaviour
    {
        private const float Every = 2f, PerSecond = 0.7f;

        private KwBowsprit spec;
        private List<KwJib> jibs = new List<KwJib>();
        private KwSailRig? foremast;
        private string key = "";
        private float checkAt, stage = -1f;
        private bool broken;

        private void Start()
        {
            ZDO? zdo = GetComponent<ZNetView>()?.GetZDO();
            if (zdo == null || !KwShipBody.Drawn || !KwBowsprit.TryParse(zdo.GetString(KwGenerated.Key), out spec) || spec.Jibs == 0)
            {
                enabled = false;
                return;
            }
            checkAt = Time.time + 1f;
        }

        // A throw is reported once and stops these jibs, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside jibs", static r => { r.Check(); r.Follow(); }, this);
            broken = false;
        }

        // The foremast found again; the jibs made again when it, or its stay, changed.
        private void Check()
        {
            if (Time.time < checkAt)
                return;
            checkAt = Time.time + Every;
            KwJibStay? stay = KwJibStay.OnDock(transform.position, transform.rotation, out KwSailRig? rig);
            string now = stay != null && rig != null ? rig.GetInstanceID() + "|" + stay.Key : "";
            if (now == key)
                return;
            (key, foremast, stage) = (now, rig, -1f);
            if (stay != null)
                jibs = KwJibDress.Dress(transform, spec, stay);
            else
            {
                KwJibDress.Clear(transform);
                jibs.Clear();
            }
        }

        // The jibs set as far as the foremast's sails are, easing there.
        private void Follow()
        {
            if (jibs.Count == 0)
                return;
            float target = foremast != null ? foremast.Drive : 1f;
            if (stage >= 0f && Mathf.Approximately(stage, target))
                return;
            stage = stage < 0f ? target : Mathf.MoveTowards(stage, target, PerSecond * Time.deltaTime);
            foreach (KwJib jib in jibs)
                KwJibDress.Pose(jib, Mathf.SmoothStep(0f, 1f, stage));
        }
    }
}
