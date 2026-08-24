using System.Collections.Generic;
using UnityEngine;

namespace WorkCard.Config
{
    public class ConfigList<T> : ConfigLoader, IConfigTable where T : IConfigItem, new()
    {
        readonly List<T> _items = new List<T>();
        readonly int _startIndex;

        public string Name { get; }
        public IReadOnlyList<T> items => _items;

        public ConfigList(string name, bool indexFromOne = false)
        {
            Name = name;
            _startIndex = indexFromOne ? 1 : 0;
        }

        public virtual void Load(byte[] data)
        {
            Initialize(data);
            _items.Clear();
            for (var i = 0; i < itemCount; i++)
            {
                var item = new T();
                LoadItem(item);
                _items.Add(item);
            }

            Reset();
        }

        public virtual void Unload() => _items.Clear();

        public T GetItem(int index)
        {
            var real = index - _startIndex;
            if (real < 0 || real >= _items.Count)
            {
                Debug.LogWarning($"ConfigList（{Name}）指定索引（{index}）超出范围");
                return default;
            }

            return _items[real];
        }
    }
}