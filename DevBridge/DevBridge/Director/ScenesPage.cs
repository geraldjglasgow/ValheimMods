using System.IO;

namespace DevBridge.Director
{
    /// <summary>The Studio's Scenes tab (Director/scenes.html, embedded), added to the studio page before its body ends.</summary>
    internal static class ScenesPage
    {
        private const string Resource = "DevBridge.Director.scenes.html";

        internal static string AddTo(string page)
        {
            using (Stream stream = typeof(ScenesPage).Assembly.GetManifestResourceStream(Resource))
            {
                if (stream == null) return page;
                using (var reader = new StreamReader(stream)) return page.Replace("</body>", reader.ReadToEnd() + "\n</body>");
            }
        }
    }
}
