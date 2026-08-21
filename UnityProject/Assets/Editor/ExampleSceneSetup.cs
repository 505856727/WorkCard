using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WorkCard.Editor
{
    public static class ExampleSceneSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("WorkCard/创建示例场景")]
        public static void CreateExampleScene()
        {
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                EnsureBuildSettings();
                Debug.Log("示例场景已存在：" + ScenePath);
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var camera = Object.FindObjectOfType<Camera>();
            if (camera != null)
            {
                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
            }

            var go = new GameObject("GameEntry");
            go.AddComponent<WorkCard.GameEntry>();

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettings();
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("已创建示例场景 " + ScenePath + "，运行后会打开 ExampleWindow");
        }

        static void EnsureBuildSettings()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
