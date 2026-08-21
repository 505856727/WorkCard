using System.Collections.Generic;
using System.IO;
using WorkCard.LitJson;
using WorkCard.UI;
using UnityEditor;
using UnityEngine;

namespace WorkCard.Editor
{
    public class UIEditorWindow : EditorWindow
    {
        public const string UIConfigFileName = "ui_config.json";
        public static string EditorConfigPath => Path.GetFullPath(Application.dataPath + "/../UIEditor");
        public static string EditorConfigFile => EditorConfigPath + "/config.json";

        static readonly FGUIReader Reader = new FGUIReader();

        string _fguiPath = "";
        string _exportPath = "";
        readonly List<string> _excludeDepPackages = new List<string>();
        string _packageFilter = "None";
        string _className;
        EditorUIClass _selected;
        Vector2 _scroll;
        string _newExclude = "";

        [MenuItem("WorkCard/UI编辑器 %#D")]
        static void OpenWindow()
        {
            var window = GetWindow<UIEditorWindow>("UI编辑器");
            window.minSize = new Vector2(720, 520);
            window.Show();
        }

        [MenuItem("WorkCard/导出UI配置")]
        public static void ExportNoUI()
        {
            var window = CreateInstance<UIEditorWindow>();
            window.LoadEditorConfig();
            window.DoExport();
            DestroyImmediate(window);
        }

        void OnEnable()
        {
            LoadEditorConfig();
            Reload();
        }

        void Reload()
        {
            if (Directory.Exists(_fguiPath))
            {
                Reader.LoadFGUIProject(_fguiPath);
            }

            Reader.RefreshUIClasses();
            SelectClass(_className);
        }

        void LoadEditorConfig()
        {
            var json = IOHelper.ReadJson(EditorConfigFile);
            if (json == null)
            {
                _fguiPath = Path.GetFullPath(Application.dataPath + "/../../FGUIProject");
                _exportPath = Path.GetFullPath(Application.dataPath + "/UI/UI");
                return;
            }

            _fguiPath = IOHelper.FullProjectPath((string)json["FGUIPath"]);
            _exportPath = IOHelper.FullProjectPath((string)json["ExportPath"]);
            _excludeDepPackages.Clear();
            if (json.ContainsKey("ExcludeDepPackages"))
            {
                foreach (JsonData item in json["ExcludeDepPackages"])
                {
                    _excludeDepPackages.Add((string)item);
                }
            }
        }

        void SaveEditorConfig()
        {
            var json = new JsonData();
            json.SetJsonType(JsonType.Object);
            json["FGUIPath"] = IOHelper.RelativeProjectPath(_fguiPath);
            json["ExportPath"] = IOHelper.RelativeProjectPath(_exportPath);
            var exclude = new JsonData();
            exclude.SetJsonType(JsonType.Array);
            foreach (var pkg in _excludeDepPackages)
            {
                exclude.Add(pkg);
            }

            json["ExcludeDepPackages"] = exclude;
            IOHelper.WriteJson(EditorConfigFile, json, true);
        }

        void OnGUI()
        {
            DrawPaths();
            EditorGUILayout.Space();
            DrawClassPickers();
            EditorGUILayout.Space();
            DrawProps();
        }

