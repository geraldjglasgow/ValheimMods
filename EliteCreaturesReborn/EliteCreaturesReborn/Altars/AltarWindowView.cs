using System.Collections.Generic;
using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Loot;
using EliteCreaturesReborn.Recap.Window;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using TMPro;
using Translations;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCreaturesReborn.Altars
{
    /// <summary>
    /// The open altar window for one altar: Random and every aspect the boss can roll on the left, Random and every star
    /// count the boss table can roll as buttons on the right (the chosen one's label in the game's highlight orange), what
    /// the chosen aspect does, and the altar's own lines as the bowl shows them. A click sends both choices to the altar's
    /// owner at once (<see cref="AltarAspect.Choose"/>); the marks follow what the altar stores, so another player's
    /// choice shows here too. It closes when the player walks away, the offering is made or choosing is switched off.
    /// </summary>
    internal sealed class AltarWindowView : MonoBehaviour
    {
        private const float Reach = 8f;
        private static readonly Color Chosen = new Color(1f, 0.72f, 0.25f, 1f);

        private readonly List<int> _starValues = new List<int>();
        private readonly List<TMP_Text?> _starLabels = new List<TMP_Text?>();
        private AltarPane _pane = null!;
        private ElementList _list = null!;
        private Button _buttonTemplate = null!;
        private Color _normal = Color.white;
        private AltarAspect? _altar;
        private List<Aspect> _aspects = new List<Aspect>();
        private int _stars = AltarPicks.Random;
        private int _aspect = AltarPicks.Random;
        private int _seenStars;
        private int _seenAspect;
        private string? _shownNow;

        public void Init(AltarPane pane, ElementList list, Button buttonTemplate)
        {
            _pane = pane;
            _list = list;
            _list.Clicked = PickAspect;
            _buttonTemplate = buttonTemplate;
            TMP_Text? label = buttonTemplate.GetComponentInChildren<TMP_Text>(true);
            _normal = label != null ? label.color : Color.white;
        }

        /// <summary>Shown for <paramref name="altar"/>, the choices read from the live rules and the marks from the altar.</summary>
        public void Show(AltarAspect altar)
        {
            _altar = altar;
            _shownNow = null;
            gameObject.SetActive(true);
            _pane.Title.text = Lang.Get("ecr_altar_title");
            _pane.NowTitle.text = "<color=orange>" + Lang.Get("ecr_altar_now") + "</color>";
            _pane.Boss.text = BossName(altar.Bowl);
            StarButtons(AltarPicks.StarChoices());
            AspectRows(AltarPicks.AspectChoices(altar.Boss));
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
            string now = _altar.StateLines();
            if (!ReferenceEquals(now, _shownNow))
            {
                _shownNow = now;
                _pane.Now.text = now.TrimStart('\n');
            }
        }

        private static bool InReach(AltarAspect altar)
        {
            Player player = Player.m_localPlayer;
            return player != null && Vector3.Distance(player.transform.position, altar.transform.position) <= Reach;
        }

        // The choices as the altar stores them, taken up when they change (another player's, or ours arriving back), so
        // the marks always show what the altar holds. Unchanged, only two ints are read each frame.
        private void Adopt(bool force)
        {
            ZDO? zdo = _altar != null ? _altar.Zdo() : null;
            if (zdo == null)
            {
                return;
            }
            int stars = AspectStore.GetPickedStars(zdo);
            int aspect = AspectStore.GetPickedAspect(zdo);
            if (!force && stars == _seenStars && aspect == _seenAspect)
            {
                return;
            }
            _seenStars = stars;
            _seenAspect = aspect;
            _stars = AltarPicks.Stars(zdo) ?? AltarPicks.Random;
            _aspect = AltarPicks.AspectOf(zdo, _altar!.Boss) is Aspect chosen ? (int)chosen : AltarPicks.Random;
            Mark();
        }

        private void StarButtons(List<int> choices)
        {
            foreach (Transform old in _pane.StarRow)
            {
                Destroy(old.gameObject);
            }
            _starValues.Clear();
            _starLabels.Clear();
            string title = Lang.Get(choices.Count > 0 ? "ecr_altar_stars" : "ecr_altar_stars_off");
            _pane.StarsTitle.text = "<color=orange>" + title + "</color>";
            if (choices.Count == 0)
            {
                return;
            }
            _starValues.Add(AltarPicks.Random);
            _starValues.AddRange(choices);
            float width = Mathf.Min(110f, (AltarWindowBuilder.Wide - (_starValues.Count - 1) * 8f) / _starValues.Count);
            for (int i = 0; i < _starValues.Count; i++)
            {
                StarButton(_starValues[i], i * (width + 8f), width);
            }
        }

        private void StarButton(int stars, float x, float width)
        {
            string label = stars == AltarPicks.Random ? Lang.Get("ecr_altar_random") : stars == 0 ? Lang.Get("ecr_altar_none")
                : AspectText.Stars(stars);
            Button button = UiParts.Button(_buttonTemplate, _pane.StarRow, "ecr_altar_star", label, () => PickStars(stars));
            UiParts.Place((RectTransform)button.transform, x, 0f, width, 40f);
            _starLabels.Add(button.GetComponentInChildren<TMP_Text>(true));
        }

        private void AspectRows(List<Aspect> choices)
        {
            _aspects = choices;
            _list.Clear();
            if (choices.Count == 0)
            {
                return;
            }
            _list.Add(Lang.Get("ecr_altar_random"), 18f);
            foreach (Aspect aspect in choices)
            {
                _list.Add(Capital(AspectText.Name(aspect)), 18f);
            }
        }

        private void PickStars(int stars)
        {
            _stars = stars;
            Send();
        }

        private void PickAspect(int row)
        {
            _aspect = row <= 0 || row > _aspects.Count ? AltarPicks.Random : (int)_aspects[row - 1];
            Send();
        }

        private void Send()
        {
            _altar?.Choose(_stars, _aspect);
            Mark();
        }

        private void Mark()
        {
            for (int i = 0; i < _starLabels.Count; i++)
            {
                TMP_Text? label = _starLabels[i];
                if (label != null)
                {
                    label.color = _starValues[i] == _stars ? Chosen : _normal;
                }
            }
            int row = _aspect == AltarPicks.Random ? 0 : _aspects.IndexOf((Aspect)_aspect) + 1;
            _list.Mark(_aspects.Count > 0 ? row : -1, false);
            Describe();
        }

        private void Describe()
        {
            if (_aspects.Count == 0)
            {
                _pane.AspectTitle.text = "<color=orange>" + Lang.Get("ecr_altar_aspect_off") + "</color>";
                _pane.Description.text = "";
                return;
            }
            if (_aspect == AltarPicks.Random)
            {
                _pane.AspectTitle.text = Title(Lang.Get("ecr_altar_random"));
                _pane.Description.text = Lang.Get("ecr_altar_random_text");
                return;
            }
            Aspect aspect = (Aspect)_aspect;
            AspectRules rules = RuleState.Active.Boss.Aspects;
            _pane.AspectTitle.text = Title(Capital(AspectText.Name(aspect)));
            _pane.Description.text = AspectText.Sentence(aspect, rules)
                + (aspect == Aspect.Bountiful ? " " + Lang.Get("ecr_altar_bountiful_text") : "")
                + Loot(AspectLoot.FactorOf(new BossAspects(aspect, 0), rules));
        }

        private static string Title(string aspect) => "<color=orange>" + Lang.Format("ecr_altar_aspect_line", aspect) + "</color>";

        private static string Loot(float factor) => Mathf.Approximately(factor, 1f) ? ""
            : "\n<color=#A0A0A0>" + Lang.Format("ecr_altar_loot", factor.ToString("0.##")) + "</color>";

        private static string BossName(OfferingBowl bowl)
        {
            Character? boss = bowl.m_bossPrefab != null ? bowl.m_bossPrefab.GetComponent<Character>() : null;
            return boss != null ? Localization.instance.Localize(boss.m_name) : "";
        }

        private static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
