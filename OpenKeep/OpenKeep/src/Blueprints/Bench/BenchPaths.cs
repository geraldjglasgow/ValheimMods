using System;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>The bench's fixed limits (it has no settings of its own).</summary>
    public static class BenchLimits
    {
        /// <summary>One network part of a packed file, bytes.</summary>
        public const int PartBytes = 32 * 1024;

        /// <summary>The largest blueprint file that can be shared, bytes (the biggest saved compound is about 420 KB).</summary>
        public const int MaxFileBytes = 4 * 1024 * 1024;

        /// <summary>The most parts a packed file may come in (2 MB packed).</summary>
        public const int MaxParts = 64;

        /// <summary>The most blueprints one player may have shared in a world.</summary>
        public const int MaxPerPlayer = 250;

        /// <summary>Parts sent per frame while sharing, and blueprints asked for at once while taking.</summary>
        public const int PartsPerFrame = 2;
        public const int TakesInFlight = 3;

        /// <summary>A transfer the server stops answering is given up after this many seconds.</summary>
        public const float Timeout = 30f;
    }

    /// <summary>
    /// Paths in the shared pool: "houses/barn" from the top of one player's blueprints, no ".json", every part a name
    /// that is a valid file name on Windows and Linux alike (the server may run on either), at most
    /// <see cref="BlueprintFiles.NameLimit"/> characters.
    /// </summary>
    public static class BenchPaths
    {
        private static readonly char[] Invalid = "<>:\"/\\|?*".ToCharArray();

        public static bool ValidPart(string part) =>
            !string.IsNullOrWhiteSpace(part) && part == part.Trim() && part != "." && part != ".." && !part.EndsWith(".")
            && part.IndexOfAny(Invalid) < 0 && part.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !part.Any(char.IsControl);

        /// <summary>The path when every part is valid, else null; "" (the top) only when <paramref name="top"/> allows it.</summary>
        public static string Clean(string path, bool top = false)
        {
            if (path == null || path.Length > BlueprintFiles.NameLimit)
                return null;
            if (path.Length == 0)
                return top ? "" : null;
            return path.Split('/').All(ValidPart) ? path : null;
        }

        /// <summary>A usable file or folder name from any text (a player's name): bad characters become "_".</summary>
        public static string Safe(string name)
        {
            StringBuilder text = new StringBuilder();
            foreach (char c in (name ?? "").Trim())
                text.Append(c == '/' || Array.IndexOf(Invalid, c) >= 0 || char.IsControl(c) || Path.GetInvalidFileNameChars().Contains(c) ? '_' : c);
            string safe = text.ToString().Trim('.', ' ');
            return safe.Length > 0 ? safe : "_";
        }

        /// <summary>The part of a path below a folder ("houses/old/barn" below "houses" is "old/barn"; below "" all of it).</summary>
        public static string Below(string path, string folder) => folder.Length == 0 ? path : path.Substring(folder.Length + 1);

        /// <summary>The path is the folder itself or lies inside it ("" holds everything).</summary>
        public static bool Within(string path, string folder) =>
            folder.Length == 0 || string.Equals(path, folder, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
    }
}
