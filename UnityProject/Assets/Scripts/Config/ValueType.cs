using System.Collections.Generic;

namespace WorkCard.Config
{
    public enum ValueType
    {
        Null = 0,
        Bool = 1,
        SByte = 2,
        UByte = 3,
        Int16 = 4,
        UInt16 = 5,
        Int = 6,
        UInt = 7,
        Float = 8,
        Double = 9,
        String = 10,
        Array1 = 11,
        Array2 = 12,
        Vec2 = 13,
        Vec3 = 14,
        Size = 15,
        Color = 16,
        IndexString = 17,
        Buffer = 18,
        Expression = 19,
        Text = 20,
        IndexText = 21,
        Object = 99,
        Function = 100,
        Button = 9999,
        Time = 10000,
        Asset = 10001,
        EndType = 10002,
    }

    public enum ConfigKind
    {
        List = 0,
        Map = 1,
        Group = 2,
        MapList = 3,
        GroupList = 4,
        GroupMap = 5,
    }

    public static class ConfigKindUtil
    {
        public static bool IsList(ConfigKind kind) =>
            kind == ConfigKind.List || kind == ConfigKind.GroupList || kind == ConfigKind.MapList;

        public static bool IsGroup(ConfigKind kind) =>
            kind == ConfigKind.Group || kind == ConfigKind.GroupList || kind == ConfigKind.GroupMap;

        public static bool IsMap(ConfigKind kind) =>
            kind == ConfigKind.Map || kind == ConfigKind.MapList || kind == ConfigKind.GroupMap;
    }

    public static class ValueTypeUtil
    {
        static readonly Dictionary<string, ValueType> Names = new Dictionary<string, ValueType>
        {
            { "bool", ValueType.Bool },
            { "boolean", ValueType.Bool },
            { "int8", ValueType.SByte },
            { "sbyte", ValueType.SByte },
            { "uint8", ValueType.UByte },
            { "ubyte", ValueType.UByte },
            { "byte", ValueType.UByte },
            { "int16", ValueType.Int16 },
            { "short", ValueType.Int16 },
            { "uint16", ValueType.UInt16 },
            { "ushort", ValueType.UInt16 },
            { "int", ValueType.Int },
            { "int32", ValueType.Int },
            { "uint", ValueType.UInt },
            { "uint32", ValueType.UInt },
            { "float", ValueType.Float },
            { "double", ValueType.Double },
            { "string", ValueType.String },
            { "vec2", ValueType.Vec2 },
            { "vec3", ValueType.Vec3 },
            { "size", ValueType.Size },
            { "color", ValueType.Color },
            { "index_string", ValueType.IndexString },
            { "buffer", ValueType.Buffer },
            { "expression", ValueType.Expression },
            { "text", ValueType.Text },
            { "index_text", ValueType.IndexText },
            { "object", ValueType.Object },
            { "time", ValueType.Time },
            { "asset", ValueType.Asset },
        };

        static readonly Dictionary<string, string> CSharpNames = new Dictionary<string, string>
        {
            { "bool", "bool" },
            { "boolean", "bool" },
            { "int8", "sbyte" },
            { "sbyte", "sbyte" },
            { "uint8", "byte" },
            { "ubyte", "byte" },
            { "byte", "byte" },
            { "int16", "short" },
            { "short", "short" },
            { "uint16", "ushort" },
            { "ushort", "ushort" },
            { "int", "int" },
            { "int32", "int" },
            { "uint", "uint" },
            { "uint32", "uint" },
            { "float", "float" },
            { "double", "double" },
            { "string", "string" },
            { "vec2", "Vector2" },
            { "vec3", "Vector3" },
            { "size", "Vector2" },
            { "color", "uint" },
            { "index_string", "string" },
            { "buffer", "byte[]" },
            { "expression", "object" },
            { "text", "string" },
            { "index_text", "string" },
            { "object", "object" },
            { "time", "ushort" },
            { "asset", "string" },
        };

        public static bool TryParse(string typeString, out ValueType type, out ValueType elemType)
        {
            type = ValueType.Null;
            elemType = ValueType.Null;
            typeString = (typeString ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(typeString))
            {
                return false;
            }

            if (Names.TryGetValue(typeString, out type))
            {
                return true;
            }

            var bracket = typeString.IndexOf('[');
            if (bracket <= 0)
            {
                return false;
            }

            var elemName = typeString.Substring(0, bracket);
            var dim = typeString.Substring(bracket);
            if (!Names.TryGetValue(elemName, out elemType))
            {
                return false;
            }

            if (dim == "[]")
            {
                type = ValueType.Array1;
                return true;
            }

            if (dim == "[][]")
            {
                type = ValueType.Array2;
                return true;
            }

            return false;
        }

        public static ValueType GetExportType(ValueType type)
        {
            switch (type)
            {
                case ValueType.Time: return ValueType.UInt16;
                case ValueType.Asset:
                case ValueType.Text:
                case ValueType.IndexText: return ValueType.String;
                default: return type;
            }
        }

        public static string ToCSharpType(string typeString)
        {
            typeString = (typeString ?? "").Trim().ToLowerInvariant();
            var bracket = typeString.IndexOf('[');
            if (bracket < 0)
            {
                return CSharpNames.TryGetValue(typeString, out var name) ? name : "object";
            }

            var elem = typeString.Substring(0, bracket);
            var dim = typeString.Substring(bracket);
            var elemType = CSharpNames.TryGetValue(elem, out var mapped) ? mapped : "object";
            return dim == "[][]" ? elemType + "[][]" : elemType + "[]";
        }

        public static bool IsNumber(ValueType type) =>
            type is ValueType.SByte or ValueType.UByte or ValueType.Int16 or ValueType.UInt16
                or ValueType.Int or ValueType.UInt or ValueType.Float or ValueType.Double;

        public static bool IsString(ValueType type) =>
            type is ValueType.String or ValueType.IndexString or ValueType.Text
                or ValueType.IndexText or ValueType.Asset;
    }
}
