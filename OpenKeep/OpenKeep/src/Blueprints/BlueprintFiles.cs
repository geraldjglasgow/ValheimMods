using System;
using System.IO;
using System.Linq;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Making, renaming and moving blueprint files and folders in the library, never touching what a file holds. A name
    /// is one file or folder name; a typed name with "/" is a path from the top folder ("houses/barn" moves a blueprint
    /// into the folder houses, made when missing; "/barn" moves it to the top), never with "." or ".." parts. Every
    /// refusal comes back as a message for the player.
    /// </summary>
    public static class BlueprintFiles
    {
        /// <summary>The longest name the name box takes (a path with folders, so more than one file name).</summary>
        public const int NameLimit = 120;

        /// <summary>Null when <paramref name="name"/> can be one file or folder name, else why not.</summary>
        public static string CheckName(string name)
        {
            bool bad = string.IsNullOrWhiteSpace(name) || name != name.Trim() || name == "." || name == ".."
                || name.EndsWith(".") || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
            return bad ? BlueprintWords.Format(BlueprintWords.BadName, name ?? "") : null;
        }

        /// <summary>
        /// The path a typed name means: a plain name stays in <paramref name="folder"/>, a name with "/" is a path from the
        /// top (spaces around a "/" dropped). Null (with the reason) when a part is not a valid name.
        /// </summary>
        public static string Target(string typed, string folder, out string error)
        {
            string trimmed = typed.Trim();
            bool fromTop = trimmed.Contains("/");
            string[] parts = trimmed.Trim('/').Split('/').Select(part => part.Trim()).ToArray();
            error = parts.Select(CheckName).FirstOrDefault(e => e != null);
            if (error != null)
                return null;
            string path = string.Join("/", parts);
            return fromTop ? path : BlueprintLibrary.Join(folder, path);
        }

        /// <summary>Makes a folder in <paramref name="parent"/>; false with the reason when the name is bad or taken.</summary>
        public static bool CreateFolder(string parent, string name, out string path, out string error)
        {
            path = Target(name, parent, out error);
            if (error != null)
                return false;
            string full = BlueprintLibrary.FullPath(path);
            if (Directory.Exists(full))
            {
                error = BlueprintWords.Format(BlueprintWords.Exists, path);
                return false;
            }
            return Run(() => Directory.CreateDirectory(full), out error);
        }

        /// <summary>
        /// Renames or moves a blueprint (<paramref name="folder"/> false) or a folder to what the player typed; false with
        /// the reason when it cannot (an unchanged name gives no reason). The library and the tab follow the move.
        /// </summary>
        public static bool Rename(string from, bool folder, string typed, out string to, out string error)
        {
            to = Target(typed, BlueprintLibrary.Parent(from), out error);
            if (error != null || to == from)
                return false;
            error = Refusal(from, to, folder);
            if (error != null)
                return false;
            string source = folder ? BlueprintLibrary.FullPath(from) : BlueprintLibrary.PathOf(from);
            string target = folder ? BlueprintLibrary.FullPath(to) : BlueprintLibrary.PathOf(to);
            if (!Run(() => Move(source, target, folder), out error))
                return false;
            BlueprintLibrary.Rescan();
            if (folder)
                BlueprintLibrary.FollowMove(from, to);
            return true;
        }

        /// <summary>Why a move cannot be made: the target exists (a change of case only is allowed) or a folder would go into itself.</summary>
        private static string Refusal(string from, string to, bool folder)
        {
            if (folder && to.StartsWith(from + "/", StringComparison.OrdinalIgnoreCase))
                return BlueprintWords.Format(BlueprintWords.IntoItself, from);
            bool caseOnly = string.Equals(from, to, StringComparison.OrdinalIgnoreCase);
            bool taken = folder ? Directory.Exists(BlueprintLibrary.FullPath(to)) || File.Exists(BlueprintLibrary.FullPath(to))
                : File.Exists(BlueprintLibrary.PathOf(to));
            return taken && !caseOnly ? BlueprintWords.Format(BlueprintWords.Exists, to) : null;
        }

        /// <summary>Moves through a temporary name, so a change of case alone also works on Windows.</summary>
        private static void Move(string source, string target, bool folder)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            string step = source + ".openkeep-move";
            if (folder)
            {
                Directory.Move(source, step);
                Directory.Move(step, target);
            }
            else
            {
                File.Move(source, step);
                File.Move(step, target);
            }
        }

        /// <summary>Runs a file operation; false with the system's reason when it throws.</summary>
        private static bool Run(Action action, out string error)
        {
            error = null;
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                Plugin.Log.LogWarning("OpenKeep: blueprint file operation failed: " + e.Message);
                return false;
            }
        }
    }
}
