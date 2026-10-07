namespace Hearthhold
{
    /// <summary>
    /// The player custom data keys of trader friendship (<see cref="Player.m_customData"/>, saved with the character),
    /// one pair per trader prefab name: the friendship points ("hearthhold_friendship_Haldor" = "300") and the in-game
    /// day of the last gift ("hearthhold_giftday_Haldor" = "42").
    /// </summary>
    public static class FriendshipKeys
    {
        public const string PointsPrefix = "hearthhold_friendship_";
        public const string GiftDayPrefix = "hearthhold_giftday_";
    }
}
