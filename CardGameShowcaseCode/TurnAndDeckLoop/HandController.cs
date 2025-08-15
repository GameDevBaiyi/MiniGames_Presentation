using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.TurnAndDeckLoop
{
    public sealed class HandController : MonoBehaviour
    {
        public event Action<CardDefinitionSO> OnCardPlayed;

        [Header("依赖")]
        [SerializeField] private TurnController _turnController;
        [SerializeField] private DrawPile _drawPile;
        [SerializeField] private CardView _cardPrefab;
        [SerializeField] private Transform[] _cardSlots = Array.Empty<Transform>();

        [Header("手牌")]
        [Min(1)]
        [SerializeField] private int _maximumHandSize = 5;
        [Min(0f)]
        [SerializeField] private float _playedCardHoldTime = 0.3f;

        private readonly List<CardView> _cards = new List<CardView>();

        private CancellationTokenSource _lifetimeCancellationSource;

        private void OnEnable()
        {
            _lifetimeCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                destroyCancellationToken);
            _turnController.OnPlayerTurnStarted += HandlePlayerTurnStarted;
            _turnController.OnPlayerTurnEnded += DisableHand;
        }

        private void OnDisable()
        {
            _turnController.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
            _turnController.OnPlayerTurnEnded -= DisableHand;
            _lifetimeCancellationSource?.Cancel();
            _lifetimeCancellationSource?.Dispose();
            _lifetimeCancellationSource = null;
        }

        public bool TryPlay(CardView card)
        {
            if (card == null || !_cards.Contains(card))
            {
                return false;
            }

            CardDefinitionSO definition = card.Definition;
            if (!_turnController.TrySpendActions(definition.ActionCost))
            {
                return false;
            }

            DisableHand();
            _cards.Remove(card);
            _drawPile.Discard(definition);
            OnCardPlayed?.Invoke(definition);
            ReleasePlayedCardAsync(
                card,
                _lifetimeCancellationSource.Token).Forget();
            return true;
        }

        private void HandlePlayerTurnStarted()
        {
            RefillHand();
            SetHandInteractable(true);
        }

        private void RefillHand()
        {
            int targetCount = Mathf.Min(_maximumHandSize, _cardSlots.Length);

            while (_cards.Count < targetCount)
            {
                if (!_drawPile.TryDraw(out CardDefinitionSO definition))
                {
                    break;
                }

                CardView card = Instantiate(_cardPrefab, transform);
                card.Init(definition, this);
                _cards.Add(card);
            }

            RepositionCards();
        }

        private async UniTask ReleasePlayedCardAsync(
            CardView card,
            CancellationToken cancellationToken)
        {
            card.PlayFeedback();
            TimeSpan delay = TimeSpan.FromSeconds(_playedCardHoldTime);
            await UniTask.Delay(delay, cancellationToken: cancellationToken);
            Destroy(card.gameObject);
            RepositionCards();

            if (_turnController.CanPlayCard)
            {
                SetHandInteractable(true);
            }
        }

        private void DisableHand()
        {
            SetHandInteractable(false);
        }

        private void SetHandInteractable(bool isInteractable)
        {
            for (int index = 0; index < _cards.Count; index++)
            {
                _cards[index].SetInteractable(isInteractable);
            }
        }

        private void RepositionCards()
        {
            for (int index = 0; index < _cards.Count; index++)
            {
                Transform cardTransform = _cards[index].transform;
                cardTransform.SetParent(_cardSlots[index]);
                cardTransform.localPosition = Vector3.zero;
            }
        }
    }
}
