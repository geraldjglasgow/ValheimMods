using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// The aspect on one boss altar. It lives in the altar's own ZDO - the aspect and the world time of its next shift -
    /// so every player at the bowl reads the same thing and a restart does not shuffle it. The altar's owner rolls it the
    /// first time and shifts it when the world clock passes the stored time, never to the aspect it already shows; every
    /// machine draws the hover line from the ZDO, so no message is ever sent for it. At the offering the current aspect
    /// is locked on this machine - the one that will instantiate the boss - so a shift during the few seconds before
    /// the boss appears cannot change the fight that was offered for.
    /// </summary>
    public sealed class AltarAspect : MonoBehaviour
    {
        private const float Interval = 1f;

        /// <summary>Used for an in-game hour when the environment is not up yet: 1/24 of the default 30-minute day.</summary>
        private const double FallbackHourSeconds = 75.0;

        private OfferingBowl _bowl = null!;
        private string _boss = "";
        private float _timer;
        private Aspect? _locked;

        /// <summary>Added to an altar that summons a boss, on every machine, as the bowl starts.</summary>
        public static void Attach(OfferingBowl bowl)
        {
            Character? boss = bowl.m_bossPrefab != null ? bowl.m_bossPrefab.GetComponent<Character>() : null;
            if (boss != null && boss.IsBoss() && bowl.GetComponent<AltarAspect>() == null)
            {
                bowl.gameObject.AddComponent<AltarAspect>();
            }
        }

        private void Start()
        {
            _bowl = GetComponent<OfferingBowl>();
            _boss = Utils.GetPrefabName(_bowl.m_bossPrefab);
        }

        private void Update() => Guard.Run("AltarAspect.Update", Tick);

        private void Tick()
        {
            _timer += Time.deltaTime;
            if (_timer < Interval)
            {
                return;
            }
            _timer = 0f;
            ZDO? zdo = OwnedZdo();
            if (zdo != null && RuleState.Active.Boss.Aspects.Enabled && Due(zdo))
            {
                Shift(zdo);
            }
        }

        private static bool Due(ZDO zdo)
        {
            if (!AspectStore.AltarRolled(zdo))
            {
                return true;
            }
            return RuleState.Active.Boss.Aspects.ShiftHours > 0f && NowMs() >= AspectStore.GetAltarShiftAt(zdo);
        }

        // Owner only. The next time is stored even when shifting is off, so switching it back on later shifts at once.
        private void Shift(ZDO zdo)
        {
            Aspect? current = AspectStore.AltarRolled(zdo) ? AspectStore.GetAltarAspect(zdo) : (Aspect?)null;
            Aspect next = AspectRoller.Roll(_boss, current);
            double hours = RuleState.Active.Boss.Aspects.ShiftHours;
            AspectStore.SetAltar(zdo, next, NowMs() + (long)(hours * HourSeconds() * 1000.0));
        }

        /// <summary>At the offering, on the machine that will spawn the boss: fix the aspect on the bowl right now.</summary>
        public void Lock()
        {
            ZDO? zdo = OwnedZdo();
            if (zdo == null || !RuleState.Active.Boss.Aspects.Enabled)
            {
                _locked = null; // no altar state to read: the boss rolls its own, as an altar-less boss does
                return;
            }
            if (!AspectStore.AltarRolled(zdo))
            {
                Shift(zdo); // offered before the first roll landed: roll it now, so the bowl and the fight agree
            }
            _locked = AspectStore.GetAltarAspect(zdo);
        }

        public Aspect? TakeLocked()
        {
            Aspect? locked = _locked;
            _locked = null;
            return locked;
        }

        /// <summary>The lines appended to the bowl's hover text, drawn from the ZDO on whichever machine is looking.</summary>
        public string HoverLines()
        {
            ZDO? zdo = Zdo();
            AspectRules rules = RuleState.Active.Boss.Aspects;
            if (zdo == null || !rules.Enabled || !AspectStore.AltarRolled(zdo))
            {
                return "";
            }
            return AspectText.AltarLines(AspectStore.GetAltarAspect(zdo), rules, SecondsToShift(zdo, rules));
        }

        /// <summary>Seconds until the next shift; negative when shifting is off and the aspect is fixed.</summary>
        private static double SecondsToShift(ZDO zdo, AspectRules rules) =>
            rules.ShiftHours > 0f ? (AspectStore.GetAltarShiftAt(zdo) - NowMs()) / 1000.0 : -1.0;

        private ZDO? Zdo()
        {
            ZNetView? view = _bowl != null ? _bowl.m_nview : null;
            return view != null && view.IsValid() ? view.GetZDO() : null;
        }

        private ZDO? OwnedZdo()
        {
            ZDO? zdo = Zdo();
            return zdo != null && _bowl.m_nview.IsOwner() ? zdo : null;
        }

        private static long NowMs() => ZNet.instance != null ? (long)(ZNet.instance.GetTimeSeconds() * 1000.0) : 0L;

        private static double HourSeconds() =>
            EnvMan.instance != null && EnvMan.instance.m_dayLengthSec > 0 ? EnvMan.instance.m_dayLengthSec / 24.0 : FallbackHourSeconds;
    }
}
