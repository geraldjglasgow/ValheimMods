using UnityEngine;
using Party.Chat;
using Party.Hooks;
using Party.Server;
using Party.UI;

namespace Party.Client
{
    /// <summary>
    /// The mod's one MonoBehaviour: drives every per-frame client responsibility and draws every IMGUI overlay.
    /// Everything it calls is a static class with its own single responsibility; this only decides when.
    /// </summary>
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
            PartyPing.Tick(dt);
            InvitePromptUI.Tick(dt);
            if (HealthPanel.EditMode && Input.GetKeyDown(KeyCode.Escape))
                HealthPanel.ToggleEditMode(false);
        }

        /// <summary>Server-side invite timeouts, checked once a second. <see cref="InviteManager.Tick"/> no-ops on clients.</summary>
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
        }
    }
}
