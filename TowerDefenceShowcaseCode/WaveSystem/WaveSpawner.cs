using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.TowerDefence.WaveSystem
{
    public interface IEnemyFactory
    {
        public WaveEnemy Create(EnemyKind enemyKind, Vector3 spawnPosition, float healthMultiplier);
        public void Release(WaveEnemy enemy);
    }

    public sealed class WaveSpawner : MonoBehaviour
    {
        public event Action<int> WaveStarted;
        public event Action MissionCompleted;

        [Tooltip("按顺序循环的波次配置")]
        [SerializeField] private List<WaveDefinition> _waves = new List<WaveDefinition>();

        [Tooltip("敌人的初始生成位置")]
        [SerializeField] private Transform _spawnPoint;

        [Min(0f)]
        [Tooltip("完成当前波到下一波开始之间的等待时间（秒）")]
        [SerializeField] private float _timeBetweenWaves = 1f;

        [Min(1)]
        [Tooltip("完成多少波后视为任务完成")]
        [SerializeField] private int _wavesToWin = 3;

        private readonly HashSet<WaveEnemy> _activeEnemies = new HashSet<WaveEnemy>();

        private IEnemyFactory _enemyFactory;
        private int _currentWaveIndex;
        private int _completedWaveCount;
        private int _spawnedEnemyCount;
        private int _removedEnemyCount;
        private float _spawnTimer;
        private float _nextWaveTimer;
        private bool _isWaitingForNextWave;
        private bool _isMissionComplete;
        private bool _isEndlessMode;

        private WaveDefinition CurrentWave => _waves[_currentWaveIndex];

        private void Start()
        {
            if (_enemyFactory == null)
            {
                Debug.LogError("敌人工厂尚未注入，无法开始生成敌人。", this);
                enabled = false;
                return;
            }

            if (_waves.Count == 0)
            {
                Debug.LogError("波次列表为空，无法开始生成敌人。", this);
                enabled = false;
                return;
            }

            BeginWave(0);
        }

        private void Update()
        {
            if (_isMissionComplete)
            {
                return;
            }

            if (_isWaitingForNextWave)
            {
                UpdateNextWaveCooldown();
                return;
            }

            UpdateCurrentWave();
        }

        public void Init(IEnemyFactory enemyFactory)
        {
            _enemyFactory = enemyFactory ?? throw new ArgumentNullException(nameof(enemyFactory));
        }

        public void EnableEndlessMode()
        {
            if (!_isMissionComplete)
            {
                return;
            }

            _isEndlessMode = true;
            _isMissionComplete = false;
            _isWaitingForNextWave = true;
            _nextWaveTimer = 0f;
        }

        private void UpdateNextWaveCooldown()
        {
            _nextWaveTimer -= Time.deltaTime;
            if (_nextWaveTimer > 0f)
            {
                return;
            }

            int nextWaveIndex = (_currentWaveIndex + 1) % _waves.Count;
            BeginWave(nextWaveIndex);
        }

        private void UpdateCurrentWave()
        {
            _spawnTimer -= Time.deltaTime;
            if (_spawnedEnemyCount < CurrentWave.EnemyCount && _spawnTimer <= 0f)
            {
                SpawnEnemy();
            }

            if (_spawnedEnemyCount >= CurrentWave.EnemyCount && _removedEnemyCount >= CurrentWave.EnemyCount)
            {
                CompleteCurrentWave();
            }
        }

        private void BeginWave(int waveIndex)
        {
            _currentWaveIndex = waveIndex;
            _spawnedEnemyCount = 0;
            _removedEnemyCount = 0;
            _spawnTimer = 0f;
            _isWaitingForNextWave = false;
            WaveStarted?.Invoke(_completedWaveCount + 1);
        }

        private void SpawnEnemy()
        {
            _spawnedEnemyCount++;
            _spawnTimer = CurrentWave.SpawnInterval;

            float healthMultiplier = 1f + (_completedWaveCount * 0.4f);
            WaveEnemy enemy = _enemyFactory.Create(
                CurrentWave.EnemyKind,
                _spawnPoint.position,
                healthMultiplier);

            enemy.Removed += HandleEnemyRemoved;
            _activeEnemies.Add(enemy);
        }

        private void HandleEnemyRemoved(WaveEnemy enemy)
        {
            enemy.Removed -= HandleEnemyRemoved;

            if (!_activeEnemies.Remove(enemy))
            {
                return;
            }

            _enemyFactory.Release(enemy);
            _removedEnemyCount++;
        }

        private void CompleteCurrentWave()
        {
            _completedWaveCount++;

            if (!_isEndlessMode && _completedWaveCount >= _wavesToWin)
            {
                _isMissionComplete = true;
                MissionCompleted?.Invoke();
                return;
            }

            _isWaitingForNextWave = true;
            _nextWaveTimer = _timeBetweenWaves;
        }
    }
}
