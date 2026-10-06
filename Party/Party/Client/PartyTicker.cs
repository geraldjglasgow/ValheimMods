using UnityEngine;
using Party.Chat;
using Party.Hooks;
using Party.Server;
using Party.UI;

namespace Party.Client
{
    /// <summary>The mod's ticking MonoBehaviour: ticks everything else, which are all static classes.</summary>
    public class PartyTicker : MonoBehaviour
    {
        private float inviteExpiryTimer;
        private PartyGui gui;

        private void Awake()
        {
            gui = gameObject.AddComponent<PartyGui>();
            gui.enabled = false;
        }

        private void Update()
        {
            if (ZNet.instance == null)
                return;
            float dt = Time.deltaTime;
            TickInviteExpiry(dt);
            if (ZNet.instance.IsDedicated())
                return;   // a dedicated server has no player, map, nameplates or panel to draw
            PartyRpcClient.Tick(dt);
            MapPins.Tick();
            NameplateColorizer.Tick();
            TempPartyPins.Tick();
            InvitePromptUI.Tick(dt);
            HealthPanel.Tick();
            if (HealthPanel.EditMode && Input.GetKeyDown(KeyCode.Escape))
                HealthPanel.ToggleEditMode(false);
            gui.Refresh();
        }

        /// <summary>Server-side invite timeouts, checked once a second.</summary>
        private void TickInviteExpiry(float dt)
        {
            inviteExpiryTimer += dt;
            if (inviteExpiryTimer < 1f)
                return;
            inviteExpiryTimer = 0f;
            InviteManager.Tick();
        }

        /// <summary>Runs after every script's Update, so it wins the race against the game's own camera controller.</summary>
        private void LateUpdate() => HealthPanel.EnforceCursor();
    }

    /// <summary>
    /// The IMGUI drawing, on its own component so it is enabled only while something could be drawn: an enabled
    /// OnGUI costs a layout and a repaint pass every frame even when it draws nothing. The layout pass is kept
    /// only while the invite window (a GUI.Window, which needs it) is open.
    /// </summary>
    public class PartyGui : MonoBehaviour
    {
        public void Refresh()
        {
            bool invite = InvitePromptUI.Active;
            bool needed = invite || ChatIndicator.Visible() || OffscreenArrows.AnyCandidate();
            if (enabled != needed)
                enabled = needed;
            if (useGUILayout != invite)
                useGUILayout = invite;
        }

        private void OnGUI()
        {
            InvitePromptUI.Draw();
            ChatIndicator.Draw();
            OffscreenArrows.Draw();
        }
    }
}
