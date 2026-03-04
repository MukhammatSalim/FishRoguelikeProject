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
    ///   2. A random arrow sequence is generated and shown on screen one-by-one.
    ///   3. The player must press the matching arrow keys in order.
    ///   4. OnFishCaught fires on success; OnFishEscaped fires on wrong input or timeout.
    ///
    /// Wire up in the Inspector:
    ///   - sequenceDisplay  → ArrowSequenceDisplay on your UI canvas
    ///   - inputHandler     → FishingInputHandler on any active GameObject
    /// </summary>
    public class FishCatchingController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Sequence Settings")]
        [Tooltip("How many arrows are in the sequence.")]
        [SerializeField, Range(1, 10)] private int sequenceLength = 4;

        [Tooltip("Time in seconds the player has to complete the full sequence.")]
        [SerializeField] private float inputTimeLimit = 5f;

        [Tooltip("Delay between each arrow appearing during the show phase.")]
        [SerializeField] private float delayPerArrow = 0.4f;

        [Header("References")]
        [SerializeField] private ArrowSequenceDisplay sequenceDisplay;
        [SerializeField] private FishingInputHandler  inputHandler;

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Raised when the player successfully completes the sequence.</summary>
        public event Action OnFishCaught;

        /// <summary>Raised when the player inputs a wrong arrow or runs out of time.</summary>
        public event Action OnFishEscaped;

        // ── State ────────────────────────────────────────────────────────────────

        private List<ArrowDirection> _sequence;
        private int  _currentIndex;
        private bool _isActive;
        private bool _roundFinished;
        private bool _roundSucceeded;

        private CancellationTokenSource _cts;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Starts a fishing attempt. Safe to await from any MonoBehaviour.
        /// Subsequent calls are ignored while a round is already active.
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
                // Round was cancelled externally (e.g. player moved away).
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

        /// <summary>Cancels an active fishing round without raising any result event.</summary>
        public void CancelFishing()
        {
            _cts?.Cancel();
        }

        // ── Private logic ─────────────────────────────────────────────────────────

        private async UniTask RunRound(CancellationToken ct)
        {
            // 1. Generate & display sequence
            _sequence     = GenerateSequence(sequenceLength);
            _currentIndex = 0;
            _roundFinished  = false;
            _roundSucceeded = false;

            sequenceDisplay.Show();
            await sequenceDisplay.ShowSequenceAnimated(_sequence, delayPerArrow, ct);

            // 2. Enable input
            inputHandler.OnArrowPressed += HandleArrowInput;
            inputHandler.SetActive(true);

            // 3. Wait for success / failure / timeout
            bool success = await WaitForRoundResult(ct);

            // 4. Cleanup
            inputHandler.OnArrowPressed -= HandleArrowInput;
            inputHandler.SetActive(false);

            // Brief pause so the player sees the last arrow highlighted
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ct);

            sequenceDisplay.ClearIcons();
            sequenceDisplay.Hide();

            // 5. Fire result
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
                if (elapsed >= inputTimeLimit)
                    return false; // timeout → fish escapes

                await UniTask.Yield(ct);
            }

            return _roundSucceeded;
        }

        private void HandleArrowInput(ArrowDirection direction)
        {
            if (!_isActive || _roundFinished) return;

            bool correct = direction == _sequence[_currentIndex];

            if (correct)
            {
                sequenceDisplay.SetArrowState(_currentIndex, ArrowIconState.Correct);
                _currentIndex++;

                if (_currentIndex >= _sequence.Count)
                {
                    // All arrows matched!
                    _roundSucceeded = true;
                    _roundFinished  = true;
                }
            }
            else
            {
                sequenceDisplay.SetArrowState(_currentIndex, ArrowIconState.Wrong);
                _roundSucceeded = false;
                _roundFinished  = true;
            }
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
