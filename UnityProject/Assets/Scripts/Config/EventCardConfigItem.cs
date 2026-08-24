using WorkCard.Config;

namespace Game
{
    [Config("EventCardConfig", ConfigKind.Map)]
    public class EventCardConfigItem : IConfigItem
    {
        public int id;
        public string name;
        public string desc;
        public string image;

        public void OnLoad()
        {
        }

        public EventCard ToCard()
        {
            return new EventCard
            {
                name = name,
                desc = desc,
                image = image,
            };
        }
    }
}
