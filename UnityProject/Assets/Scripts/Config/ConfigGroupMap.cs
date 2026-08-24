using System.Collections.Generic;
using UnityEngine;

namespace WorkCard.Config
{
    public class ConfigGroupMap<T> : ConfigGroup<T> where T : IConfigItem, new()
    {
        readonly Dictionary<int, T> _items = new Dictionary<int, T>();

        public IReadOnlyDictionary<int, T> items => _items;

        public ConfigGroupMap(string name, string groupKey) : base(name, groupKey)
        {
        }

        public override void Unload()
        {
            base.Unload();
            _items.Clear();
        }

        public bool HasItem(int id) => _items.ContainsKey(id);

        public T GetItem(int id)
        {
            if (_items.TryGetValue(id, out var item))
            {
                return item;
            }

            Debug.LogWarning($"ConfigGroupMap（{Name}）配置数据（{id}）不存在");
            return default;
        }

        protected override void OnItemLoaded(T item) => _items[ConfigItemAccess.GetId(item)] = item;

        protected override void ClearItems()
        {
            base.ClearItems();
            _items.Clear();
        }
    }
}
