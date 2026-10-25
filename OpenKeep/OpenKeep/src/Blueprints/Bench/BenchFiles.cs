using System.Collections.Generic;
using System.IO;
using BlueprintShelf;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The bench window's left side as the hammer's own blueprint library: listings from <see cref="BlueprintLibrary"/>
    /// (its cache and version, so the Blueprints tab and the window follow each other), changes through
    /// <see cref="BlueprintFiles"/> (its names, checks and messages), a renamed blueprint followed by the tab's picks and pin
    /// (<see cref="BlueprintMenu.Moved"/>). A blueprint is shared only when the tab can read it.
    /// </summary>
    public sealed class BenchFiles : IShelfFiles
    {
        public int Version => BlueprintLibrary.Version;

        public bool Exists(string folder) => Directory.Exists(BlueprintLibrary.FullPath(folder));

        public List<string> Folders(string folder) => BlueprintLibrary.Folders(folder);

        public List<string> Files(string folder) => BlueprintLibrary.Names(folder);

        public List<string> Everything() => BlueprintLibrary.Everything(false);

        public string FileOf(string path) => BlueprintLibrary.PathOf(path);

        public bool Readable(string path) => BlueprintLibrary.Load(path) != null;

        public string Target(string typed, string folder, out string error) => BlueprintFiles.Target(typed, folder, out error);

        public bool CreateFolder(string parent, string typed, out string path, out string error)
        {
            bool made = BlueprintFiles.CreateFolder(parent, typed, out path, out error);
            BlueprintLibrary.Rescan();
            return made;
        }

        public bool Rename(string from, bool folder, string typed, out string to, out string error)
        {
            if (!BlueprintFiles.Rename(from, folder, typed, out to, out error))
                return false;
            if (!folder)
                BlueprintMenu.Moved(from, to);
            return true;
        }

        public bool Delete(string path, bool folder, out string error) => BlueprintFiles.Delete(path, folder, out error);

        public void Changed() => BlueprintLibrary.Rescan();
    }
}
