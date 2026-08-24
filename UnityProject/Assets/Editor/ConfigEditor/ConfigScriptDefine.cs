using System.Collections.Generic;
using System.Text;
using WorkCard.Config;

namespace WorkCard.Editor.Config
{
    public static class ConfigScriptDefine
    {
        static readonly string[] Containers =
        {
            "ConfigList", "ConfigMap", "ConfigGroup", "ConfigMapList", "ConfigGroupList", "ConfigGroupMap",
        };

        public static string Generate(ParsedTable table, ConfigKind kind, bool indexFromOne, string groupKey)
        {
            var name = table.Name;
            var itemName = name + "Item";
            var container = Containers[(int)kind];
            var itemInterface = ConfigKindUtil.IsGroup(kind) ? "IConfigGroupItem" : "IConfigItem";
            var sb = new StringBuilder();
            var needUnity = false;
            foreach (var header in table.Headers)
            {
                var csharp = ValueTypeUtil.ToCSharpType(header.TypeString);
                if (csharp == "Vector2" || csharp == "Vector3")
                {
                    needUnity = true;
                }
            }

            sb.AppendLine("using WorkCard.Config;");
            if (needUnity)
            {
                sb.AppendLine("using UnityEngine;");
            }

            sb.AppendLine();
            sb.AppendLine($"public class {itemName} : {itemInterface}");
            sb.AppendLine("{");
            foreach (var header in table.Headers)
            {
                var desc = (header.Desc ?? "").Replace('\n', ' ').Replace('\r', ' ');
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {desc}");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public {ValueTypeUtil.ToCSharpType(header.TypeString)} {header.Key};");
                sb.AppendLine();
            }

            if (ConfigKindUtil.IsGroup(kind))
            {
                sb.AppendLine("    public int groupId { get; set; }");
                sb.AppendLine();
            }

            sb.AppendLine("    public void OnLoad()");
            sb.AppendLine("    {");
            if (ConfigKindUtil.IsGroup(kind) && !string.IsNullOrEmpty(groupKey))
            {
                var key = TableParser.Camelize(groupKey);
                var groupHeader = table.Headers.Find(h => h.Key == key || h.RawKey == groupKey);
                if (groupHeader != null)
                {
                    sb.AppendLine($"        groupId = {groupHeader.Key};");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"[Config(\"{name}\")]");
            sb.AppendLine($"public class {name} : {container}<{itemName}>");
            sb.AppendLine("{");
            sb.AppendLine($"    public static {name} Instance {{ get; private set; }}");
            sb.AppendLine();
            sb.AppendLine($"    public {name}() : base({BuildCtorArgs(name, kind, indexFromOne, groupKey)})");
            sb.AppendLine("    {");
            sb.AppendLine("        Instance = this;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString().Replace("\r\n", "\n");
        }

        static string BuildCtorArgs(string name, ConfigKind kind, bool indexFromOne, string groupKey)
        {
            var args = new List<string> { $"\"{name}\"" };
            if (ConfigKindUtil.IsGroup(kind))
            {
                var key = string.IsNullOrEmpty(groupKey) ? "分组key（Item属性名）" : TableParser.Camelize(groupKey);
                args.Add($"\"{key}\"");
            }

            if (ConfigKindUtil.IsList(kind))
            {
                args.Add(indexFromOne ? "true" : "false");
            }

            return string.Join(", ", args);
        }
    }
}
