namespace WorkCard.Config
{
    public interface IConfigItem
    {
        void OnLoad();
    }

    public interface IConfigItemWithId : IConfigItem
    {
        int id { get; set; }
    }

    public interface IConfigGroupItem : IConfigItem
    {
        int groupId { get; set; }
    }
}
