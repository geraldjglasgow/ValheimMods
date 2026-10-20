using System;
using EliteCreaturesReborn.Config;
using PatchGuard;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Rendering
{
    /// <summary>
    /// A Rendering Table in the world, on every machine that holds one: its hover and what E does. E renders the next
    /// trophy (held, one every 0.2 s, the game's own repeat); a trophy used on it from the hotbar renders that one, a
    /// boss trophy included (<see cref="TrophyRender"/>). Shift + E changes what it makes, Ores, Ingredients, Idols in
    /// turn, for a player a ward lets in: the choice is in the table's ZDO, so every player sees and uses the same, and
    /// the player asks the table's owner to write it. The hover is rebuilt only when what it shows changes.
    /// </summary>
    internal sealed class RenderingTable : MonoBehaviour, Hoverable, Interactable
    {
        private const string ModeKey = "ecr_render_mode";
        private const string ModeRpc = "ECR_RenderMode";
        private static readonly int ModeHash = ModeKey.GetStableHashCode();

        private ZNetView? _view;
        private string _hover = "";
        private int _hoverState = -1;
        private int _hoverVersion = -1;
        private bool _faultReported;

        private bool Live => _view != null && _view.IsValid();

        /// <summary>What the table makes, from its ZDO.</summary>
        private RenderMode Mode => (RenderMode)Mathf.Clamp(_view!.GetZDO().GetInt(ModeHash), 0, 2);

        private void Awake() => Guard.Run("RenderingTable.Awake", Setup);

        private void Setup()
        {
            _view = GetComponent<ZNetView>();
            if (_view == null || !_view.IsValid())
            {
                _view = null; // a placement ghost: no ZDO
                return;
            }
            _view.Register<int>(ModeRpc, OnMode);
        }

        public string GetHoverText()
        {
            try
            {
                return Live ? Hover() : "";
            }
            catch (Exception e)
            {
                ReportOnce(e);
                return "";
            }
        }

        public string GetHoverName() => RenderPrefab.Name;

        public float GetHoverOffset() => 0f;

        /// <summary>E (and its repeats while held) and Shift + E from the local player.</summary>
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            try
            {
                return Live && Use(user, hold, alt);
            }
            catch (Exception e)
            {
                ReportOnce(e);
                return false;
            }
        }

        /// <summary>An item used on the table from the hotbar: a trophy renders, anything else the game refuses.</summary>
        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            try
            {
                return Live && TrophyRender.One(user, item, Mode, transform.position);
            }
            catch (Exception e)
            {
                ReportOnce(e);
                return false;
            }
        }

        private bool Use(Humanoid user, bool hold, bool alt)
        {
            if (!alt)
            {
                return TrophyRender.Next(user, Mode, transform.position, hold);
            }
            if (hold || !TableSettings.TableEnabled || !PrivateArea.CheckAccess(transform.position))
            {
                return false;
            }
            _view!.InvokeRPC(ModeRpc, ((int)Mode + 1) % 3);
            return true;
        }

        // On the owner: the asker's choice, kept in range.
        private void OnMode(long sender, int mode)
        {
            if (Live && _view!.IsOwner())
            {
                _view.GetZDO().Set(ModeHash, Mathf.Clamp(mode, 0, 2));
            }
        }

        private string Hover()
        {
            bool enabled = TableSettings.TableEnabled;
            bool gamepad = ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive();
            int state = (int)Mode * 4 + (enabled ? 2 : 0) + (gamepad ? 1 : 0);
            if (state != _hoverState || _hoverVersion != Lang.Version)
            {
                _hoverState = state;
                _hoverVersion = Lang.Version;
                _hover = RenderWords.Hover(Mode, enabled, gamepad);
            }
            return _hover;
        }

        private void ReportOnce(Exception e)
        {
            if (!_faultReported)
            {
                _faultReported = true;
                Guard.Report(e, $"Rendering Table at {transform.position:F0}");
            }
        }
    }
}
