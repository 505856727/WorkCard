using System.Collections.Generic;
using UnityEngine;

namespace WorkCard.Config
{
    public class ConfigGroup<T> : ConfigLoader, IConfigTable where T : IConfigItem, new()
    {
        readonly Dictionary<int, List<T>> _groups = new Dictionary<int, List<T>>();
        readonly string _groupKey;

        public string Name { get; }
        public int groupCount => _groups.Count;
        public IReadOnlyDictionary<int, List<T>> groups => _groups;

        public ConfigGroup(string name, string groupKey)
        {
            Name = name;
            _groupKey = groupKey;
            if (string.IsNullOrEmpty(groupKey))
            {
                Debug.LogError($"ConfigGroup（{name}）未设置分组字段 GroupKey");
            }
        }

        public virtual void Load(byte[] data)
        {
            Initialize(data);
            ClearItems();
            for (var i = 0; i < itemCount; i++)
            {
                var item = new T();
                LoadItem(item);
                AddToGroup(item);
                OnItemLoaded(item);
            }

            Reset();
        }

        public virtual void Unload() => ClearItems();

        public bool HasGroup(int groupId) => _groups.ContainsKey(groupId);

        public List<T> GetGroup(int groupId)
        {
            if (_groups.TryGetValue(groupId, out var list))
            {
                return list;
            }

            Debug.LogWarning($"ConfigGroup（{Name}）分组（{groupId}）不存在");
            return null;
        }

        public bool TryGetGroup(int groupId, out List<T> list) => _groups.TryGetValue(groupId, out list);

        public int GetGroupItemCount(int groupId) =>
            _groups.TryGetValue(groupId, out var list) ? list.Count : 0;

        protected virtual void OnItemLoaded(T item)
        {
        }

        protected virtual void ClearItems() => _groups.Clear();

        void AddToGroup(T item)
        {
            var groupId = ConfigItemAccess.GetGroupId(item, _groupKey);
            if (!_groups.TryGetValue(groupId, out var list))
            {
                list = new List<T>();
                _groups[groupId] = list;
            }

            list.Add(item);
        }
    }
}
