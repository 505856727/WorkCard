using System;
using System.Collections.Generic;
using System.IO;
using WorkCard.LitJson;
using UnityEngine;

namespace WorkCard.Editor
{
    public static class IOHelper
    {
        public static string RelativeProjectPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var from = Path.GetFullPath(Application.dataPath).Replace("\\", "/");
            var to = Path.GetFullPath(path).Replace("\\", "/");
            if (!from.EndsWith("/"))
            {
                from += "/";
            }

            try
            {
                var rel = new Uri(from).MakeRelativeUri(new Uri(to + (Directory.Exists(to) ? "/" : ""))).ToString();
                return Uri.UnescapeDataString(rel).Replace("\\", "/");
            }
            catch
            {
                return to;
            }
        }

        public static string FullProjectPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, path));
        }

        public static bool PathExists(string path) => Directory.Exists(path);
        public static bool FileExists(string file) => File.Exists(file);
        public static bool FilePathExists(string file) => Directory.Exists(Path.GetDirectoryName(file));

        public static string ReadText(string file) => File.Exists(file) ? File.ReadAllText(file) : null;

        public static JsonData ReadJson(string file)
        {
            var text = ReadText(file);
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            try
            {
                return JsonMapper.ToObject(text);
            }
            catch
            {
                return null;
            }
        }

        public static bool WriteText(string file, string text)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(file, text);
            return true;
        }

        public static bool WriteJson(string file, JsonData jsonData, bool prettyPrint = false)
        {
            var writer = new JsonWriter { PrettyPrint = prettyPrint };
            jsonData.ToJson(writer);
            return WriteText(file, writer.ToString());
        }

        public static List<string> GetFiles(string path, bool recursive = true)
        {
            var files = new List<string>();
            if (!Directory.Exists(path))
            {
                return files;
            }

            Collect(path, files, recursive);
            return files;
        }

        static void Collect(string path, List<string> files, bool recursive)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                if (!file.EndsWith(".DS_Store"))
                {
                    files.Add(file);
                }
            }

            if (!recursive)
            {
                return;
            }

            foreach (var dir in Directory.GetDirectories(path))
            {
                Collect(dir, files, true);
            }
        }
    }
}
