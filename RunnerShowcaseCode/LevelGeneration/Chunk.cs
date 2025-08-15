using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runner.LevelGeneration
{
    public sealed class Chunk : MonoBehaviour
    {
        [Header("Layout")]
        [Min(0.1f)]
        [SerializeField] private float _length = 10f;
        [SerializeField] private bool _isContentRandomizationEnabled = true;
        [SerializeField] private LaneContent[] _lanes = Array.Empty<LaneContent>();

        [Header("Content")]
        [Range(0f, 1f)]
        [SerializeField] private float _appleSpawnChance = 0.3f;
        [Range(0f, 1f)]
        [SerializeField] private float _coinLineSpawnChance = 0.5f;

        private readonly List<int> _availableLaneIndices = new List<int>(3);

        public float Length => _length;

        private void Awake()
        {
            ValidateConfiguration();

            for (int i = 0; i < _lanes.Length; i++)
            {
                _availableLaneIndices.Add(i);
            }
        }

        // 车道内容预置在预制体中，Chunk 复用时仅切换激活状态。
        public void PrepareForUse(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            ResetLaneContent();

            if (_isContentRandomizationEnabled)
            {
                RandomizeLaneContent();
            }

            gameObject.SetActive(true);
        }

        public void Release()
        {
            gameObject.SetActive(false);
            ResetLaneContent();
        }

        private void RandomizeLaneContent()
        {
            ShuffleAvailableLaneIndices();

            int nextLaneIndex = 0;
            int maximumObstacleCount = _lanes.Length - 1;
            int obstacleCount = UnityEngine.Random.Range(0, maximumObstacleCount + 1);

            for (int i = 0; i < obstacleCount; i++)
            {
                int laneIndex = _availableLaneIndices[nextLaneIndex];
                _lanes[laneIndex].ShowRandomObstacle();
                nextLaneIndex++;
            }

            if (
                nextLaneIndex < _availableLaneIndices.Count &&
                UnityEngine.Random.value <= _appleSpawnChance)
            {
                int laneIndex = _availableLaneIndices[nextLaneIndex];
                _lanes[laneIndex].ShowApple();
                nextLaneIndex++;
            }

            if (
                nextLaneIndex < _availableLaneIndices.Count &&
                UnityEngine.Random.value <= _coinLineSpawnChance)
            {
                int laneIndex = _availableLaneIndices[nextLaneIndex];
                _lanes[laneIndex].ShowCoinLine();
            }
        }

        private void ShuffleAvailableLaneIndices()
        {
            for (int i = _availableLaneIndices.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                int laneIndex = _availableLaneIndices[i];
                _availableLaneIndices[i] = _availableLaneIndices[swapIndex];
                _availableLaneIndices[swapIndex] = laneIndex;
            }
        }

        private void ResetLaneContent()
        {
            for (int i = 0; i < _lanes.Length; i++)
            {
                if (_lanes[i] != null)
                {
                    _lanes[i].Reset();
                }
            }
        }

        private void ValidateConfiguration()
        {
            if (_length <= 0f)
            {
                throw new InvalidOperationException("Chunk length must be positive.");
            }

            if (_lanes == null)
            {
                _lanes = Array.Empty<LaneContent>();
            }

            if (!_isContentRandomizationEnabled)
            {
                return;
            }

            if (_lanes == null || _lanes.Length == 0)
            {
                throw new InvalidOperationException("Randomized chunks require lane content.");
            }

            for (int i = 0; i < _lanes.Length; i++)
            {
                if (_lanes[i] == null)
                {
                    throw new InvalidOperationException("Lane content entries cannot be null.");
                }

                _lanes[i].Validate(i);
            }
        }

        [Serializable]
        private sealed class LaneContent
        {
            [SerializeField] private GameObject[] _obstacleVariants = Array.Empty<GameObject>();
            [SerializeField] private GameObject _apple = null;
            [SerializeField] private GameObject _coinLine = null;

            public void Reset()
            {
                if (_obstacleVariants != null)
                {
                    for (int i = 0; i < _obstacleVariants.Length; i++)
                    {
                        if (_obstacleVariants[i] != null)
                        {
                            _obstacleVariants[i].SetActive(false);
                        }
                    }
                }

                if (_apple != null)
                {
                    _apple.SetActive(false);
                }

                if (_coinLine != null)
                {
                    _coinLine.SetActive(false);
                }
            }

            public void ShowRandomObstacle()
            {
                int obstacleIndex = UnityEngine.Random.Range(0, _obstacleVariants.Length);
                _obstacleVariants[obstacleIndex].SetActive(true);
            }

            public void ShowApple()
            {
                _apple.SetActive(true);
            }

            public void ShowCoinLine()
            {
                _coinLine.SetActive(true);
            }

            public void Validate(int laneIndex)
            {
                if (_obstacleVariants == null || _obstacleVariants.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Lane {laneIndex} requires at least one obstacle variant.");
                }

                for (int i = 0; i < _obstacleVariants.Length; i++)
                {
                    if (_obstacleVariants[i] == null)
                    {
                        throw new InvalidOperationException(
                            $"Lane {laneIndex} contains a null obstacle variant.");
                    }
                }

                if (_apple == null || _coinLine == null)
                {
                    throw new InvalidOperationException(
                        $"Lane {laneIndex} requires apple and coin line objects.");
                }
            }
        }
    }
}
