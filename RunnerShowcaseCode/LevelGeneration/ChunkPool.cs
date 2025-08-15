using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Runner.LevelGeneration
{
    public sealed class ChunkPool : IDisposable
    {
        // 每种预制体独立维护对象池，确保加权选择后仍能正确归还。
        private readonly Dictionary<Chunk, ObjectPool<Chunk>> _prefabPools =
            new Dictionary<Chunk, ObjectPool<Chunk>>();
        private readonly Dictionary<Chunk, Chunk> _instanceSourcePrefabs =
            new Dictionary<Chunk, Chunk>();
        private readonly Transform _parent;
        private readonly int _defaultCapacity;
        private readonly int _maximumSize;

        private bool _isDisposed;

        public ChunkPool(Transform parent, int defaultCapacity, int maximumSize)
        {
            _parent = parent != null
                ? parent
                : throw new ArgumentNullException(nameof(parent));

            if (defaultCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultCapacity));
            }

            if (maximumSize < defaultCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSize));
            }

            _defaultCapacity = defaultCapacity;
            _maximumSize = maximumSize;
        }

        public bool IsRegistered(Chunk prefab)
        {
            ThrowIfDisposed();

            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            return _prefabPools.ContainsKey(prefab);
        }

        public void Register(Chunk prefab, int prewarmCount)
        {
            ThrowIfDisposed();

            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (_prefabPools.ContainsKey(prefab))
            {
                throw new InvalidOperationException($"Chunk prefab '{prefab.name}' is already registered.");
            }

            if (prewarmCount < 0 || prewarmCount > _maximumSize)
            {
                throw new ArgumentOutOfRangeException(nameof(prewarmCount));
            }

            ObjectPool<Chunk> pool = new ObjectPool<Chunk>(
                createFunc: () => CreateInstance(prefab),
                actionOnGet: null,
                actionOnRelease: null,
                actionOnDestroy: DestroyInstance,
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize: _maximumSize);

            _prefabPools.Add(prefab, pool);
            Prewarm(pool, prewarmCount);
        }

        public Chunk Get(Chunk prefab, Vector3 position, Quaternion rotation)
        {
            ThrowIfDisposed();

            ObjectPool<Chunk> pool = GetPool(prefab);
            Chunk chunk = pool.Get();
            chunk.PrepareForUse(position, rotation);
            return chunk;
        }

        public void Release(Chunk chunk)
        {
            ThrowIfDisposed();

            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            if (!_instanceSourcePrefabs.TryGetValue(chunk, out Chunk sourcePrefab))
            {
                throw new InvalidOperationException("The chunk does not belong to this pool.");
            }

            chunk.Release();
            _prefabPools[sourcePrefab].Release(chunk);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            foreach (ObjectPool<Chunk> pool in _prefabPools.Values)
            {
                pool.Clear();
            }

            _prefabPools.Clear();

            if (_instanceSourcePrefabs.Count > 0)
            {
                List<Chunk> remainingInstances =
                    new List<Chunk>(_instanceSourcePrefabs.Keys);

                for (int i = 0; i < remainingInstances.Count; i++)
                {
                    Chunk instance = remainingInstances[i];

                    if (instance != null)
                    {
                        UnityEngine.Object.Destroy(instance.gameObject);
                    }
                }
            }

            _instanceSourcePrefabs.Clear();
        }

        private Chunk CreateInstance(Chunk prefab)
        {
            Chunk instance = UnityEngine.Object.Instantiate(prefab, _parent);
            _instanceSourcePrefabs.Add(instance, prefab);
            instance.Release();
            return instance;
        }

        private void DestroyInstance(Chunk instance)
        {
            _instanceSourcePrefabs.Remove(instance);

            if (instance != null)
            {
                UnityEngine.Object.Destroy(instance.gameObject);
            }
        }

        private ObjectPool<Chunk> GetPool(Chunk prefab)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (!_prefabPools.TryGetValue(prefab, out ObjectPool<Chunk> pool))
            {
                throw new KeyNotFoundException($"Chunk prefab '{prefab.name}' is not registered.");
            }

            return pool;
        }

        private static void Prewarm(ObjectPool<Chunk> pool, int count)
        {
            if (count == 0)
            {
                return;
            }

            List<Chunk> instances = new List<Chunk>(count);

            for (int i = 0; i < count; i++)
            {
                instances.Add(pool.Get());
            }

            for (int i = 0; i < instances.Count; i++)
            {
                pool.Release(instances[i]);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ChunkPool));
            }
        }
    }
}
