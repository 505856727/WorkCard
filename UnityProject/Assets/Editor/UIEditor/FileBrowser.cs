using System;

namespace WorkCard.Editor
{
    public static class FileBrowser
    {
        public static void Open(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var mac = UnityEngine.SystemInfo.operatingSystem.IndexOf("Mac OS", StringComparison.Ordinal) >= 0;
            if (mac)
            {
                var macPath = path.Replace("\\", "/");
                var args = DirectoryExists(macPath) ? "\"" + macPath + "\"" : "-R \"" + macPath + "\"";
                System.Diagnostics.Process.Start("open", args);
            }
            else
            {
                var winPath = path.Replace("/", "\\");
                System.Diagnostics.Process.Start("explorer.exe", DirectoryExists(winPath) ? "/root," + winPath : "/select," + winPath);
            }
        }

        static bool DirectoryExists(string path) => System.IO.Directory.Exists(path);
    }
}
