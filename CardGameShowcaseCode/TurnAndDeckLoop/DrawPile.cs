using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.TurnAndDeckLoop
{
    public sealed class DrawPile : MonoBehaviour
    {
        public event Action OnPilesChanged;
        public event Action OnDiscardReshuffled;

        [Tooltip("只在初始化时读取，运行时始终操作副本")]
        [SerializeField] private List<CardDefinitionSO> _startingDeck =
            new List<CardDefinitionSO>();

        private readonly List<CardDefinitionSO> _drawCards =
            new List<CardDefinitionSO>();
        private readonly List<CardDefinitionSO> _discardedCards =
            new List<CardDefinitionSO>();

        public int DrawCount => _drawCards.Count;
        public int DiscardCount => _discardedCards.Count;

        private void Awake()
        {
            ResetDeck();
        }

        public bool TryDraw(out CardDefinitionSO card)
        {
            if (_drawCards.Count == 0)
            {
                ReshuffleDiscardPile();
            }

            if (_drawCards.Count == 0)
            {
                card = null;
                return false;
            }

            int topIndex = _drawCards.Count - 1;
            card = _drawCards[topIndex];
            _drawCards.RemoveAt(topIndex);
            OnPilesChanged?.Invoke();
            return true;
        }

        public void Discard(CardDefinitionSO card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            _discardedCards.Add(card);
            OnPilesChanged?.Invoke();
        }

        private void ResetDeck()
        {
            _drawCards.Clear();
            _discardedCards.Clear();
            _drawCards.AddRange(_startingDeck);
            Shuffle(_drawCards);
            OnPilesChanged?.Invoke();
        }

        private void ReshuffleDiscardPile()
        {
            if (_discardedCards.Count == 0)
            {
                return;
            }

            _drawCards.AddRange(_discardedCards);
            _discardedCards.Clear();
            Shuffle(_drawCards);
            OnDiscardReshuffled?.Invoke();
            OnPilesChanged?.Invoke();
        }

        private static void Shuffle(List<CardDefinitionSO> cards)
        {
            // Fisher–Yates：每个位置只与尚未确定的区间交换。
            for (int index = 0; index < cards.Count; index++)
            {
                int randomIndex = UnityEngine.Random.Range(index, cards.Count);
                CardDefinitionSO selectedCard = cards[index];
                cards[index] = cards[randomIndex];
                cards[randomIndex] = selectedCard;
            }
        }
    }
}
