using TMPro;
using UnityEngine;

namespace CardGame.TurnAndDeckLoop
{
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _illustration;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private TMP_Text _actionCostLabel;
        [SerializeField] private Collider2D _interactionCollider;
        [SerializeField] private ParticleSystem _playEffect;

        private HandController _owner;

        public CardDefinitionSO Definition { get; private set; }

        public void Init(CardDefinitionSO definition, HandController owner)
        {
            Definition = definition;
            _owner = owner;

            _illustration.sprite = definition.Illustration;
            _nameLabel.text = definition.DisplayName;
            _descriptionLabel.text = definition.Description;
            _actionCostLabel.text = definition.ActionCost.ToString();
        }

        public void SetInteractable(bool isInteractable)
        {
            _interactionCollider.enabled = isInteractable;
        }

        public void PlayFeedback()
        {
            _playEffect.Play();
        }

        // 实际项目由拖拽结束时的出牌区检测调用同一入口。
        public void RequestPlay()
        {
            _owner.TryPlay(this);
        }
    }
}
