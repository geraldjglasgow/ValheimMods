using System.Collections.Generic;
using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Recap.Window;
using EliteCreaturesReborn.Traits;
using TMPro;
using Translations;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCreaturesReborn.Altars
{
    /// <summary>
    /// The open altar window for one altar: Random and every aspect the boss can roll on the left, Random and every star
    /// count the boss table can roll as small buttons on the right (the chosen one pushed in, the game's pressed button,
    /// inside the inventory's slot frame in the list's orange, its label white), then the fight being chosen: what its
    /// aspect does and what it pays (<see cref="FightText"/>), never what
    /// the altar happens to show. A click sends both choices to the altar's owner at once
    /// (<see cref="AltarAspect.Choose"/>); the marks follow what the altar stores, so another player's choice shows here
    /// too. It closes when the player walks away, the offering is made or choosing is switched off.
    /// </summary>
    internal sealed class AltarWindowView : MonoBehaviour
    {
        private const float Reach = 8f;
        private const float StarHeight = 30f;
        private const float StarGap = 6f;
        private static readonly Color Chosen = new Color(1f, 0.72f, 0.25f, 1f);

        private readonly List<int> _starValues = new List<int>();
        private readonly List<TMP_Text?> _starLabels = new List<TMP_Text?>();
        private readonly List<GameObject?> _starMarks = new List<GameObject?>();
        private readonly List<Image?> _starFaces = new List<Image?>();
        private AltarPane _pane = null!;
        private ElementList _list = null!;
        private Button _buttonTemplate = null!;
        private Color _normal = Color.white;
        private Sprite? _plain;
        private Sprite? _pressed;
        private AltarAspect? _altar;
        private List<Aspect> _aspects = new List<Aspect>();
        private int _stars = AltarPicks.Random;
        private int _aspect = AltarPicks.Random;
        private int _seenStars;
        private int _seenAspect;

        public void Init(AltarPane pane, ElementList list, Button buttonTemplate)
        {
            _pane = pane;
            _list = list;
            _list.Clicked = PickAspect;
            _buttonTemplate = buttonTemplate;
            TMP_Text? label = buttonTemplate.GetComponentInChildren<TMP_Text>(true);
            _normal = label != null ? label.color : Color.white;
            _plain = buttonTemplate.image != null ? buttonTemplate.image.sprite : null;
            bool swaps = buttonTemplate.transition == Selectable.Transition.SpriteSwap;
            _pressed = swaps ? buttonTemplate.spriteState.pressedSprite : null;
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
            _starMarks.Clear();
            _starFaces.Clear();
            string title = Lang.Get(choices.Count > 0 ? "ecr_altar_stars" : "ecr_altar_stars_off");
            _pane.StarsTitle.text = "<color=orange>" + title + "</color>";
            if (choices.Count == 0)
            {
                return;
            }
            _starValues.Add(AltarPicks.Random);
            _starValues.AddRange(choices);
            float width = Mathf.Min(76f, (AltarWindowBuilder.Wide - (_starValues.Count - 1) * StarGap) / _starValues.Count);
            for (int i = 0; i < _starValues.Count; i++)
            {
                StarButton(_starValues[i], i * (width + StarGap), width);
            }
        }

        private void StarButton(int stars, float x, float width)
        {
            string label = stars == AltarPicks.Random ? Lang.Get("ecr_altar_random") : stars == 0 ? Lang.Get("ecr_altar_none")
                : AspectText.Stars(stars);
            Button button = UiParts.Button(_buttonTemplate, _pane.StarRow, "ecr_altar_star", label, () => PickStars(stars));
            UiParts.Place((RectTransform)button.transform, x, 0f, width, StarHeight);
            TMP_Text? text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                Small(text);
            }
            _starLabels.Add(text);
            _starMarks.Add(StarMark(button));
            _starFaces.Add(button.image);
        }

        // The inventory's slot frame, rim only, in the list's orange inside the button's rim and under its label, shown
        // while it is the choice.
        private GameObject? StarMark(Button button)
        {
            if (_pane.StarFrame == null)
            {
                return null;
            }
            Image mark = UiParts.Image(_pane.StarFrame, button.transform, "ecr_altar_star_mark");
            mark.fillCenter = false;
            mark.color = _pane.StarTint;
            UiParts.Fill(mark.rectTransform, 3f);
            mark.transform.SetAsFirstSibling();
            mark.gameObject.SetActive(false);
            return mark.gameObject;
        }

        // The label sized for the small button: one line filling it, never shrunk by auto sizing.
        private static void Small(TMP_Text text)
        {
            UiParts.Fill(text.rectTransform, 2f);
            text.enableAutoSizing = false;
            text.fontSize = 16f;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.Center;
        }

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
                MarkStar(i, _starValues[i] == _stars);
            }
            int row = _aspect == AltarPicks.Random ? 0 : _aspects.IndexOf((Aspect)_aspect) + 1;
            _list.Mark(_aspects.Count > 0 ? row : -1, false);
            Describe();
        }

        // The chosen button pushed in, in its frame with a white label; without a frame, its label in the highlight
        // orange. Hovering still lights it: the game's sprite swap lays its hover sprite over the face.
        private void MarkStar(int index, bool chosen)
        {
            Image? face = _starFaces[index];
            if (face != null && _plain != null && _pressed != null)
            {
                face.sprite = chosen ? _pressed : _plain;
            }
            GameObject? mark = _starMarks[index];
            mark?.SetActive(chosen);
            TMP_Text? label = _starLabels[index];
            if (label != null)
            {
                label.color = !chosen ? _normal : mark != null ? Color.white : Chosen;
            }
        }

        // The fight being chosen, never the one on the altar: its aspect, what that does, and what it pays.
        private void Describe()
        {
            bool any = _aspects.Count > 0;
            _pane.AspectTitle.text = FightText.AspectTitle(_aspect, any);
            _pane.Description.text = FightText.Description(_aspect, any);
            _pane.Fight.text = _altar != null ? FightText.Fight(_altar.Boss, _stars, _aspect, _aspects) : "";
        }

        private static string BossName(OfferingBowl bowl)
        {
            Character? boss = bowl.m_bossPrefab != null ? bowl.m_bossPrefab.GetComponent<Character>() : null;
            return boss != null ? Localization.instance.Localize(boss.m_name) : "";
        }
    }
}
