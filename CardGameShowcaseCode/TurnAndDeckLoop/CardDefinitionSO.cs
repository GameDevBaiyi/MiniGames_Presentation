using UnityEngine;

namespace CardGame.TurnAndDeckLoop
{
    [CreateAssetMenu(
        fileName = "CardDefinition",
        menuName = "Portfolio/Card Game/Card Definition")]
    public sealed class CardDefinitionSO : ScriptableObject
    {
        [Header("显示")]
        [SerializeField] private string _displayName = string.Empty;
        [TextArea]
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private Sprite _illustration;

        [Header("战斗")]
        [Min(0)]
        [SerializeField] private int _actionCost = 1;
        [Min(0)]
        [SerializeField] private int _damage;
        [Min(0)]
        [SerializeField] private int _healing;

        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Illustration => _illustration;
        public int ActionCost => _actionCost;
        public int Damage => _damage;
        public int Healing => _healing;
    }
}
