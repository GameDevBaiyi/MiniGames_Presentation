using System;
using UnityEngine;

namespace Portfolio.TowerDefence.WaveSystem
{
    public enum EnemyKind
    {
        Orc,
        Dragon,
        Kaiju,
        FairyOrc,
        PixieZombie,
        MummyOrc,
        WingedRabbit,
        Vampire,
        BugDragon,
        HorrorSnail,
        BehemothCrawler
    }

    [Serializable]
    public sealed class WaveDefinition
    {
        [Tooltip("本波生成的敌人类型")]
        [SerializeField] private EnemyKind _enemyKind;

        [Min(1)]
        [Tooltip("本波敌人总数")]
        [SerializeField] private int _enemyCount = 5;

        [Min(0.1f)]
        [Tooltip("同一波中两名敌人的生成间隔（秒）")]
        [SerializeField] private float _spawnInterval = 1f;

        public EnemyKind EnemyKind => _enemyKind;
        public int EnemyCount => _enemyCount;
        public float SpawnInterval => _spawnInterval;
    }
}
