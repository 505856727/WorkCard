using System.Collections.Generic;
using UnityEngine;

namespace WorkCard.Config
{
    public class ConfigMapList<T> : ConfigLoader, IConfigTable where T : IConfigItem, new()
    {
        readonly List<T> _listItems = new List<T>();
        readonly Dictionary<int, T> _mapItems = new Dictionary<int, T>();
        readonly int _startIndex;

        public string Name { get; }
        public IReadOnlyList<T> listItems => _listItems;
        public IReadOnlyDictionary<int, T> mapItems => _mapItems;

        public ConfigMapList(string name, bool indexFromOne = false)
        {
            Name = name;
            _startIndex = indexFromOne ? 1 : 0;
        }

        public void Load(byte[] data)
        {
            Initialize(data);
            _listItems.Clear();
            _mapItems.Clear();
            for (var i = 0; i < itemCount; i++)
            {
                var item = new T();
                LoadItem(item);
                _listItems.Add(item);
                _mapItems[ConfigItemAccess.GetId(item)] = item;
            }

            Reset();
        }

        public void Unload()
        {
            _listItems.Clear();
            _mapItems.Clear();
        }

        public T GetItemByIndex(int index)
        {
            var real = index - _startIndex;
            if (real < 0 || real >= _listItems.Count)
            {
                Debug.LogWarning($"ConfigMapList（{Name}）指定索引（{index}）超出范围");
                return default;
            }

            return _listItems[real];
        }

        public bool HasItem(int id) => _mapItems.ContainsKey(id);

        public T GetItemById(int id)
        {
            if (_mapItems.TryGetValue(id, out var item))
            {
                return item;
            }

            Debug.LogWarning($"ConfigMapList（{Name}）配置数据（{id}）不存在");
            return default;
        }
    }
}
