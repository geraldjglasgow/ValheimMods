using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// What the Animal Feeder costs, from Feeder Recipe ("Wood:10, LeatherScraps:4"): item prefab names with amounts,
    /// looked up in the item database (in the net scene's prefab table while the database is not there yet). Every
    /// entry is given back when the feeder is taken down. Unknown items and bad amounts are skipped; an empty or
    /// unusable recipe falls back to Wood:10. Applied to the feeder prefab when it is installed, and again on every
    /// machine whenever the setting changes (a server's synced value included).
    /// </summary>
    public static class FeederRecipe
    {
        public const string Fallback = "Wood:10";

        private static bool watching;

        /// <summary>Writes the current recipe into the feeder prefab's Piece and starts following the setting.</summary>
        public static void Apply()
        {
            Watch();
            Piece piece = FeederPrefab.Piece;
            if (piece != null)
                piece.m_resources = Parse(HusbandryCompanionSettings.FeederRecipe.Value);
        }

        /// <summary>The requirements a recipe text names; the fallback's when none of its entries is usable.</summary>
        public static Piece.Requirement[] Parse(string text)
        {
            List<Piece.Requirement> requirements = Read(text);
            if (requirements.Count == 0)
                requirements = Read(Fallback);
            return requirements.ToArray();
        }

        private static List<Piece.Requirement> Read(string text)
        {
            List<Piece.Requirement> requirements = new List<Piece.Requirement>();
            foreach (string part in (text ?? "").Split(','))
            {
                Piece.Requirement requirement = Entry(part);
                if (requirement != null)
                    requirements.Add(requirement);
            }
            return requirements;
        }

        /// <summary>One "Name:Amount" entry; null when the amount is not a whole number above 0 or the item is unknown.</summary>
        private static Piece.Requirement Entry(string part)
        {
            string[] fields = part.Split(':');
            if (fields.Length != 2)
                return null;
            if (!int.TryParse(fields[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount < 1)
                return null;
            ItemDrop item = Item(fields[0].Trim());
            if (item == null)
                return null;
            return new Piece.Requirement { m_resItem = item, m_amount = amount, m_amountPerLevel = 0, m_recover = true };
        }

        private static ItemDrop Item(string prefabName)
        {
            if (prefabName.Length == 0)
                return null;
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefabName) : null;
            if (prefab == null && ZNetScene.instance != null)
                prefab = ZNetScene.instance.GetPrefab(prefabName);
            return prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        }

        private static void Watch()
        {
            if (watching)
                return;
            watching = true;
            HusbandryCompanionSettings.FeederRecipe.SettingChanged += (sender, args) => HookGuard.Run("feeder recipe", Apply);
        }
    }
}
