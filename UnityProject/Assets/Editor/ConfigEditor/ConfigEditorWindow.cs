using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using WorkCard.Config;
using WorkCard.Editor;
using WorkCard.LitJson;

namespace WorkCard.Editor.Config
{
    public class ConfigEditorWindow : EditorWindow
    {
        class TableInfo
        {
            public string File;
            public string SheetName;
            public string ConfigName;
            public bool Interested = true;
            public ConfigKind Kind = ConfigKind.Map;
            public bool IndexFromOne;
            public string GroupKey = "";
        }

        public static string EditorConfigPath => Path.GetFullPath(Application.dataPath + "/../ConfigEditor");
        public static string EditorConfigFile => EditorConfigPath + "/config.json";

        string _excelPath = "";
        string _exportPath = "";
        string _csvPath = "";
        int _descRow = 1;
        int _keyRow = 3;
        int _typeRow = 2;
        readonly List<TableInfo> _tables = new List<TableInfo>();
        readonly Dictionary<string, TableInfo> _savedInfos = new Dictionary<string, TableInfo>();
        readonly List<string> _logs = new List<string>();
        Vector2 _tableScroll;
        Vector2 _logScroll;
        static readonly string[] KindNames =
        {
            "ConfigList", "ConfigMap", "ConfigGroup", "ConfigMapList", "ConfigGroupList", "ConfigGroupMap",
        };

        [MenuItem("WorkCard/配置编辑器")]
        static void OpenWindow()
        {
            var window = GetWindow<ConfigEditorWindow>("配置编辑器");
            window.minSize = new Vector2(960, 520);
            window.Show();
        }

        [MenuItem("WorkCard/导出配置")]
        public static void ExportNoUI()
        {
            var window = CreateInstance<ConfigEditorWindow>();
            window.LoadEditorConfig();
            window.RefreshTables();
            window.ExportAll();
            DestroyImmediate(window);
        }

        void OnEnable()
        {
            LoadEditorConfig();
            RefreshTables();
        }

        void OnGUI()
        {
            DrawPaths();
            EditorGUILayout.Space();
            DrawTables();
            EditorGUILayout.Space();
            DrawLogs();
        }

