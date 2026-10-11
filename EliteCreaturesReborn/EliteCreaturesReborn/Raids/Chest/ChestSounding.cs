namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Sounding a raid from a Raiders Chest (features/raids.md section 4.1). The player's Shift + E goes to the chest's
    /// owner as one RPC (this machine when it owns it: the game routes it to itself, one code path). The owner decides:
    /// the chest's own rules (<see cref="ChestRules"/>), the raid's (<see cref="Raid.Check"/>: no raid on here, none within
    /// 200 m), the chest not open, coins in it. Then the raid is sounded with every coin as the stake
    /// (<see cref="Raid.Start"/>, which tells the players near and blows the horn) and, only once it is on, every coin is
    /// taken out of the chest in the same frame, so a refused raid never costs a coin and nothing can slip in between. A
    /// refusal goes back to the player who asked, and only to them; the owner always answers, even when ownership moved
    /// while the ask was on its way.
    /// </summary>
    internal static class ChestSounding
    {
        /// <summary>On the chest's ZNetView, to its owner: a player asks to sound the raid.</summary>
        public const string AskRpc = "ecr_raid_sound_ask";

        /// <summary>On the chest's ZNetView, to the asker: why the raid was not sounded (string).</summary>
        public const string ReplyRpc = "ecr_raid_sound_reply";

        /// <summary>The local player's Shift + E: past the ward, the chest's owner is asked.</summary>
        public static bool Ask(RaidChest chest)
        {
            if (chest.Allowed())
            {
                chest.View.InvokeRPC(AskRpc);
            }
            return true;
        }

        /// <summary>The owner's answer to a player's ask.</summary>
        public static void Answer(RaidChest chest, long asker)
        {
            if (!chest.Live)
            {
                return;
            }
            string? refusal = chest.View.IsOwner() ? Sound(chest) : ChestText.Moved;
            if (refusal != null)
            {
                chest.View.InvokeRPC(asker, ReplyRpc, refusal);
            }
        }

        /// <summary>The asker's side of a refusal: the reason in the middle of the screen.</summary>
        public static void Show(string text)
        {
            Player player = Player.m_localPlayer;
            if (player != null)
            {
                player.Message(MessageHud.MessageType.Center, text);
            }
        }

        private static string? Sound(RaidChest chest)
        {
            string? refusal = ChestRules.Refusal(chest) ?? Raid.Check(chest.View);
            Container container = chest.Container;
            if (refusal != null || container.IsInUse())
            {
                return refusal ?? ChestText.InUse;
            }
            container.Load(); // the contents as last saved, should ownership have only just come here
            Inventory inventory = container.GetInventory();
            int stake = ChestCoins.Count(inventory);
            if (stake <= 0)
            {
                return ChestText.NoGold;
            }
            refusal = Raid.Start(chest.View, stake);
            if (refusal == null)
            {
                ChestCoins.Take(inventory, stake);
            }
            return refusal;
        }
    }
}
