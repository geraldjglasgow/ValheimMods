using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Wayfare.Core;

namespace Wayfare.SeaGates
{
    /// <summary>What the Sea Gate Pillar costs, from <see cref="WayfareConfig.PillarRecipe"/> ("Stone:10,FineWood:5"):
    /// item prefab names with amounts, looked up in the item database. Every entry is given back when the pillar is
    /// taken down. Unknown items and bad amounts are skipped with one warning each; a recipe with no usable entry falls
    /// back to the setting's default. The pillar is in the build menu only while Wayfare and sea gates are on; pillars
    /// already built stand either way. Applied when the prefab is installed, and again on every machine whenever one of
    /// the settings changes, since a server's synced values arrive after startup.</summary>
    public static class SeaGatePieceRecipe
    {
        private static readonly HashSet<string> warned = new HashSet<string>();
        private static bool watching;

        /// <summary>Writes the current recipe and availability into the pillar prefab's Piece and starts following the
        /// settings. Needs the item database: before it exists (the start scene) the next install applies it.</summary>
        public static void Apply()
        {
            Watch();
            Piece piece = SeaGatePiece.Piece;
            if (piece == null || ObjectDB.instance == null)
                return;
            piece.m_resources = Parse(WayfareConfig.PillarRecipe.Value);
            piece.m_enabled = WayfareConfig.Enabled.Value && WayfareConfig.SeaGatesEnabled.Value;
        }

        /// <summary>The requirements a recipe text names; the default recipe's when none of its entries is usable.</summary>
        public static Piece.Requirement[] Parse(string text)
        {
            List<Piece.Requirement> requirements = Read(text);
            if (requirements.Count == 0)
                requirements = Read((string)WayfareConfig.PillarRecipe.DefaultValue);
            return requirements.ToArray();
        }

        private static List<Piece.Requirement> Read(string text)
        {
            List<Piece.Requirement> requirements = new List<Piece.Requirement>();
            foreach (string part in (text ?? "").Split(','))
            {
                if (part.Trim().Length == 0)
                    continue;
                Piece.Requirement requirement = Entry(part.Trim());
                if (requirement != null)
                    requirements.Add(requirement);
            }
            return requirements;
        }

        /// <summary>One "Name:Amount" entry; null (with one warning) when the amount is not a whole number above 0 or
        /// the item is unknown.</summary>
        private static Piece.Requirement Entry(string part)
        {
            string[] fields = part.Split(':');
            if (fields.Length != 2 || !int.TryParse(fields[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount < 1)
            {
                Warn(part, $"Sea Gate Pillar recipe: \"{part}\" is not Item:Amount with an amount above 0; skipped.");
                return null;
            }
            ItemDrop item = Item(fields[0].Trim());
            if (item == null)
            {
                Warn(fields[0].Trim(), $"Sea Gate Pillar recipe: no item named \"{fields[0].Trim()}\"; skipped.");
                return null;
            }
            return new Piece.Requirement { m_resItem = item, m_amount = amount, m_amountPerLevel = 0, m_recover = true };
        }

        private static ItemDrop Item(string prefabName)
        {
            if (prefabName.Length == 0 || ObjectDB.instance == null)
                return null;
            GameObject prefab = ObjectDB.instance.GetItemPrefab(prefabName);
            return prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        }

        private static void Warn(string key, string message)
        {
            if (warned.Add(key))
                Plugin.Log.LogWarning(message);
        }

        private static void Watch()
        {
            if (watching)
                return;
            watching = true;
            WayfareConfig.PillarRecipe.SettingChanged += OnChanged;
            WayfareConfig.SeaGatesEnabled.SettingChanged += OnChanged;
            WayfareConfig.Enabled.SettingChanged += OnChanged;
        }

        private static void OnChanged(object sender, EventArgs args)
        {
            try
            {
                Apply();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Sea Gate Pillar recipe update failed: {e}");
            }
        }
    }
}
