using TMPro;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cannons' lines on Broadside's helm key list (<see cref="KwHelmKeys"/>: shown at the bottom of the screen while a
    /// launched ship is steered, the container <see cref="KwHelmKeys.Container"/> under the game's key hints, each child a
    /// copy of one of the game's own key hints, a label "Text" and a key cap "key_bkg" holding the key "Key"). With cannons
    /// aboard two lines are added after its own, each a copy of its first line given the cannons' words:
    /// <see cref="KwBroadside.Key"/> fires the broadside (its reloads noted), and <see cref="KwBroadside.ArcsKey"/> shows or
    /// hides the shot arcs, on or off (<see cref="KwShotArcs"/>); without cannons they are hidden, and they go with the list
    /// when it is hidden. On a game that draws only (the broadside's ticker).
    /// </summary>
    public static class KwHelmRows
    {
        public const string BroadsideRow = "Broadside_FireBroadside", ArcsRow = "Broadside_ShotArcs";
        private const string Label = "Text", Key = "key_bkg/Key";

        private static Transform? list;

        /// <summary>Each frame: the two lines shown with cannons aboard the ship steered, hidden without.</summary>
        public static void Show(bool cannons)
        {
            Transform? keys = Find();
            if (keys == null)
                return;
            Row(keys, BroadsideRow, KwBroadside.KeyName, KwArmWords.BroadsideRow, cannons);
            Row(keys, ArcsRow, KwBroadside.ArcsKeyName, KwArmWords.ArcsRow(KwShotArcs.On), cannons);
        }

        // The helm key list under the game's key hints (found while hidden too), kept until the scene goes.
        private static Transform? Find()
        {
            if (list == null && KeyHints.instance != null)
                list = KeyHints.instance.transform.Find(KwHelmKeys.Container);
            return list;
        }

        // One line: made from the list's first line the first time it is shown, shown or hidden, its words kept up to date.
        private static void Row(Transform keys, string name, string key, string words, bool shown)
        {
            Transform? row = keys.Find(name);
            if (row == null && shown && keys.childCount > 0)
            {
                row = Object.Instantiate(keys.GetChild(0).gameObject, keys, false).transform;
                row.name = name;
            }
            if (row == null)
                return;
            if (row.gameObject.activeSelf != shown)
                row.gameObject.SetActive(shown);
            if (shown)
            {
                Set(row, Label, words);
                Set(row, Key, key);
            }
        }

        private static void Set(Transform row, string path, string text)
        {
            TMP_Text? field = row.Find(path)?.GetComponent<TMP_Text>();
            if (field != null && field.text != text)
                field.text = text;
        }
    }
}
