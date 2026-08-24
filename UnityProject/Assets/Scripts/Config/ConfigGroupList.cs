using System.Collections.Generic;
using UnityEngine;

namespace WorkCard.Config
{
    public class ConfigGroupList<T> : ConfigGroup<T> where T : IConfigItem, new()
    {
        readonly List<T> _items = new List<T>();
        readonly int _startIndex;

        public IReadOnlyList<T> items => _items;

        public ConfigGroupList(string name, string groupKey, bool indexFromOne = false)
            : base(name, groupKey)
        {
            _startIndex = indexFromOne ? 1 : 0;
        }

        public override void Unload()
        {
            base.Unload();
            _items.Clear();
        }

        public T GetItem(int index)
        {
            var real = index - _startIndex;
            if (real < 0 || real >= _items.Count)
            {
                Debug.LogWarning($"ConfigGroupList（{Name}）指定索引（{index}）超出范围");
                return default;
            }

            return _items[real];
        }

        protected override void OnItemLoaded(T item) => _items.Add(item);

        protected override void ClearItems()
        {
            base.ClearItems();
            _items.Clear();
        }
    }
}
