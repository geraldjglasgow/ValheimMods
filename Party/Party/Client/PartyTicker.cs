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

        private void OnGUI()
        {
            HealthPanel.Draw();
            InvitePromptUI.Draw();
            ChatIndicator.Draw();
            OffscreenArrows.Draw();
        }
    }
}
