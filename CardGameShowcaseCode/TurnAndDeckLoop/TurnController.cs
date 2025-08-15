using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.TurnAndDeckLoop
{
    public sealed class TurnController : MonoBehaviour
    {
        public enum TurnPhaseEnum
        {
            Player,
            Boss,
            Finished
        }

        public event Action OnPlayerTurnStarted;
        public event Action OnPlayerTurnEnded;
        public event Action OnBossTurnStarted;
        public event Action OnBossTurnEnded;
        public event Action<int> OnActionPointsChanged;

        [Header("回合")]
        [Min(1)]
        [SerializeField] private int _maximumActions = 3;
        [Min(0f)]
        [Tooltip("Boss 行动前后的节奏留白（秒）")]
        [SerializeField] private float _bossActionDelay = 2f;

        [Header("界面")]
        [SerializeField] private TMP_Text _turnLabel;
        [SerializeField] private TMP_Text _actionPointsLabel;
        [SerializeField] private Button _endTurnButton;

        private CancellationTokenSource _battleCancellationSource;
        private int _actionPoints;

        public TurnPhaseEnum CurrentPhase { get; private set; } = TurnPhaseEnum.Player;
        public int ActionPoints => _actionPoints;
        public bool CanPlayCard =>
            CurrentPhase == TurnPhaseEnum.Player && _actionPoints > 0;

        private void Awake()
        {
            _battleCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                destroyCancellationToken);
        }

        private void Start()
        {
            BeginFirstPlayerTurnAsync(_battleCancellationSource.Token).Forget();
        }

        private void OnDestroy()
        {
            _battleCancellationSource?.Cancel();
            _battleCancellationSource?.Dispose();
        }

        public bool TrySpendActions(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (CurrentPhase != TurnPhaseEnum.Player || amount > _actionPoints)
            {
                return false;
            }

            _actionPoints -= amount;
            UpdateActionPoints();

            if (_actionPoints == 0)
            {
                EndPlayerTurn();
            }

            return true;
        }

        public void EndPlayerTurnFromUi()
        {
            if (CurrentPhase == TurnPhaseEnum.Player)
            {
                EndPlayerTurn();
            }
        }

        public void FinishBattle()
        {
            CurrentPhase = TurnPhaseEnum.Finished;
            _battleCancellationSource.Cancel();
            _turnLabel.text = string.Empty;
            _endTurnButton.interactable = false;
        }

        private async UniTask BeginFirstPlayerTurnAsync(
            CancellationToken cancellationToken)
        {
            // 首回合延迟一帧，确保牌组等组件已经完成 Start 初始化。
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            StartPlayerTurn();
        }

        private void StartPlayerTurn()
        {
            CurrentPhase = TurnPhaseEnum.Player;
            _actionPoints = _maximumActions;
            _turnLabel.text = "Player's Turn";
            _endTurnButton.interactable = true;
            UpdateActionPoints();
            OnPlayerTurnStarted?.Invoke();
        }

        private void EndPlayerTurn()
        {
            CurrentPhase = TurnPhaseEnum.Boss;
            _endTurnButton.interactable = false;
            OnPlayerTurnEnded?.Invoke();
            RunBossTurnAsync(_battleCancellationSource.Token).Forget();
        }

        private async UniTask RunBossTurnAsync(CancellationToken cancellationToken)
        {
            _turnLabel.text = "Boss's Turn";

            TimeSpan delay = TimeSpan.FromSeconds(_bossActionDelay);
            await UniTask.Delay(delay, cancellationToken: cancellationToken);
            OnBossTurnStarted?.Invoke();

            await UniTask.Delay(delay, cancellationToken: cancellationToken);
            OnBossTurnEnded?.Invoke();

            if (CurrentPhase != TurnPhaseEnum.Finished)
            {
                StartPlayerTurn();
            }
        }

        private void UpdateActionPoints()
        {
            _actionPointsLabel.text = _actionPoints.ToString();
            OnActionPointsChanged?.Invoke(_actionPoints);
        }
    }
}
