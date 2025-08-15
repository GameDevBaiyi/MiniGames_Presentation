using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGunner.WeaponPooling
{
    // 弹药、枪口特效等高频出现的对象共用的对象池：每种 Prefab 各自维护一个队列，
    // 用完不销毁，直接禁用后放回队尾，下次直接复用。
    public sealed class PoolManager : MonoBehaviour
    {
        [Serializable]
        private struct PoolDefinition
        {
            [SerializeField] private Component _prefab;
            [SerializeField] private int _initialSize;

            public Component Prefab => _prefab;
            public int InitialSize => _initialSize;
        }

        [SerializeField] private PoolDefinition[] _pools = Array.Empty<PoolDefinition>();

        private readonly Dictionary<int, Queue<Component>> _poolsByPrefabId = new Dictionary<int, Queue<Component>>();
        private Transform _poolRoot;

        private void Awake()
        {
            _poolRoot = transform;

            foreach (PoolDefinition definition in _pools)
            {
                CreatePool(definition.Prefab, definition.InitialSize);
            }
        }

        // 取出一个可复用的实例并摆到指定位置。如果 prefab 没有预先在 Inspector 里注册池，
        // 直接抛异常而不是静默返回 null——调用方忘记检查空引用时能立刻定位问题，而不是在别处崩掉。
        public T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            int prefabId = prefab.GetInstanceID();

            if (!_poolsByPrefabId.TryGetValue(prefabId, out Queue<Component> pool))
            {
                throw new InvalidOperationException($"Prefab '{prefab.name}' has no registered pool.");
            }

            Component instance = pool.Dequeue();
            pool.Enqueue(instance);

            instance.gameObject.SetActive(false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = prefab.transform.localScale;

            return (T)instance;
        }

        private void CreatePool(Component prefab, int size)
        {
            if (prefab == null)
            {
                return;
            }

            int prefabId = prefab.GetInstanceID();

            if (_poolsByPrefabId.ContainsKey(prefabId))
            {
                return;
            }

            Transform anchor = new GameObject(prefab.name + "Pool").transform;
            anchor.SetParent(_poolRoot);

            Queue<Component> pool = new Queue<Component>(size);

            for (int i = 0; i < size; i++)
            {
                Component instance = Instantiate(prefab, anchor);
                instance.gameObject.SetActive(false);
                pool.Enqueue(instance);
            }

            _poolsByPrefabId.Add(prefabId, pool);
        }
    }
}
