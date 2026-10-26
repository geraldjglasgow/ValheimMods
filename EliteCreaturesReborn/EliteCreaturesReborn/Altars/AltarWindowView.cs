using System.Collections.Generic;
using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Recap.Window;
using EliteCreaturesReborn.Traits;
using Translations;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCreaturesReborn.Altars
{
    /// <summary>
    /// The open altar window for one altar: Random and every aspect the boss can roll on the left, Random and every star
    /// count the boss table can roll as small buttons on the right (<see cref="ChoiceButtons"/>), then the fight being
    /// chosen: what its aspect does and what it pays (<see cref="FightText"/>), never what the altar happens to show.
    /// With Bountiful chosen, a block of the same buttons picks its extras (<see cref="ExtraChoice"/>): Random and every
    /// aspect it can carry, up to `extra aspects` of them; the rest roll. A click sends the choice to the altar's owner at
    /// once (<see cref="AltarChoices"/>); the marks follow what the altar stores, so another player's choice shows here
    /// too. It closes when the player walks away, the offering is made or choosing is switched off.
    /// </summary>
    internal sealed class AltarWindowView : MonoBehaviour
    {
        private const float Reach = 8f;
        private const int ExtraColumns = 4;

        private readonly ExtraChoice _extras = new ExtraChoice();
        private AltarPane _pane = null!;
        private ElementList _list = null!;
        private ChoiceButtons _starRow = null!;
        private ChoiceButtons _extraRow = null!;
        private AltarAspect? _altar;
        private List<Aspect> _aspects = new List<Aspect>();
        private int _stars = AltarPicks.Random;
        private int _aspect = AltarPicks.Random;
        private int _seenStars;
        private int _seenAspect;
        private int _seenExtras;

        public void Init(AltarPane pane, ElementList list, Button buttonTemplate)
        {
            _pane = pane;
            _list = list;
            _list.Clicked = PickAspect;
            ButtonLook look = ButtonLook.Of(buttonTemplate, pane);
            _starRow = new ChoiceButtons(look, pane.StarRow, "ecr_altar_star", 30f, 6f, false);
            _extraRow = new ChoiceButtons(look, pane.ExtraRow, "ecr_altar_extra", 28f, 6f, true);
        }

        /// <summary>Shown for <paramref name="altar"/>, the choices read from the live rules and the marks from the altar.</summary>
        public void Show(AltarAspect altar)
        {
            _altar = altar;
            gameObject.SetActive(true);
            _pane.Title.text = Lang.Get("ecr_altar_title");
            _pane.Boss.text = BossName(altar.Bowl);
            StarButtons(AltarPicks.StarChoices());
            AspectRows(AltarPicks.AspectChoices(altar.Boss));
            ExtraButtons(AspectRoller.ExtraChoices(altar.Boss));
            Adopt(true);
        }

        public void Hide()
        {
            _altar = null;
            gameObject.SetActive(false);
        }

        // A fault here would repeat every frame while open, so it is reported once and the window closes.
        private void Update()
        {
            try
            {
                Frame();
            }
            catch (System.Exception e)
            {
                PatchGuard.Guard.Report(e, "altar window");
                AltarWindow.Close();
            }
        }

        private void Frame()
        {
            if (_altar == null || !AltarWindow.CanChoose(_altar) || !InReach(_altar))
            {
                AltarWindow.Close();
                return;
            }
            Adopt(false);
        }

        private static bool InReach(AltarAspect altar)
        {
            Player player = Player.m_localPlayer;
            return player != null && Vector3.Distance(player.transform.position, altar.transform.position) <= Reach;
        }

        // The choices as the altar stores them, taken up when they change (another player's, or ours arriving back), so
        // the marks always show what the altar holds. Unchanged, only three ints are read each frame.
        private void Adopt(bool force)
        {
            ZDO? zdo = _altar != null ? _altar.Zdo() : null;
            if (zdo == null)
            {
                return;
            }
            int stars = AspectStore.GetPickedStars(zdo);
            int aspect = AspectStore.GetPickedAspect(zdo);
            int extras = AspectStore.GetPickedExtras(zdo);
            if (!force && stars == _seenStars && aspect == _seenAspect && extras == _seenExtras)
            {
                return;
            }
            _seenStars = stars;
            _seenAspect = aspect;
            _seenExtras = extras;
            _stars = AltarPicks.Stars(zdo) ?? AltarPicks.Random;
            _aspect = AltarPicks.AspectOf(zdo, _altar!.Boss) is Aspect chosen ? (int)chosen : AltarPicks.Random;
            _extras.Adopt(AltarPicks.Extras(zdo, _altar.Boss));
            Mark();
        }

        private void StarButtons(List<int> choices)
        {
            string title = Lang.Get(choices.Count > 0 ? "ecr_altar_stars" : "ecr_altar_stars_off");
            _pane.StarsTitle.text = "<color=orange>" + title + "</color>";
            List<int> values = new List<int>();
            if (choices.Count > 0)
            {
                values.Add(AltarPicks.Random);
                values.AddRange(choices);
            }
            _starRow.Build(values, StarLabel, PickStars, values.Count, 76f);
        }

        private static string StarLabel(int stars) => stars == AltarPicks.Random ? Lang.Get("ecr_altar_random")
            : stars == 0 ? Lang.Get("ecr_altar_none") : AspectText.Stars(stars);

        // Random and every aspect a Bountiful boss of this kind can carry, four to a row; none while it carries none.
        private void ExtraButtons(List<Aspect> choices)
        {
            List<int> values = new List<int>();
            if (choices.Count > 0 && AspectRoller.ExtraCount() > 0)
            {
                values.Add(AltarPicks.Random);
                values.AddRange(choices.ConvertAll(aspect => (int)aspect));
            }
            _extraRow.Build(values, ExtraLabel, PickExtra, ExtraColumns, float.MaxValue);
        }

        private static string ExtraLabel(int aspect) => aspect == AltarPicks.Random ? Lang.Get("ecr_altar_random")
            : FightText.Capital(AspectText.Name((Aspect)aspect));

        private void AspectRows(List<Aspect> choices)
        {
            _aspects = choices;
            _list.Clear();
            if (choices.Count == 0)
            {
                return;
            }
            _list.Add(Lang.Get("ecr_altar_random"), 16f);
            foreach (Aspect aspect in choices)
            {
                _list.Add(FightText.Capital(AspectText.Name(aspect)), 16f);
            }
        }

        private void PickStars(int stars)
        {
            _stars = stars;
            Send();
        }

        // Another aspect lets go of the extras: they belong to Bountiful.
        private void PickAspect(int row)
        {
            _aspect = row <= 0 || row > _aspects.Count ? AltarPicks.Random : (int)_aspects[row - 1];
            if (_aspect != (int)Aspect.Bountiful)
            {
                _extras.Clear();
            }
            Send();
        }

        // Random lets every extra roll; an aspect is picked, or let go when it was.
        private void PickExtra(int aspect)
        {
            if (aspect == AltarPicks.Random)
            {
                _extras.Clear();
            }
            else
            {
                _extras.Toggle((Aspect)aspect, AspectRoller.ExtraCount());
            }
            if (_altar != null)
            {
                AltarChoices.ChooseExtras(_altar, _extras.Mask);
            }
            Mark();
        }

        private void Send()
        {
            if (_altar != null)
            {
                AltarChoices.Choose(_altar, _stars, _aspect);
            }
            Mark();
        }

        private void Mark()
        {
            _starRow.Mark(value => value == _stars);
            int row = _aspect == AltarPicks.Random ? 0 : _aspects.IndexOf((Aspect)_aspect) + 1;
            _list.Mark(_aspects.Count > 0 ? row : -1, false);
            _extraRow.Mark(value => value == AltarPicks.Random ? _extras.Count == 0 : _extras.Holds((Aspect)value));
            Describe();
        }

        // The fight being chosen, never the one on the altar: its aspect, what that does (with a chosen Bountiful's
        // extras), and what it pays; then the parts laid out for what shows.
        private void Describe()
        {
            bool any = _aspects.Count > 0;
            bool extras = _aspect == (int)Aspect.Bountiful && _extraRow.Height > 0f;
            int chosen = extras ? _extras.Mask : 0;
            bool rolls = extras && _altar != null && AspectRoller.CanFill(_altar.Boss, chosen);
            _pane.AspectTitle.text = FightText.AspectTitle(_aspect, any);
            _pane.Description.text = FightText.Description(_aspect, any, chosen, rolls);
            _pane.ExtrasTitle.text = extras ? FightText.ExtrasTitle(_extras.Count, AspectRoller.ExtraCount()) : "";
            _pane.ExtraRow.gameObject.SetActive(extras);
            _pane.Fight.text = _altar != null ? FightText.Fight(_altar.Boss, _stars, _aspect, _aspects, chosen, rolls) : "";
            AltarWindowBuilder.Layout(_pane, extras ? _extraRow.Height : 0f);
        }

        private static string BossName(OfferingBowl bowl)
        {
            Character? boss = bowl.m_bossPrefab != null ? bowl.m_bossPrefab.GetComponent<Character>() : null;
            return boss != null ? Localization.instance.Localize(boss.m_name) : "";
        }
    }
}
