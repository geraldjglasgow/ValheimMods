using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Keeps a fish looking right on every client with a screen (not on a dedicated server). The game reads a fish's item
    /// data from its ZDO only when the fish loads (ItemDrop.Awake), so a change made later on the owner (a big one growing
    /// on the hook) would never show elsewhere. Twice a second, on a client that does not own the fish, the mark reloads
    /// it while it is hooked, and once two seconds after it loaded (in case its data arrived after it did); ItemDrop.Load
    /// reads only when the ZDO's data changed, and rescales the fish. It also puts the glow on a legendary fish
    /// (<see cref="LegendaryGlow"/>), on every client including the owner's, and takes it off if the fish stops being one.
    /// While Fishing is turned off it does nothing and takes any glow off.
    /// </summary>
    public sealed class FishMark : MonoBehaviour
    {
        private const float Every = 0.5f;
        private const float LateLoad = 2f;

        private Fish fish;
        private LegendaryGlow glow;
        private float next;
        private float born;
        private bool lateLoaded;

        [HarmonyPatch(typeof(Fish), nameof(Fish.Start))]
        private static class Attach
        {
            [HarmonyPostfix]
            private static void Postfix(Fish __instance)
            {
                if (ZNet.instance != null && !ZNet.instance.IsDedicated())
                    HookGuard.Run("fish mark", () => Add(__instance));
            }
        }

        private static void Add(Fish fish)
        {
            FishMark mark = fish.gameObject.AddComponent<FishMark>();
            mark.fish = fish;
            mark.born = Time.time;
        }

        private void Update()
        {
            if (Time.time < next)
                return;
            next = Time.time + Every;
            HookGuard.Run("fish mark", Refresh);
        }

        private void Refresh()
        {
            if (fish == null || fish.m_nview == null || !fish.m_nview.IsValid())
                return;
            if (!FishSkill.Active)
            {
                RemoveGlow();
                return;
            }
            ItemDrop item = FishInfo.Item(fish);
            if (item != null && !fish.m_nview.IsOwner() && (Hooked() || !lateLoaded && Time.time - born >= LateLoad))
            {
                lateLoaded = true;
                item.Load();
            }
            if (!FishInfo.IsLegendary(FishInfo.Level(fish)))
                RemoveGlow();
            else if (glow == null)
                glow = LegendaryGlow.Attach(transform);
        }

        private void RemoveGlow()
        {
            if (glow == null)
                return;
            glow.Remove();
            glow = null;
        }

        private bool Hooked() => fish.m_nview.GetZDO().GetInt(ZDOVars.s_hooked) == 1;
    }
}
