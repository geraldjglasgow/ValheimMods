using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeastMaster
{
    /// <summary>
    /// The HUD's food slots. Hud.UpdateFood draws one food per entry of m_foodBars, m_foodIcons and m_foodTime and
    /// hides the entries beyond the foods held, so the three lists are extended to <see cref="FoodSlots.MaxSlots"/>
    /// once, when the HUD is created, whatever Food Slots is: unused slots stay hidden. A new slot copies the last
    /// one and moves it by the distance between the last two. The copied object is each element's slot root, the
    /// highest ancestor that does not also hold the previous slot's element, so a slot built as one object with
    /// its bar, icon and timer inside is copied once and its parts found in the copy by their path.
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    public static class FoodSlotsHud
    {
        [HarmonyPostfix]
        public static void Postfix(Hud __instance)
        {
            int count = __instance.m_foodBars?.Length ?? 0;
            if (count < 2 || count >= FoodSlots.MaxSlots || __instance.m_foodIcons?.Length != count || __instance.m_foodTime?.Length != count)
                return;
            List<Image> bars = new List<Image>(__instance.m_foodBars);
            List<Image> icons = new List<Image>(__instance.m_foodIcons);
            List<TMP_Text> times = new List<TMP_Text>(__instance.m_foodTime);
            while (bars.Count < FoodSlots.MaxSlots)
                AddSlot(bars, icons, times);
            __instance.m_foodBars = bars.ToArray();
            __instance.m_foodIcons = icons.ToArray();
            __instance.m_foodTime = times.ToArray();
        }

        private static void AddSlot(List<Image> bars, List<Image> icons, List<TMP_Text> times)
        {
            Dictionary<Transform, Transform> copies = new Dictionary<Transform, Transform>();
            bars.Add(CopyNext(bars, copies));
            icons.Add(CopyNext(icons, copies));
            times.Add(CopyNext(times, copies));
        }

        /// <summary>The last element's counterpart in a copy of its slot root (copied once per root per slot).</summary>
        private static T CopyNext<T>(List<T> list, Dictionary<Transform, Transform> copies) where T : Component
        {
            Transform last = list[list.Count - 1].transform;
            Transform previous = list[list.Count - 2].transform;
            Transform root = SlotRoot(last, previous);
            if (!copies.TryGetValue(root, out Transform copy))
                copies[root] = copy = CopyShifted(root, SlotRoot(previous, last));
            return Counterpart(copy, root, last).GetComponent<T>();
        }

        private static Transform SlotRoot(Transform element, Transform other)
        {
            Transform root = element;
            while (root.parent != null && !other.IsChildOf(root.parent))
                root = root.parent;
            return root;
        }

        private static Transform CopyShifted(Transform root, Transform previousRoot)
        {
            Transform copy = Object.Instantiate(root.gameObject, root.parent).transform;
            copy.name = root.name + "_feastmaster";
            copy.SetSiblingIndex(root.GetSiblingIndex() + 1);
            copy.localPosition = root.localPosition + (root.localPosition - previousRoot.localPosition);
            return copy;
        }

        /// <summary>The transform in <paramref name="copy"/> at the path <paramref name="target"/> has below <paramref name="root"/>.</summary>
        private static Transform Counterpart(Transform copy, Transform root, Transform target)
        {
            Stack<int> path = new Stack<int>();
            for (Transform t = target; t != root; t = t.parent)
                path.Push(t.GetSiblingIndex());
            Transform result = copy;
            while (path.Count > 0)
                result = result.GetChild(path.Pop());
            return result;
        }
    }
}
