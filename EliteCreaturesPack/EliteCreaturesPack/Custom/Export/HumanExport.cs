using System.Collections.Generic;
using System.Linq;
using EliteCreaturesPack.Custom.Humans;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// <c>ecp export human</c>: a definition to start a human from (features/custom-creatures.md sections 4 and 7). The
    /// values are read from a human body made for the purpose (<see cref="HumanBody.Build"/>, destroyed right after),
    /// through the same blocks as any export. Written as values: base Human, its name, health and faction (Boss), the
    /// whole `human:` block (any gender, hair and beard, no beard on women, natural hair colours, every skin tone), the
    /// senses and behaviour it learned from the Draugr, an empty drop table and a gear block of the game's early items.
    /// Everything else it has is written commented out: shown, to be changed by removing the <c>#</c>.
    /// </summary>
    internal static class HumanExport
    {
        private const string Body = "ECP_ExportHumanBody";

        /// <summary>A Black Forest bandit: leather, a helmet or a cape, one of five early weapon sets (a bow draws its own arrows).</summary>
        private static readonly string[] Always = { "ArmorLeatherChest", "ArmorLeatherLegs" };
        private static readonly string[][] PickOneFrom = { new[] { "HelmetLeather", "CapeDeerHide" } };
        private static readonly string[][] OneSetFrom =
        {
            new[] { "AxeFlint", "ShieldWood" },
            new[] { "Club", "ShieldWood" },
            new[] { "SpearFlint" },
            new[] { "KnifeFlint" },
            new[] { "Bow" },
        };

        public static string Text()
        {
            GameObject? body = HumanBody.Build(Body);
            try
            {
                return Write(body != null ? new ExportSource(body, null) : null);
            }
            finally
            {
                if (body != null)
                {
                    Object.DestroyImmediate(body);
                }
            }
        }

        private static string Write(ExportSource? body)
        {
            ExportWriter writer = new ExportWriter();
            WriteHeader(writer);
            writer.Open("creatures");
            writer.Item();
            CreatureExport.WriteIdentity(writer, ExportFile.SuggestName(HumanBody.Base), HumanBody.Base);
            if (body != null)
            {
                WriteBody(writer, body);
            }
            else
            {
                writer.Note("the human body could not be made here (the log says why), so its own values are not shown");
                WriteGear(writer, null);
            }
            writer.EndItem();
            writer.Close();
            return writer.ToString();
        }

        private static void WriteBody(ExportWriter writer, ExportSource body)
        {
            HumanLookExport.Write(writer, body);
            WriteCharacter(writer, body);
            MindExport.WriteSenses(writer, body);
            MindExport.WriteBehaviour(writer, body);
            WriteGear(writer, body);
            writer.Note("drops: the human body drops nothing of its own; add rows such as `- item: Coins` with `amount: [5, 20]` "
                + "and `chance: 0.5`");
            KitExport.WriteDrops(writer, body);
            WriteDefaults(writer, body);
        }

        /// <summary>Name, health (the Draugr's) and faction as values; the rest of the block commented.</summary>
        private static void WriteCharacter(ExportWriter writer, ExportSource body)
        {
            writer.Open("character");
            writer.Note("faction boss is the human base's: in the game's rules hostile only to players and what they bring "
                + "(tames, summons), and left alone by every monster; players makes it fight monsters instead");
            CharacterExport.WriteWho(writer, body.Character);
            writer.BeginComment();
            CharacterExport.WriteBody(writer, body.Character);
            writer.EndComment();
            writer.Close();
        }

        /// <summary>Every other block of the body, commented: what it has already, shown to be changed.</summary>
        private static void WriteDefaults(ExportWriter writer, ExportSource body)
        {
            writer.Note("The human body's other values, commented: remove the # in front of a line to change it.");
            writer.BeginComment();
            CharacterExport.WriteProgress(writer, body);
            MindExport.WriteMovement(writer, body);
            MindExport.WriteTaming(writer, body);
            MindExport.WriteSounds(writer, body);
            CreatureExport.WriteAttackBlocks(writer, body);
            LookExport.WriteEffects(writer, body);
            LookExport.WriteLook(writer, body);
            LookExport.WriteTexture(writer, body);
            LookExport.WriteElite(writer, body);
            writer.EndComment();
        }

        private static void WriteGear(ExportWriter writer, ExportSource? body)
        {
            List<string> kit = KitExport.Present(body?.Humanoid?.m_defaultItems).Select(item => item.name).ToList();
            string bare = kit.Count > 0 ? $" ({string.Join(", ", kit)})" : "";
            writer.Note($"gear: any gear block takes the place of the bare kit{bare}, so list armour and a "
                + "weapon. Each human wears the always items, one item of each pick one from list and one of the sets.");
            writer.Open("gear");
            writer.Switch("replace", true);
            writer.Key("always", ExportValues.Names(Known(writer, Always)));
            writer.NameLists("pick one from", PickOneFrom.Select(list => Known(writer, list)));
            writer.NameLists("one set from", OneSetFrom.Select(set => Known(writer, set)));
            writer.Close();
        }

        /// <summary>The items the game has; one it lacks (a game changed by another mod) is left out with a note.</summary>
        private static List<string> Known(ExportWriter writer, string[] items)
        {
            List<string> known = items.Where(item => ObjectDB.instance.GetItemPrefab(item) != null).ToList();
            foreach (string missing in items.Except(known))
            {
                writer.Note($"{missing} is left out: this game has no such item");
            }
            return known;
        }

        private static void WriteHeader(ExportWriter writer)
        {
            CreatureExport.WriteStamp(writer, "ecp export human");
            writer.Note("A definition to start a human from: a person on the player's body (base: Human), each with a random look "
                + "within the `human:` ranges, wearing and fighting with the game's own items through the player's animations.");
            writer.Note("To make one, copy the entry under `creatures:` into EliteCreaturesPack.Creatures.yml (or another "
                + "EliteCreaturesPack.Creatures*.yml), give it a name of your own and change what you want. Lines starting with "
                + "# are notes, or the human body's own values: remove the # in front of one to change it.");
            writer.Note("");
        }
    }
}