        void DrawPaths()
        {
            EditorGUILayout.LabelField("导出设置", EditorStyles.boldLabel);
            DrawFolder("配置表路径", ref _excelPath, RefreshTables);
            DrawFolder("二进制导出路径", ref _exportPath, null);
            DrawFolder("CSV导出路径", ref _csvPath, null);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("表头行（从1起）", GUILayout.Width(110));
            _descRow = EditorGUILayout.IntPopup("描述", _descRow, new[] { "1", "2", "3" }, new[] { 1, 2, 3 }, GUILayout.Width(120));
            _keyRow = EditorGUILayout.IntPopup("字段", _keyRow, new[] { "1", "2", "3" }, new[] { 1, 2, 3 }, GUILayout.Width(120));
            _typeRow = EditorGUILayout.IntPopup("类型", _typeRow, new[] { "1", "2", "3" }, new[] { 1, 2, 3 }, GUILayout.Width(120));
            if (GUI.changed)
            {
                if (_descRow == _keyRow || _descRow == _typeRow || _keyRow == _typeRow)
                {
                    Debug.LogError("行配置不可相同");
                }

                SaveEditorConfig();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新", GUILayout.Width(64)))
            {
                RefreshTables();
            }

            if (GUILayout.Button("导出全部", GUILayout.Width(80)))
            {
                ExportAll();
            }

            if (GUILayout.Button("导出关注", GUILayout.Width(80)))
            {
                ExportSelected();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawFolder(string label, ref string path, System.Action onChanged)
        {
            EditorGUILayout.BeginHorizontal();
            var next = EditorGUILayout.TextField(label, path);
            if (next != path)
            {
                path = next;
                SaveEditorConfig();
                onChanged?.Invoke();
            }

            if (GUILayout.Button("浏览", GUILayout.Width(48)))
            {
                var picked = EditorUtility.OpenFolderPanel("选择" + label, path, "");
                if (!string.IsNullOrEmpty(picked))
                {
                    path = picked;
                    SaveEditorConfig();
                    onChanged?.Invoke();
                }
            }

            if (GUILayout.Button("打开", GUILayout.Width(48)))
            {
                FileBrowser.Open(path);
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawTables()
        {
            EditorGUILayout.LabelField($"配置表（{_tables.Count}）", EditorStyles.boldLabel);
            _tableScroll = EditorGUILayout.BeginScrollView(_tableScroll, GUILayout.MinHeight(180));
            if (_tables.Count == 0)
            {
                EditorGUILayout.HelpBox("没有扫描到以 Config 结尾的 csv / xlsx。源表目录默认是仓库根目录 Tables/。", MessageType.Info);
            }

            foreach (var table in _tables)
            {
                EditorGUILayout.BeginHorizontal();
                table.Interested = EditorGUILayout.Toggle(table.Interested, GUILayout.Width(18));
                EditorGUILayout.LabelField(table.ConfigName, GUILayout.Width(180));
                var kind = (ConfigKind)EditorGUILayout.Popup((int)table.Kind, KindNames, GUILayout.Width(140));
                if (kind != table.Kind)
                {
                    table.Kind = kind;
                    Remember(table);
                    SaveEditorConfig();
                }

                if (ConfigKindUtil.IsList(table.Kind))
                {
                    var fromOne = EditorGUILayout.ToggleLeft("索引从1开始", table.IndexFromOne, GUILayout.Width(100));
                    if (fromOne != table.IndexFromOne)
                    {
                        table.IndexFromOne = fromOne;
                        Remember(table);
                        SaveEditorConfig();
                    }
                }

                if (ConfigKindUtil.IsGroup(table.Kind))
                {
                    EditorGUILayout.LabelField("分组key", GUILayout.Width(50));
                    var groupKey = EditorGUILayout.TextField(table.GroupKey, GUILayout.Width(90));
                    if (groupKey != table.GroupKey)
                    {
                        table.GroupKey = groupKey;
                        Remember(table);
                        SaveEditorConfig();
                    }
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("复制声明", GUILayout.Width(72)))
                {
                    CopyDefine(table);
                }

                if (GUILayout.Button("导出", GUILayout.Width(48)))
                {
                    ExportOne(table);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawLogs()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
            if (GUILayout.Button("清空", GUILayout.Width(48)))
            {
                _logs.Clear();
            }

            EditorGUILayout.EndHorizontal();
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.MinHeight(140));
            foreach (var log in _logs)
            {
                EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        void RefreshTables()
        {
            _tables.Clear();
            foreach (var file in ConfigExporter.ScanSourceFiles(_excelPath))
            {
                List<TableSheet> sheets;
                try
                {
                    sheets = ConfigExporter.LoadSourceFile(file);
                }
                catch (System.Exception e)
                {
                    Log("读取失败 " + Path.GetFileName(file) + "：" + e.Message);
                    continue;
                }

                foreach (var sheet in sheets)
                {
                    if (!TableParser.IsConfigName(sheet.Name))
                    {
                        continue;
                    }

                    var info = new TableInfo
                    {
                        File = file,
                        SheetName = sheet.Name,
                        ConfigName = TableParser.Pascalize(sheet.Name),
                    };
                    ApplyInfo(info);
                    _tables.Add(info);
                }
            }

            Repaint();
        }

        void ExportAll() => Export(_tables);

        void ExportSelected()
        {
            var selected = new List<TableInfo>();
            foreach (var table in _tables)
            {
                if (table.Interested)
                {
                    selected.Add(table);
                }
            }

            Export(selected);
        }

        void ExportOne(TableInfo table) => Export(new List<TableInfo> { table });

        void CopyDefine(TableInfo table)
        {
            try
            {
                var sheets = ConfigExporter.LoadSourceFile(table.File);
                foreach (var sheet in sheets)
                {
                    if (TableParser.Pascalize(sheet.Name) != table.ConfigName)
                    {
                        continue;
                    }

                    var desc = _descRow - 1;
                    var key = _keyRow - 1;
                    var type = _typeRow - 1;
                    var parsed = TableParser.Parse(sheet, desc, key, type, Math.Max(desc, Math.Max(key, type)) + 1);
                    if (parsed.Errors.Count > 0)
                    {
                        foreach (var error in parsed.Errors)
                        {
                            Log(error);
                        }

                        return;
                    }

                    EditorGUIUtility.systemCopyBuffer = ConfigScriptDefine.Generate(
                        parsed, table.Kind, table.IndexFromOne, table.GroupKey);
                    Log($"声明代码已复制到剪贴板: {table.ConfigName}");
                    return;
                }

                Log($"生成声明失败: 未找到表 {table.ConfigName}");
            }
            catch (System.Exception e)
            {
                Log("生成声明失败: " + e.Message);
            }
        }

        void Export(List<TableInfo> tables)
        {
            if (string.IsNullOrEmpty(_exportPath))
            {
                Log("未设置二进制导出路径");
                return;
            }

            Directory.CreateDirectory(_exportPath);
            if (!string.IsNullOrEmpty(_csvPath))
            {
                Directory.CreateDirectory(_csvPath);
            }

            var ok = 0;
            foreach (var table in tables)
            {
                List<TableSheet> sheets;
                try
                {
                    sheets = ConfigExporter.LoadSourceFile(table.File);
                }
                catch (System.Exception e)
                {
                    Log("读取失败 " + table.ConfigName + "：" + e.Message);
                    continue;
                }

                foreach (var sheet in sheets)
                {
                    if (TableParser.Pascalize(sheet.Name) != table.ConfigName)
                    {
                        continue;
                    }

                    var result = ConfigExporter.Export(
                        sheet, _exportPath, _csvPath, _descRow - 1, _keyRow - 1, _typeRow - 1);
                    if (result.Ok)
                    {
                        ok++;
                        Log($"导出 {result.Name}（{result.RowCount}行）→ {result.BytesPath}");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            Log(error);
                        }
                    }
                }
            }

            Log($"导出完成：{ok}/{tables.Count}");
            AssetDatabase.Refresh();
        }

        void Log(string message)
        {
            _logs.Add(message);
            Debug.Log("[Config] " + message);
            Repaint();
        }

        void LoadEditorConfig()
        {
            var json = IOHelper.ReadJson(EditorConfigFile);
            if (json == null)
            {
                _excelPath = Path.GetFullPath(Application.dataPath + "/../../Tables");
                _exportPath = Path.GetFullPath(Application.dataPath + "/Resources/Config");
                _csvPath = Path.GetFullPath(EditorConfigPath + "/csv");
                return;
            }

            _excelPath = IOHelper.FullProjectPath((string)json["ExcelPath"]);
            _exportPath = IOHelper.FullProjectPath((string)json["ExportPath"]);
            _csvPath = json.ContainsKey("CsvPath") ? IOHelper.FullProjectPath((string)json["CsvPath"]) : "";
            if (json.ContainsKey("DescRow"))
            {
                _descRow = (int)json["DescRow"];
            }

            if (json.ContainsKey("KeyRow"))
            {
                _keyRow = (int)json["KeyRow"];
            }

            if (json.ContainsKey("TypeRow"))
            {
                _typeRow = (int)json["TypeRow"];
            }

            _savedInfos.Clear();
            if (json.ContainsKey("Configs"))
            {
                foreach (var key in json["Configs"].Keys)
                {
                    var item = json["Configs"][key];
                    _savedInfos[key] = new TableInfo
                    {
                        ConfigName = key,
                        Kind = item.ContainsKey("Type") ? (ConfigKind)(int)item["Type"] : ConfigKind.Map,
                        IndexFromOne = item.ContainsKey("IndexFromOne") && (bool)item["IndexFromOne"],
                        GroupKey = item.ContainsKey("GroupKey") ? (string)item["GroupKey"] : "",
                    };
                }
            }
        }

        void SaveEditorConfig()
        {
            foreach (var table in _tables)
            {
                Remember(table);
            }

            var json = new JsonData();
            json.SetJsonType(JsonType.Object);
            json["ExcelPath"] = IOHelper.RelativeProjectPath(_excelPath);
            json["ExportPath"] = IOHelper.RelativeProjectPath(_exportPath);
            json["CsvPath"] = IOHelper.RelativeProjectPath(_csvPath);
            json["DescRow"] = _descRow;
            json["KeyRow"] = _keyRow;
            json["TypeRow"] = _typeRow;
            var configs = new JsonData();
            configs.SetJsonType(JsonType.Object);
            foreach (var kv in _savedInfos)
            {
                var info = new JsonData();
                info.SetJsonType(JsonType.Object);
                info["Type"] = (int)kv.Value.Kind;
                info["IndexFromOne"] = kv.Value.IndexFromOne;
                info["GroupKey"] = kv.Value.GroupKey ?? "";
                configs[kv.Key] = info;
            }

            json["Configs"] = configs;
            IOHelper.WriteJson(EditorConfigFile, json, true);
        }

        void Remember(TableInfo table)
        {
            _savedInfos[table.ConfigName] = table;
        }

        void ApplyInfo(TableInfo table)
        {
            if (_savedInfos.TryGetValue(table.ConfigName, out var saved))
            {
                table.Kind = saved.Kind;
                table.IndexFromOne = saved.IndexFromOne;
                table.GroupKey = saved.GroupKey ?? "";
                table.Interested = saved.Interested;
                return;
            }

            foreach (var (type, attr) in ConfigManager.Collect())
            {
                if (attr.Name != table.ConfigName)
                {
                    continue;
                }

                table.Kind = attr.Kind;
                table.IndexFromOne = attr.IndexFromOne;
                table.GroupKey = attr.GroupKey ?? "";
                break;
            }
        }
    }
}
