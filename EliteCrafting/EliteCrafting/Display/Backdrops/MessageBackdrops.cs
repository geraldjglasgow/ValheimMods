using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCrafting.Display.Backdrops
{
    /// <summary>
    /// The backdrop on the top-left message's icon ("Bronze sword x1" picked up, removed, dropped or broken). The message
    /// queue carries only a sprite, so while the game reports one of those four for an item (<see cref="_about"/>, set
    /// first among the prefixes, so another mod's or our own replacement of the message still counts) the message it
    /// queues with that item's icon is tagged with its tone. When the game puts a message up, a tagged one shows the
    /// backdrop and fades it with the icon. Only the local player's own messages are queued here.
    /// </summary>
    internal static class MessageBackdrops
    {
        private const float FadeSeconds = 4f;   // the game's own fade of the message icon

        private static readonly ConditionalWeakTable<MessageHud.MsgData, Tagged> Tags = new ConditionalWeakTable<MessageHud.MsgData, Tagged>();
        private static ItemDrop.ItemData? _about;
        private static MessageHud.MsgData? _shown;

        private sealed class Tagged
        {
            public Color Tone;
        }

        private static void Begin(ItemDrop.ItemData item) => _about = item;

        private static void End() => _about = null;

        [HarmonyPatch]
        private static class ItemMessagePatch
        {
            [HarmonyTargetMethods]
            private static IEnumerable<MethodBase> Targets()
            {
                yield return AccessTools.Method(typeof(Character), nameof(Character.ShowPickupMessage));
                yield return AccessTools.Method(typeof(Character), nameof(Character.ShowRemovedMessage));
                yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.DropItem));
                yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.DrainEquipedItemDurability));
            }

            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix(ItemDrop.ItemData item) => Begin(item);

            [HarmonyPostfix]
            private static void Postfix() => End();
        }

        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.ShowMessage))]
        private static class QueuePatch
        {
            [HarmonyPrefix]
            private static void Prefix(MessageHud __instance, out int __state) => __state = __instance.m_msgQeue.Count;

            [HarmonyPostfix]
            private static void Postfix(MessageHud __instance, MessageHud.MessageType type, Sprite icon, int __state)
            {
                ItemDrop.ItemData? item = _about;
                if (item == null || type != MessageHud.MessageType.TopLeft || icon == null || __instance.m_msgQeue.Count <= __state)
                {
                    return;
                }
                if (icon == item.GetIcon() && BackdropTone.TryGet(item, out Color tone))
                {
                    Tag(Newest(__instance), tone);
                }
            }
        }

        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.UpdateMessage))]
        private static class ShowPatch
        {
            [HarmonyPostfix]
            private static void Postfix(MessageHud __instance)
            {
                MessageHud.MsgData current = __instance.currentMsg;
                if (ReferenceEquals(current, _shown))
                {
                    return;
                }
                _shown = current;
                Show(__instance.m_messageIcon, current);
            }
        }

        private static void Show(Image icon, MessageHud.MsgData? message)
        {
            if (icon == null)
            {
                return;
            }
            if (message == null || message.m_icon == null || !Tags.TryGetValue(message, out Tagged tagged))
            {
                IconBackdrop.Hide(icon);
                return;
            }
            Image backdrop = IconBackdrop.Show(icon, tagged.Tone);
            backdrop.canvasRenderer.SetAlpha(1f);
            backdrop.CrossFadeAlpha(0f, FadeSeconds, true);
        }

        private static MessageHud.MsgData? Newest(MessageHud hud)
        {
            MessageHud.MsgData? newest = null;
            foreach (MessageHud.MsgData message in hud.m_msgQeue)
            {
                newest = message;
            }
            return newest;
        }

        private static void Tag(MessageHud.MsgData? message, Color tone)
        {
            if (message == null)
            {
                return;
            }
            Tags.Remove(message);
            Tags.Add(message, new Tagged { Tone = tone });
        }
    }
}
