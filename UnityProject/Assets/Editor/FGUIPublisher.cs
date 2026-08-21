using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WorkCard.Editor
{
    public static class FGUIPublisher
    {
        static readonly string[] EditorCandidates =
        {
            "/Applications/FairyGUI-Editor.app/Contents/MacOS/FairyGUI-Editor",
            "/Applications/FairyGUI Editor.app/Contents/MacOS/FairyGUI Editor",
        };

        [MenuItem("WorkCard/发布FGUI")]
        public static void Publish()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            var fairyFile = Path.Combine(projectRoot, "FGUIProject/FGUIProject.fairy");
            if (!File.Exists(fairyFile))
            {
                UnityEngine.Debug.LogError("未找到 FGUI 工程：" + fairyFile);
                return;
            }

            var editor = FindEditor();
            if (string.IsNullOrEmpty(editor))
            {
                UnityEngine.Debug.LogError("未找到 FairyGUI-Editor。请打开 FGUIProject/FGUIProject.fairy，在编辑器里发布 Example 包到 Assets/UI。");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = editor,
                Arguments = "-batchmode -nographics -quit /export \"" + fairyFile + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using (var process = Process.Start(startInfo))
            {
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrEmpty(stdout))
                {
                    UnityEngine.Debug.Log(stdout);
                }

                if (process.ExitCode != 0)
                {
                    UnityEngine.Debug.LogError("发布 FGUI 失败\n" + stderr);
                    return;
                }
            }

            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("FGUI 已发布到 Assets/UI");
        }

        static string FindEditor()
        {
            foreach (var path in EditorCandidates)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }
    }
}
