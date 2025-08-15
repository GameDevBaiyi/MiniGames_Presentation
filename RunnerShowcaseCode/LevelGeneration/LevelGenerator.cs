using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runner.LevelGeneration
{
    public sealed class LevelGenerator : MonoBehaviour
    {
        public event Action<float> MoveSpeedChanged;

        [Header("References")]
        [SerializeField] private Chunk[] _chunkPrefabs = Array.Empty<Chunk>();
        [SerializeField] private Chunk _checkpointChunkPrefab = null;
        [SerializeField] private Transform _chunkParent = null;
        [SerializeField] private Transform _recycleBoundary = null;

        [Header("Layout")]
        [Min(1)]
        [SerializeField] private int _startingChunkCount = 12;
        [Min(1)]
        [SerializeField] private int _checkpointInterval = 8;

        [Header("Pool")]
        [Min(0)]
        [SerializeField] private int _prewarmCountPerPrefab = 4;
        [Min(1)]
        [SerializeField] private int _poolDefaultCapacity = 8;
        [Min(1)]
        [SerializeField] private int _poolMaximumSize = 24;

        [Header("Speed")]
        [Min(0f)]
        [SerializeField] private float _startingMoveSpeed = 6f;
        [Min(0f)]
        [SerializeField] private float _minimumMoveSpeed = 2f;
        [Min(0f)]
        [SerializeField] private float _maximumMoveSpeed = 20f;
        [SerializeField] private float _minimumGravityZ = -22f;
        [SerializeField] private float _maximumGravityZ = -8f;

        // 队列顺序与跑道上的 Chunk 空间顺序一致。
        private Queue<Chunk> _activeChunks;
        private ChunkPool _chunkPool;
        private Vector3 _initialGravity;
        private float _moveSpeed;
        private float _nextSpawnPositionZ;
        private int _spawnedChunkCount;
        private bool _isInitialized;

        public float MoveSpeed => _moveSpeed;

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            float movementDistance = _moveSpeed * Time.deltaTime;

            MoveActiveChunks(movementDistance);
            _nextSpawnPositionZ -= movementDistance;
            RecyclePassedChunks();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void ChangeMoveSpeed(float amount)
        {
            float nextMoveSpeed = Mathf.Clamp(
                _moveSpeed + amount,
                _minimumMoveSpeed,
                _maximumMoveSpeed);

            if (Mathf.Approximately(nextMoveSpeed, _moveSpeed))
            {
                return;
            }

            _moveSpeed = nextMoveSpeed;
            ApplyGravityForCurrentSpeed();
            MoveSpeedChanged?.Invoke(_moveSpeed);
        }

        private void Initialize()
        {
            ValidateConfiguration();

            _activeChunks = new Queue<Chunk>(_startingChunkCount);
            _chunkPool = new ChunkPool(
                _chunkParent,
                _poolDefaultCapacity,
                _poolMaximumSize);

            RegisterChunkPrefabs();

            _initialGravity = Physics.gravity;
            _moveSpeed = Mathf.Clamp(
                _startingMoveSpeed,
                _minimumMoveSpeed,
                _maximumMoveSpeed);
            _nextSpawnPositionZ = transform.position.z;

            ApplyGravityForCurrentSpeed();

            for (int i = 0; i < _startingChunkCount; i++)
            {
                SpawnChunk();
            }

            _isInitialized = true;
        }

        private void RegisterChunkPrefabs()
        {
            for (int i = 0; i < _chunkPrefabs.Length; i++)
            {
                Chunk chunkPrefab = _chunkPrefabs[i];

                if (!_chunkPool.IsRegistered(chunkPrefab))
                {
                    _chunkPool.Register(chunkPrefab, _prewarmCountPerPrefab);
                }
            }

            if (!_chunkPool.IsRegistered(_checkpointChunkPrefab))
            {
                _chunkPool.Register(_checkpointChunkPrefab, _prewarmCountPerPrefab);
            }
        }

        private void SpawnChunk()
        {
            Chunk chunkPrefab = SelectChunkPrefab();
            Vector3 spawnPosition = new Vector3(
                transform.position.x,
                transform.position.y,
                _nextSpawnPositionZ);

            Chunk chunk = _chunkPool.Get(chunkPrefab, spawnPosition, Quaternion.identity);
            _activeChunks.Enqueue(chunk);

            _nextSpawnPositionZ += chunk.Length;
            _spawnedChunkCount++;
        }

        private Chunk SelectChunkPrefab()
        {
            bool isCheckpointDue =
                _spawnedChunkCount > 0 &&
                _spawnedChunkCount % _checkpointInterval == 0;

            if (isCheckpointDue)
            {
                return _checkpointChunkPrefab;
            }

            int prefabIndex = UnityEngine.Random.Range(0, _chunkPrefabs.Length);
            return _chunkPrefabs[prefabIndex];
        }

        private void MoveActiveChunks(float movementDistance)
        {
            foreach (Chunk chunk in _activeChunks)
            {
                Transform chunkTransform = chunk.transform;
                Vector3 position = chunkTransform.position;
                position.z -= movementDistance;
                chunkTransform.position = position;
            }
        }

        // 先回收越界 Chunk，再在队尾补足相同数量。
        private void RecyclePassedChunks()
        {
            int recycledChunkCount = 0;

            while (
                _activeChunks.Count > 0 &&
                HasPassedRecycleBoundary(_activeChunks.Peek()))
            {
                Chunk expiredChunk = _activeChunks.Dequeue();
                _chunkPool.Release(expiredChunk);
                recycledChunkCount++;
            }

            for (int i = 0; i < recycledChunkCount; i++)
            {
                SpawnChunk();
            }
        }

        private bool HasPassedRecycleBoundary(Chunk chunk)
        {
            float chunkEndPositionZ = chunk.transform.position.z + chunk.Length;
            return chunkEndPositionZ <= _recycleBoundary.position.z;
        }

        private void ApplyGravityForCurrentSpeed()
        {
            float normalizedSpeed = Mathf.InverseLerp(
                _minimumMoveSpeed,
                _maximumMoveSpeed,
                _moveSpeed);
            float gravityZ = Mathf.Lerp(
                _maximumGravityZ,
                _minimumGravityZ,
                normalizedSpeed);

            Physics.gravity = new Vector3(
                _initialGravity.x,
                _initialGravity.y,
                gravityZ);
        }

        private void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            while (_activeChunks.Count > 0)
            {
                _chunkPool.Release(_activeChunks.Dequeue());
            }

            _chunkPool.Dispose();
            Physics.gravity = _initialGravity;
            MoveSpeedChanged = null;
            _isInitialized = false;
        }

        private void ValidateConfiguration()
        {
            if (_chunkPrefabs == null || _chunkPrefabs.Length == 0)
            {
                throw new InvalidOperationException("At least one chunk prefab is required.");
            }

            for (int i = 0; i < _chunkPrefabs.Length; i++)
            {
                if (_chunkPrefabs[i] == null)
                {
                    throw new InvalidOperationException("Chunk prefab entries cannot be null.");
                }
            }

            if (_checkpointChunkPrefab == null)
            {
                throw new InvalidOperationException("A checkpoint chunk prefab is required.");
            }

            if (_chunkParent == null || _recycleBoundary == null)
            {
                throw new InvalidOperationException("Chunk parent and recycle boundary are required.");
            }

            if (_startingChunkCount < 1 || _checkpointInterval < 1)
            {
                throw new InvalidOperationException("Chunk counts and intervals must be positive.");
            }

            if (_minimumMoveSpeed < 0f || _minimumMoveSpeed >= _maximumMoveSpeed)
            {
                throw new InvalidOperationException("Move speed limits are invalid.");
            }

            if (_minimumGravityZ >= _maximumGravityZ)
            {
                throw new InvalidOperationException("Gravity Z limits are invalid.");
            }

            if (
                _poolDefaultCapacity < 1 ||
                _poolMaximumSize < _poolDefaultCapacity ||
                _prewarmCountPerPrefab < 0 ||
                _prewarmCountPerPrefab > _poolMaximumSize)
            {
                throw new InvalidOperationException("Pool capacity settings are invalid.");
            }
        }
    }
}
