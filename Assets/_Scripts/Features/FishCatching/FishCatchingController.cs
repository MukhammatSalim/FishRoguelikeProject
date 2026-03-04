using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FishRoguelike.Features.FishCatching
{
    /// <summary>
    /// Core controller for the fish-catching minigame.
    ///
    /// Flow:
    ///   1. StartFishing() is called (e.g. when the player casts a line).
    ///   2. A random arrow sequence is generated and shown on screen one by one.
    ///   3. The player must press matching arrow keys in order.
    ///      - Wrong press → mistake counted, icon flashes red, same arrow must be retried.
    ///      - Too many mistakes → OnFishEscaped.
    ///      - Time runs out → OnFishEscaped.
    ///      - All arrows correct → OnFishCaught.
    ///
    /// Wire up in the Inspector:
    ///   - config          → FishCatchConfig ScriptableObject
    ///   - sequenceDisplay → ArrowSequenceDisplay on your UI canvas
    ///   - inputHandler    → FishingInputHandler on any active GameObject
    /// </summary>
    public class FishCatchingController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [SerializeField] private FishCatchConfig       config;
        [SerializeField] private ArrowSequenceDisplay  sequenceDisplay;
        [SerializeField] private FishingInputHandler   inputHandler;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>All arrows matched correctly.</summary>
        public event Action OnFishCaught;

        /// <summary>Player ran out of mistakes or time.</summary>
        public event Action OnFishEscaped;

        /// <summary>
        /// Fired on every wrong key press.
        /// int = mistakes used so far, int = max allowed mistakes.
        /// </summary>
        public event Action<int, int> OnMistake;

        // ── State ─────────────────────────────────────────────────────────────────

        private List<ArrowDirection> _sequence;
        private int  _currentIndex;
        private int  _mistakeCount;
        private bool _isActive;
        private bool _roundFinished;
        private bool _roundSucceeded;

        private CancellationTokenSource _cts;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts a fishing attempt. Ignored if a round is already active.
        /// </summary>
        public async UniTask StartFishing()
        {
            if (_isActive) return;

            _isActive = true;
            _cts      = new CancellationTokenSource();

            try
            {
                await RunRound(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                sequenceDisplay.ClearIcons();
                sequenceDisplay.Hide();
            }
            finally
            {
                _isActive = false;
                _cts.Dispose();
                _cts = null;
            }
        }

        /// <summary>Cancels an active round without raising any result event.</summary>
        public void CancelFishing() => _cts?.Cancel();

        // ── Private logic ─────────────────────────────────────────────────────────

        private async UniTask RunRound(CancellationToken ct)
        {
            _sequence      = GenerateSequence(config.sequenceLength);
            _currentIndex  = 0;
            _mistakeCount  = 0;
            _roundFinished  = false;
            _roundSucceeded = false;

            sequenceDisplay.Show();
            await sequenceDisplay.ShowSequenceAnimated(_sequence, config.delayPerArrow, ct);

            inputHandler.OnArrowPressed += HandleArrowInput;
            inputHandler.SetActive(true);

            bool success = await WaitForRoundResult(ct);

            inputHandler.OnArrowPressed -= HandleArrowInput;
            inputHandler.SetActive(false);

            await UniTask.Delay(
                TimeSpan.FromSeconds(config.resultDisplayDuration),
                cancellationToken: ct);

            sequenceDisplay.ClearIcons();
            sequenceDisplay.Hide();

            if (success)
                OnFishCaught?.Invoke();
            else
                OnFishEscaped?.Invoke();
        }

        private async UniTask<bool> WaitForRoundResult(CancellationToken ct)
        {
            float elapsed = 0f;

            while (!_roundFinished)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                if (elapsed >= config.inputTimeLimit)
                    return false;

                await UniTask.Yield(ct);
            }

            return _roundSucceeded;
        }

        private void HandleArrowInput(ArrowDirection direction)
        {
            if (!_isActive || _roundFinished) return;

            if (direction == _sequence[_currentIndex])
            {
                sequenceDisplay.SetArrowState(_currentIndex, ArrowIconState.Correct);
                _currentIndex++;

                if (_currentIndex >= _sequence.Count)
                {
                    _roundSucceeded = true;
                    _roundFinished  = true;
                }
            }
            else
            {
                _mistakeCount++;
                OnMistake?.Invoke(_mistakeCount, config.maxMistakes);

                if (_mistakeCount > config.maxMistakes)
                {
                    // Too many mistakes — fail immediately.
                    sequenceDisplay.SetArrowState(_currentIndex, ArrowIconState.Wrong);
                    _roundSucceeded = false;
                    _roundFinished  = true;
                }
                else
                {
                    // Flash red, then let the player retry the same arrow.
                    FlashWrongAndRetry(_currentIndex, _cts.Token).Forget();
                }
            }
        }

        private async UniTaskVoid FlashWrongAndRetry(int index, CancellationToken ct)
        {
            sequenceDisplay.SetArrowState(index, ArrowIconState.Wrong);

            await UniTask.Delay(
                TimeSpan.FromSeconds(config.wrongFlashDuration),
                cancellationToken: ct);

            // Only reset if the round hasn't ended while we were waiting.
            if (!_roundFinished)
                sequenceDisplay.SetArrowState(index, ArrowIconState.Pending);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static List<ArrowDirection> GenerateSequence(int length)
        {
            var values   = (ArrowDirection[])Enum.GetValues(typeof(ArrowDirection));
            var sequence = new List<ArrowDirection>(length);

            for (int i = 0; i < length; i++)
                sequence.Add(values[Random.Range(0, values.Length)]);

            return sequence;
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
