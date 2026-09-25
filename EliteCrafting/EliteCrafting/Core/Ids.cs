namespace EliteCrafting.Core
{
    /// <summary>The id grammar every YAML id and item-data id obeys: <c>^[a-z][a-z0-9_]{1,47}$</c>, checked without regex.</summary>
    public static class Ids
    {
        public static bool IsValid(string? id)
        {
            if (id == null || id.Length < 2 || id.Length > 48 || id[0] < 'a' || id[0] > 'z')
            {
                return false;
            }
            for (int i = 1; i < id.Length; i++)
            {
                char c = id[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>snake_case id to PascalCase, for prefab names: <c>growth_lesser</c> → <c>GrowthLesser</c>.</summary>
        public static string Pascal(string id)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(id.Length);
            bool upper = true;
            foreach (char c in id)
            {
                if (c == '_')
                {
                    upper = true;
                    continue;
                }
                sb.Append(upper ? char.ToUpperInvariant(c) : c);
                upper = false;
            }
            return sb.ToString();
        }
    }
}
