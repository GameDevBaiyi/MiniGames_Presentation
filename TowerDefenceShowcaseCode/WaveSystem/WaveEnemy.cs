using System;
using UnityEngine;

namespace Portfolio.TowerDefence.WaveSystem
{
    public sealed class WaveEnemy : MonoBehaviour
    {
        public event Action<WaveEnemy> Removed;

        [Min(1f)]
        [Tooltip("敌人的基础生命值，由当前波次倍率修正")]
        [SerializeField] private float _baseHealth = 10f;

        private float _currentHealth;
        private bool _hasLeftWave;

        public float CurrentHealth => _currentHealth;

        public void Init(float healthMultiplier)
        {
            _currentHealth = _baseHealth * healthMultiplier;
            _hasLeftWave = false;
        }

        public void TakeDamage(float damage)
        {
            if (_hasLeftWave)
            {
                return;
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            if (_currentHealth <= 0f)
            {
                LeaveWave();
            }
        }

        public void ReachGoal()
        {
            if (_hasLeftWave)
            {
                return;
            }

            LeaveWave();
        }

        private void LeaveWave()
        {
            _hasLeftWave = true;
            Removed?.Invoke(this);
        }
    }
}
