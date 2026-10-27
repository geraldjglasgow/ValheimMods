using HarmonyLib;
using PatchGuard;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// The Post cap tool in hand (<see cref="KwCapTool"/>): every frame after the game places its ghost, the wheel's notches
    /// go to the tool (<see cref="KwToolNotches"/>, <see cref="KwCapTool.Frame"/>), and aiming at a post's top with no cap on it shows the cap there as the
    /// click would build it (<see cref="KwCapMesh"/>), in its tier's look (<see cref="KwTierCap.Look"/>), solid as built (not see-through), with the tool's own placeholder
    /// hidden meanwhile. The placeholder stays active, only undrawn, so the game still counts the wheel's notches. The cap's
    /// mesh is made again only when what it shows changes. Hidden while a window is open (an edit shows its own preview) and
    /// whenever the tool is not in hand. Local to this player, never networked.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwCapGhost
    {
        private static GameObject? shown;
        private static Mesh? mesh;
        private static string key = "";
        private static GameObject? undrawn;

        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside post cap ghost", Refresh, __instance);

        private static void Refresh(Player player)
        {
            if (player != Player.m_localPlayer)
                return;
            bool held = KwCapTool.Held(player);
            KwCapTool.Frame(player, held);
            KwCapSite? site = held && !KwForm.IsOpen && !Hud.IsPieceSelectionVisible() ? KwCapTool.Hover(player) : null;
            if (site == null)
            {
                Hide(player);
                return;
            }
            Show(site, KwCapTool.Current(site.Width, site.Depth));
            Placeholder(player, false);
        }

        // The cap at the post's top, its mesh made again when its text, tier or place changes.
        private static void Show(KwCapSite site, KwCapSpec spec)
        {
            GameObject ghost = Ghost();
            ghost.SetActive(true);
            string next = spec + "|" + site.Tier.Id + "|" + site.Top.ToString("F3") + "|" + site.Turn.eulerAngles.ToString("F1");
            if (next == key)
                return;
            key = next;
            ghost.transform.SetPositionAndRotation(site.Top, site.Turn);
            if (mesh != null)
                Object.Destroy(mesh);
            mesh = KwCapMesh.Of(spec);
            ghost.GetComponent<MeshFilter>().sharedMesh = mesh;
            ghost.GetComponent<MeshRenderer>().sharedMaterial = KwTierCap.Look(KwCapSpec.Made, spec.ToString(), site.Tier) ?? KwPrefabs.Planks;
        }

        private static void Hide(Player player)
        {
            if (shown != null && shown.activeSelf)
                shown.SetActive(false);
            key = "";
            Placeholder(player, true);
        }

        // The tool's own placeholder drawn or not, kept active so the game still reads the wheel for it; its marker too.
        private static void Placeholder(Player player, bool drawn)
        {
            GameObject? ghost = player.m_placementGhost;
            if (!drawn && player.m_placementMarkerInstance != null)
                player.m_placementMarkerInstance.SetActive(false);
            if (ghost == null || (drawn ? undrawn != ghost : undrawn == ghost))
                return;
            foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = drawn;
            undrawn = drawn ? null : ghost;
        }

        private static GameObject Ghost()
        {
            if (shown != null)
                return shown;
            shown = new GameObject("KwCapGhost");
            shown.AddComponent<MeshFilter>();
            MeshRenderer renderer = shown.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return shown;
        }
    }
}
