using UnityEngine;

namespace PlateColumn
{
    /// <summary>
    /// A mod's plate in the column: an id unique across mods (start it with the mod's name), a rank that places it
    /// among the others (the game's armour plate is <see cref="Column.ArmorRank"/> and its weight plate
    /// <see cref="Column.WeightRank"/>; lower sits higher), the icon it shows, whether it keeps a line of text over the
    /// icon the way the game's plates show a number, and the tooltip shown on hover. Tooltip words may be localization
    /// tokens; the game's tooltip localizes them.
    /// </summary>
    public sealed class PlateSpec
    {
        public PlateSpec(string id, int rank, Sprite? icon, bool withText, string topic, string tip)
        {
            Id = id;
            Rank = Mathf.Clamp(rank, 0, 9999);
            Icon = icon;
            WithText = withText;
            Topic = topic;
            Tip = tip;
        }

        public string Id { get; }

        public int Rank { get; }

        public Sprite? Icon { get; }

        public bool WithText { get; }

        public string Topic { get; }

        public string Tip { get; }
    }
}
