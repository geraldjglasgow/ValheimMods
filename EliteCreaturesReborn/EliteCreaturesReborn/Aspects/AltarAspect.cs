using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Loot;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using PatchGuard;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// The draw on one boss altar: the boss's stars and its aspect. It lives in the altar's own ZDO - the stars, the
    /// aspect and the world time of the next shift - so every player at the bowl reads the same thing and a restart does
    /// not shuffle it. The altar's owner rolls each part that is on the first time, and at each shift rolls both again,
    /// the aspect never to the one it already shows (the stars may repeat); every machine draws the hover lines from the
    /// ZDO, so no message is ever sent for them. A shift to Bountiful rolls its extras in the same moment and keeps them
    /// beside it in the ZDO, so the group reads exactly which aspects it will fight. At the offering the current stars and
    /// aspects are locked on this machine - the one that will instantiate the boss - so a shift during the few seconds
    /// before the boss appears cannot change the fight that was offered for. A part that is off (boss stars or aspects)
    /// is left as it is in the ZDO, and neither shown nor locked. A part a player chose in the altar window
    /// (<see cref="AltarPicks"/>, sent to the owner by <see cref="AltarChoices"/>) is kept instead of rolled; only the
    /// parts left on Random shift, and a chosen Bountiful's extras that nobody chose.
    /// </summary>
    public sealed class AltarAspect : MonoBehaviour
    {
        private const float Interval = 1f;

        /// <summary>Used for an in-game hour when the environment is not up yet: 1/24 of the default 30-minute day.</summary>
        private const double FallbackHourSeconds = 75.0;

        private OfferingBowl _bowl = null!;
        private string _boss = "";
        private float _timer;
        private BossDraw? _locked;

        // The hover lines as last drawn, kept while the bowl's ZDO, the whole second of the shift count-down, the rule set
        // (a reload replaces it) and the language are the ones they were drawn from: the hover is asked for every frame.
        private string _hover = "";
        private uint _hoverRevision = uint.MaxValue;
        private int _hoverSecond = int.MinValue;
        private int _hoverKills = -1;
        private RuleSet? _hoverRules;
        private bool _hoverChoice;
        private int _hoverLang = -1;

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
            AltarChoices.Register(this);
        }

        /// <summary>The boss prefab's name, as the rules know it.</summary>
        public string Boss => _boss;

        public OfferingBowl Bowl => _bowl;

        private void Update() => Guard.Run("AltarAspect.Update", static self => self.Tick(), this);

        private void Tick()
        {
            _timer += Time.deltaTime;
            if (_timer < Interval)
            {
                return;
            }
            _timer = 0f;
            ZDO? zdo = OwnedZdo();
            if (zdo != null && (StarsOn || AspectsOn))
            {
                Refresh(zdo);
            }
        }

        // Owner only. A due shift rolls every part that is on and stores the next time; between shifts only a part that is
        // on but was never rolled is rolled, alone - so an altar from before stars came to altars shows its stars at once
        // and keeps its aspect and its shift time. While shifting is off the stored time is not moved, so switching it
        // back on shifts at once.
        private void Refresh(ZDO zdo)
        {
            double interval = RuleState.Active.Boss.Aspects.ShiftInterval(HourSeconds());
            long wait = AspectStore.GetAltarShiftAt(zdo) - NowMs();
            bool due = interval > 0.0 && (wait <= 0L || wait > (long)(interval * 1000.0)); // shortened since: shift now
            Roll(zdo, due);
            if (due)
            {
                AspectStore.SetAltarShiftAt(zdo, NowMs() + (long)(interval * 1000.0));
            }
        }

        /// <summary>Rolls the stars and the aspect, each only while it is on: both on a shift, else only a missing one. A
        /// part a player chose is set to the choice instead, and a roll of every part may be the plain boss
        /// (<see cref="AltarRoller"/>).</summary>
        private void Roll(ZDO zdo, bool shift) => AltarRoller.Draw(zdo, _boss, shift, shift);

        /// <summary>True from the offering until the boss appears: the fight is locked, so nothing can be chosen.</summary>
        public bool Offered => _bowl != null && _bowl.IsBossSpawnQueued();

        /// <summary>At the offering, on the machine that will spawn the boss: fix the stars and aspects on the bowl now.</summary>
        public void Lock()
        {
            ZDO? zdo = OwnedZdo();
            if (zdo == null || !(StarsOn || AspectsOn))
            {
                _locked = null; // no altar state to read: the boss rolls its own, as an altar-less boss does
                return;
            }
            Roll(zdo, shift: false); // offered before a part's first roll landed: roll it now, so the bowl and the fight agree
            _locked = new BossDraw(StarsOn ? AspectStore.GetAltarStars(zdo) : 0,
                AspectsOn ? AspectStore.GetAltarAspects(zdo) : BossAspects.None);
        }

        public BossDraw? TakeLocked()
        {
            BossDraw? locked = _locked;
            _locked = null;
            return locked;
        }

        /// <summary>The lines appended to the bowl's hover text, drawn from the ZDO on whichever machine is looking, and
        /// drawn again only when what they show has changed.</summary>
        public string HoverLines()
        {
            ZDO? zdo = Zdo();
            if (zdo == null)
            {
                return "";
            }
            RuleSet set = RuleState.Active;
            double seconds = SecondsToShift(zdo, set.Boss.Aspects);
            int second = seconds < 0 ? -1 : Mathf.CeilToInt((float)seconds);
            int kills = BossKills.Count(_boss);
            if (Changed(zdo.DataRevision, second, kills, set, AltarSettings.Enabled))
            {
                _hover = ChoiceHint() + DrawHover(zdo, set.Boss.Aspects, seconds) + BossLines(zdo, set, kills);
            }
            return _hover;
        }

        // Whether anything the hover lines show has changed since they were drawn, the words' language included; if so,
        // the new state is kept.
        private bool Changed(uint revision, int second, int kills, RuleSet set, bool choice)
        {
            if (revision == _hoverRevision && second == _hoverSecond && kills == _hoverKills
                && ReferenceEquals(set, _hoverRules) && choice == _hoverChoice && Lang.Version == _hoverLang)
            {
                return false;
            }
            _hoverRevision = revision;
            _hoverSecond = second;
            _hoverKills = kills;
            _hoverRules = set;
            _hoverChoice = choice;
            _hoverLang = Lang.Version;
            return true;
        }

        /// <summary>"[E] Choose the fight" while players may choose; Shift+E on an altar whose E makes the offering.</summary>
        private string ChoiceHint()
        {
            if (!AltarSettings.Enabled || !(StarsOn || AspectsOn) || Offered)
            {
                return "";
            }
            string key = _bowl.m_useItemStands ? "$KEY_AltPlace + $KEY_Use" : "$KEY_Use";
            return Localization.instance.Localize($"\n[<color=yellow><b>{key}</b></color>] ") + Lang.Get("ecr_altar_choose");
        }

        /// <summary>What the fight on the bowl pays beyond the boss's table, and how often the world has defeated it.</summary>
        private string BossLines(ZDO zdo, RuleSet set, int kills)
        {
            int stars = StarsOn && AspectStore.AltarStarsRolled(zdo) ? AspectStore.GetAltarStars(zdo) : 0;
            BossAspects aspects = AspectsOn && AspectStore.AltarRolled(zdo) ? AspectStore.GetAltarAspects(zdo) : BossAspects.None;
            string reward = BossRewards.AltarLine(_boss, stars, AspectLoot.FactorOf(aspects, set.Boss.Aspects));
            string defeated = Defeated(kills);
            return defeated.Length == 0 ? reward : reward + "\n<color=#A0A0A0>" + defeated + "</color>";
        }

        /// <summary>How often the world has defeated the boss, while each defeat leans its stars up; "" before the first
        /// defeat or with that rule off. The altar window shows it too.</summary>
        public static string Defeated(int kills)
        {
            if (kills == 0 || !RuleState.Active.Boss.HarderEachKill)
            {
                return "";
            }
            return kills >= BossKills.MaxKills ? Lang.Format("ecr_altar_defeated_most", kills)
                : kills == 1 ? Lang.Get("ecr_altar_defeated_once") : Lang.Format("ecr_altar_defeated_many", kills);
        }

        // A part a player chose is marked so; the shift count-down is left out once the choices leave nothing to roll.
        private string DrawHover(ZDO zdo, AspectRules rules, double secondsToShift)
        {
            int? stars = StarsOn && AspectStore.AltarStarsRolled(zdo) ? AspectStore.GetAltarStars(zdo) : (int?)null;
            BossAspects? aspects = AspectsOn && AspectStore.AltarRolled(zdo) ? AspectStore.GetAltarAspects(zdo) : (BossAspects?)null;
            if (stars == null && aspects == null)
            {
                return ""; // both off, or the owner's first roll has not arrived yet
            }
            double shown = AltarPicks.AnyRolls(zdo, _boss) ? secondsToShift : -1.0;
            return AspectText.AltarLines(stars, aspects, rules, shown,
                AltarPicks.Stars(zdo) != null, AltarPicks.AspectOf(zdo, _boss) != null, AltarPicks.Extras(zdo, _boss));
        }

        /// <summary>Seconds until the next shift; negative when shifting is off and the altar is fixed.</summary>
        private static double SecondsToShift(ZDO zdo, AspectRules rules) =>
            rules.ShiftInterval(HourSeconds()) > 0.0 ? (AspectStore.GetAltarShiftAt(zdo) - NowMs()) / 1000.0 : -1.0;

        private static bool StarsOn => RuleState.Active.Boss.Enabled;

        private static bool AspectsOn => RuleState.Active.Boss.Aspects.Enabled;

        /// <summary>The bowl's ZDO, or null while its network view is not valid.</summary>
        public ZDO? Zdo()
        {
            ZNetView? view = _bowl != null ? _bowl.m_nview : null;
            return view != null && view.IsValid() ? view.GetZDO() : null;
        }

        /// <summary>The bowl's ZDO while this machine owns it, else null.</summary>
        internal ZDO? OwnedZdo()
        {
            ZDO? zdo = Zdo();
            return zdo != null && _bowl.m_nview.IsOwner() ? zdo : null;
        }

        private static long NowMs() => ZNet.instance != null ? (long)(ZNet.instance.GetTimeSeconds() * 1000.0) : 0L;

        private static double HourSeconds() =>
            EnvMan.instance != null && EnvMan.instance.m_dayLengthSec > 0 ? EnvMan.instance.m_dayLengthSec / 24.0 : FallbackHourSeconds;
    }
}