        void DrawPaths()
        {
            EditorGUILayout.LabelField("导出设置", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _fguiPath = EditorGUILayout.TextField("FGUI项目路径", _fguiPath);
            if (GUILayout.Button("浏览", GUILayout.Width(48)))
            {
                var path = EditorUtility.OpenFolderPanel("选择 FGUI 工程", _fguiPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _fguiPath = path;
                    SaveEditorConfig();
                    Reload();
                }
            }

            if (GUILayout.Button("刷新", GUILayout.Width(48)))
            {
                Reload();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _exportPath = EditorGUILayout.TextField("UI配置导出路径", _exportPath);
            if (GUILayout.Button("浏览", GUILayout.Width(48)))
            {
                var path = EditorUtility.OpenFolderPanel("选择导出路径", _exportPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _exportPath = path;
                    SaveEditorConfig();
                }
            }

            if (GUILayout.Button("打开", GUILayout.Width(48)))
            {
                FileBrowser.Open(_exportPath);
            }

            if (GUILayout.Button("导出", GUILayout.Width(48)))
            {
                DoExport();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("忽略依赖包", GUILayout.Width(80));
            _newExclude = EditorGUILayout.TextField(_newExclude);
            if (GUILayout.Button("添加", GUILayout.Width(48)) && !string.IsNullOrEmpty(_newExclude))
            {
                if (!_excludeDepPackages.Contains(_newExclude))
                {
                    _excludeDepPackages.Add(_newExclude);
                    SaveEditorConfig();
                }

                _newExclude = "";
            }

            EditorGUILayout.EndHorizontal();
            for (var i = 0; i < _excludeDepPackages.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(_excludeDepPackages[i]);
                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    _excludeDepPackages.RemoveAt(i);
                    SaveEditorConfig();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawClassPickers()
        {
            var packages = new List<string> { "None" };
            foreach (var uiClass in Reader.UIClasses.Values)
            {
                if (!string.IsNullOrEmpty(uiClass.Package) && !packages.Contains(uiClass.Package))
                {
                    packages.Add(uiClass.Package);
                }
            }

            var pkgIndex = Mathf.Max(0, packages.IndexOf(_packageFilter));
            var newPkgIndex = EditorGUILayout.Popup("UI包", pkgIndex, packages.ToArray());
            if (newPkgIndex != pkgIndex)
            {
                _packageFilter = packages[newPkgIndex];
                _className = null;
                _selected = null;
            }

            var classNames = new List<string>();
            foreach (var kv in Reader.UIClasses)
            {
                if (_packageFilter != "None" && kv.Value.Package != _packageFilter)
                {
                    continue;
                }

                classNames.Add(kv.Key);
            }

            classNames.Sort();
            var classIndex = Mathf.Max(0, classNames.IndexOf(_className));
            if (classNames.Count == 0)
            {
                EditorGUILayout.HelpBox("没有扫描到 [UIClass] / [UICom] 类。请先手写绑定类。", MessageType.Info);
                return;
            }

            var newClassIndex = EditorGUILayout.Popup("UI类", classIndex, classNames.ToArray());
            if (classNames[newClassIndex] != _className)
            {
                SelectClass(classNames[newClassIndex]);
            }

            if (_selected != null)
            {
                EditorGUILayout.LabelField("包名", _selected.Package);
                EditorGUILayout.LabelField("组件名", _selected.Component);
                if (_selected.ComponentData == null)
                {
                    var msg = $"UI类（{_selected.Name}）关联FGUI组件（{_selected.Component}）不存在，请检查配置";
                    var old = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0f, 0f);
                    if (GUILayout.Button(msg, GUILayout.Height(48)))
                    {
                        Debug.LogError(msg);
                    }
                    GUI.backgroundColor = old;
                }
            }
        }

        void SelectClass(string className)
        {
            _className = className;
            _selected = null;
            if (string.IsNullOrEmpty(className) || !Reader.UIClasses.TryGetValue(className, out var uiClass))
            {
                return;
            }

            Reader.LoadClassConfig(uiClass, EditorConfigPath);
            _selected = uiClass;
        }

        void DrawProps()
        {
            if (_selected == null || _selected.ComponentData == null)
            {
                return;
            }

            EditorGUILayout.LabelField("属性绑定", EditorStyles.boldLabel);
            var paths = _selected.ComponentData.NodeNamePaths.ToArray();
            var pathOptions = new List<string> { "(未绑定)" };
            pathOptions.AddRange(paths);
            var clickOptions = new List<string> { "(无)" };
            clickOptions.AddRange(_selected.Functions);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var prop in _selected.Props)
            {
                var entry = _selected.PropConfig.GetOrCreate(prop.Name, prop.Type);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(prop.Name, GUILayout.Width(160));
                EditorGUILayout.LabelField(prop.Type.ToString(), GUILayout.Width(90));

                if (prop.Type == UIPropType.ArrayNode)
                {
                    EditorGUILayout.EndHorizontal();
                    DrawArray(entry, pathOptions, clickOptions);
                }
                else
                {
                    DrawSingle(entry, pathOptions, clickOptions);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawSingle(UIPropEntry entry, List<string> pathOptions, List<string> clickOptions)
        {
            var pathIndex = Mathf.Max(0, pathOptions.IndexOf(string.IsNullOrEmpty(entry.NamePath) ? "(未绑定)" : entry.NamePath));
            var newPathIndex = EditorGUILayout.Popup(pathIndex, pathOptions.ToArray());
            if (newPathIndex != pathIndex)
            {
                entry.NamePath = newPathIndex == 0 ? null : pathOptions[newPathIndex];
                entry.IdPath = _selected.ComponentData.GetIdPathByNamePath(entry.NamePath);
                _selected.PropConfig.Save();
            }

            var isNode = string.IsNullOrEmpty(entry.NamePath) || (!entry.NamePath.Contains("$") && !entry.NamePath.Contains("@"));
            if (isNode)
            {
                var clickIndex = Mathf.Max(0, clickOptions.IndexOf(string.IsNullOrEmpty(entry.Click) ? "(无)" : entry.Click));
                var newClickIndex = EditorGUILayout.Popup(clickIndex, clickOptions.ToArray(), GUILayout.Width(160));
                if (newClickIndex != clickIndex)
                {
                    entry.Click = newClickIndex == 0 ? null : clickOptions[newClickIndex];
                    _selected.PropConfig.Save();
                }
            }
        }

        void DrawArray(UIPropEntry entry, List<string> pathOptions, List<string> clickOptions)
        {
            entry.Elements ??= new List<UIPropEntry>();
            if (GUILayout.Button("+ 元素", GUILayout.Width(80)))
            {
                entry.Elements.Add(new UIPropEntry());
                _selected.PropConfig.Save();
            }

            for (var i = 0; i < entry.Elements.Count; i++)
            {
                var elem = entry.Elements[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(24));
                var pathIndex = Mathf.Max(0, pathOptions.IndexOf(string.IsNullOrEmpty(elem.NamePath) ? "(未绑定)" : elem.NamePath));
                var newPathIndex = EditorGUILayout.Popup(pathIndex, pathOptions.ToArray());
                if (newPathIndex != pathIndex)
                {
                    elem.NamePath = newPathIndex == 0 ? null : pathOptions[newPathIndex];
                    elem.IdPath = _selected.ComponentData.GetIdPathByNamePath(elem.NamePath);
                    _selected.PropConfig.Save();
                }

                var clickIndex = Mathf.Max(0, clickOptions.IndexOf(string.IsNullOrEmpty(elem.Click) ? "(无)" : elem.Click));
                var newClickIndex = EditorGUILayout.Popup(clickIndex, clickOptions.ToArray(), GUILayout.Width(140));
                if (newClickIndex != clickIndex)
                {
                    elem.Click = newClickIndex == 0 ? null : clickOptions[newClickIndex];
                    _selected.PropConfig.Save();
                }

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    entry.Elements.RemoveAt(i);
                    _selected.PropConfig.Save();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        void DoExport()
        {
            if (!Directory.Exists(_exportPath))
            {
                _exportPath = EditorUtility.OpenFolderPanel("选择导出路径", Application.dataPath, "");
            }

            if (!Directory.Exists(_exportPath))
            {
                return;
            }

            SaveEditorConfig();
            Reader.ExportAll(_fguiPath, _excludeDepPackages, EditorConfigPath, Path.Combine(_exportPath, UIConfigFileName));
            AssetDatabase.Refresh();
        }
    }
}
