using UnityEngine;
using Party.Chat;
using Party.Hooks;
using Party.Server;
using Party.UI;

namespace Party.Client
{
    /// <summary>The mod's one MonoBehaviour: ticks and draws everything else, which are all static classes.</summary>
    public class PartyTicker : MonoBehaviour
    {
        private float inviteExpiryTimer;

        private void Update()
        {
            if (ZNet.instance == null)
                return;
            float dt = Time.deltaTime;
            PartyRpcClient.Tick(dt);
            TickInviteExpiry(dt);
            MapPins.Tick();
            NameplateColorizer.Tick();
            TempPartyPins.Tick();
            InvitePromptUI.Tick(dt);
            HealthPanel.Tick();
            if (HealthPanel.EditMode && Input.GetKeyDown(KeyCode.Escape))
                HealthPanel.ToggleEditMode(false);
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

        private void OnGUI()
        {
            InvitePromptUI.Draw();
            ChatIndicator.Draw();
            OffscreenArrows.Draw();
        }
    }
}
