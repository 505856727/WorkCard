using System.Collections.Generic;
using UnityEngine;

namespace WorkCard.Config
{
    public class ConfigMap<T> : ConfigLoader, IConfigTable where T : IConfigItem, new()
    {
        readonly Dictionary<int, T> _items = new Dictionary<int, T>();
        readonly List<T> _listItems = new List<T>();

        public string Name { get; }
        public IReadOnlyDictionary<int, T> items => _items;
        public IReadOnlyList<T> listItems => _listItems;

        public ConfigMap(string name)
        {
            Name = name;
        }

        public virtual void Load(byte[] data)
        {
            Initialize(data);
            _items.Clear();
            _listItems.Clear();
            for (var i = 0; i < itemCount; i++)
            {
                var item = new T();
                LoadItem(item);
                _items[ConfigItemAccess.GetId(item)] = item;
                _listItems.Add(item);
            }

            Reset();
        }

        public virtual void Unload()
        {
            _items.Clear();
            _listItems.Clear();
        }

        public bool HasItem(int id) => _items.ContainsKey(id);

        public T GetItem(int id)
        {
            if (_items.TryGetValue(id, out var item))
            {
                return item;
            }

            Debug.LogWarning($"ConfigMap（{Name}）配置数据（{id}）不存在");
            return default;
        }
    }
}
