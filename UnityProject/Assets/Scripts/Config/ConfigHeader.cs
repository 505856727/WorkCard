namespace WorkCard.Config
{
    public class ConfigHeader
    {
        public string Name;
        public ValueType Type;
        public ValueType ElemType;
        public string Desc;
        public string TypeString;

        public ConfigHeader()
        {
        }

        public ConfigHeader(BufferReader reader)
        {
            Type = reader.ReadType();
            if (Type == ValueType.Array1 || Type == ValueType.Array2)
            {
                ElemType = reader.ReadType();
            }

            Name = reader.ReadString();
        }
    }
}
