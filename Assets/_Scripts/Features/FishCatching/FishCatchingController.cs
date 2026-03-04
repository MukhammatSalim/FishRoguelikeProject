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
    /// Round flow:
    ///   1. Hook phase (optional): short sequence shown one-by-one, player must match it.
    ///      Fail → OnFishEscaped.
    ///   2. Preview phase: full catching sequence shown all at once, input disabled for
    ///      config.previewDuration seconds so the player can memorise it.
    ///   3. Catch phase: input enabled, timer starts.
    ///      - Wrong press → mistake counted, icon flashes red, input blocked for
    ///        config.wrongInputCooldown seconds, then same arrow must be retried.
    ///      - Mistakes exceed config.maxMistakes → OnFishEscaped.
    ///      - Timer runs out → OnFishEscaped.
    ///      - All arrows correct → OnFishCaught.
    ///
    /// Wire up in the Inspector:
    ///   - config          → FishCatchConfig ScriptableObject
    ///   - sequenceDisplay → ArrowSequenceDisplay on your UI canvas  (optional — null = no UI)
    ///   - inputHandler    → FishingInputHandler on any active GameObject
    /// </summary>
    public class FishCatchingController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [SerializeField] private FishCatchConfig      config;
        [SerializeField] private ArrowSequenceDisplay sequenceDisplay; // optional
        [SerializeField] private FishingInputHandler  inputHandler;
        [SerializeField] private FishingTimerBar      timerBar;        // optional

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired when the hook-phase sequence is generated and about to be shown.</summary>
        public event Action<IReadOnlyList<ArrowDirection>> OnHookPhaseStarted;

        /// <summary>Fired when the catch-phase sequence is generated and about to be shown.</summary>
        public event Action<IReadOnlyList<ArrowDirection>> OnCatchPhaseStarted;

        /// <summary>All catch arrows matched correctly.</summary>
        public event Action OnFishCaught;

        /// <summary>Player ran out of mistakes or time (in either phase).</summary>
        public event Action OnFishEscaped;

        /// <summary>
        /// Fired on every wrong key press.
        /// Parameters: (mistakesSoFar, mistakesAllowed).
        /// </summary>
        public event Action<int, int> OnMistake;

        // ── Shared per-phase state ────────────────────────────────────────────────

        private List<ArrowDirection> _activeSequence;
        private int  _activeIndex;
        private int  _activeMistakes;
        private int  _activeMistakeLimit;
        private bool _inputBlocked;
        private bool _phaseFinished;
        private bool _phaseSucceeded;

        // ── Round state ───────────────────────────────────────────────────────────

        private bool _isActive;
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
                sequenceDisplay?.ClearIcons();
                sequenceDisplay?.Hide();
                timerBar?.Hide();
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

        // ── Round logic ───────────────────────────────────────────────────────────

        private async UniTask RunRound(CancellationToken ct)
        {
            // ── Phase 1: Hook ─────────────────────────────────────────────────────
            if (config.hookSequenceLength > 0)
            {
                var hookSeq = GenerateSequence(config.hookSequenceLength);
                OnHookPhaseStarted?.Invoke(hookSeq);

                sequenceDisplay?.Show();
                timerBar?.Show();

                // Bar fills while the hook sequence animates in.
                float hookShowDuration = hookSeq.Count * config.hookDelayPerArrow;
                timerBar?.AnimateFill(0f, 1f, hookShowDuration, ct).Forget();

                if (sequenceDisplay != null)
                    await sequenceDisplay.ShowSequenceAnimated(hookSeq, config.hookDelayPerArrow, ct);
                else if (hookShowDuration > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(hookShowDuration), cancellationToken: ct);

                // Bar drains while the player inputs the hook sequence.
                bool hooked = await RunInputPhase(
                    hookSeq,
                    config.hookMaxMistakes,
                    config.hookInputTimeLimit,
                    ct);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(config.resultDisplayDuration),
                    cancellationToken: ct);

                sequenceDisplay?.ClearIcons();
                sequenceDisplay?.Hide();
                timerBar?.Hide();

                if (!hooked)
                {
                    OnFishEscaped?.Invoke();
                    return;
                }
            }

            // ── Phase 2: Show catch sequence (all at once) ────────────────────────
            var sequence = GenerateSequence(config.sequenceLength);
            OnCatchPhaseStarted?.Invoke(sequence);

            sequenceDisplay?.Show();
            sequenceDisplay?.ShowSequenceInstant(sequence);
            timerBar?.Show();

            // ── Phase 3: Preview — bar fills as player memorises ──────────────────
            timerBar?.AnimateFill(0f, 1f, config.previewDuration, ct).Forget();

            if (config.previewDuration > 0f)
                await UniTask.Delay(
                    TimeSpan.FromSeconds(config.previewDuration),
                    cancellationToken: ct);

            // ── Phase 4: Catch — bar drains with the input timer ──────────────────
            bool caught = await RunInputPhase(
                sequence,
                config.maxMistakes,
                config.inputTimeLimit,
                ct);

            await UniTask.Delay(
                TimeSpan.FromSeconds(config.resultDisplayDuration),
                cancellationToken: ct);

            sequenceDisplay?.ClearIcons();
            sequenceDisplay?.Hide();
            timerBar?.Hide();

            if (caught)
                OnFishCaught?.Invoke();
            else
                OnFishEscaped?.Invoke();
        }

        // ── Generic input phase ───────────────────────────────────────────────────

        private async UniTask<bool> RunInputPhase(
            List<ArrowDirection> sequence,
            int maxMistakes,
            float timeLimit,
            CancellationToken ct)
        {
            _activeSequence     = sequence;
            _activeIndex        = 0;
            _activeMistakes     = 0;
            _activeMistakeLimit = maxMistakes;
            _inputBlocked       = false;
            _phaseFinished      = false;
            _phaseSucceeded     = false;

            inputHandler.OnArrowPressed += HandleArrowInput;
            inputHandler.SetActive(true);

            // Bar drains from full to empty as the timer counts down.
            using var barCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timerBar?.AnimateFill(1f, 0f, timeLimit, barCts.Token).Forget();

            try
            {
                float elapsed = 0f;
                while (!_phaseFinished)
                {
                    ct.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    if (elapsed >= timeLimit)
                        break; // timeout; _phaseSucceeded stays false
                    await UniTask.Yield(ct);
                }
            }
            finally
            {
                barCts.Cancel(); // stop bar animation regardless of how the phase ended
                inputHandler.OnArrowPressed -= HandleArrowInput;
                inputHandler.SetActive(false);
            }

            return _phaseSucceeded;
        }

        // ── Input handling ────────────────────────────────────────────────────────

        private void HandleArrowInput(ArrowDirection direction)
        {
            if (_phaseFinished || _inputBlocked) return;

            if (direction == _activeSequence[_activeIndex])
            {
                sequenceDisplay?.SetArrowState(_activeIndex, ArrowIconState.Correct);
                _activeIndex++;

                if (_activeIndex >= _activeSequence.Count)
                {
                    _phaseSucceeded = true;
                    _phaseFinished  = true;
                }
            }
            else
            {
                _activeMistakes++;
                OnMistake?.Invoke(_activeMistakes, _activeMistakeLimit);

                if (_activeMistakes > _activeMistakeLimit)
                {
                    // Too many mistakes — fail the phase immediately.
                    sequenceDisplay?.SetArrowState(_activeIndex, ArrowIconState.Wrong);
                    _phaseSucceeded = false;
                    _phaseFinished  = true;
                }
                else
                {
                    // Flash red, block input for cooldown, then restore Pending.
                    ApplyWrongInputPenalty(_activeIndex, _cts.Token).Forget();
                }
            }
        }

        private async UniTaskVoid ApplyWrongInputPenalty(int index, CancellationToken ct)
        {
            _inputBlocked = true;
            sequenceDisplay?.SetArrowState(index, ArrowIconState.Wrong);

            // Show red for flash duration.
            await UniTask.Delay(
                TimeSpan.FromSeconds(config.wrongFlashDuration),
                cancellationToken: ct);

            if (!_phaseFinished)
                sequenceDisplay?.SetArrowState(index, ArrowIconState.Pending);

            // Block input for the rest of the cooldown.
            float remaining = config.wrongInputCooldown - config.wrongFlashDuration;
            if (remaining > 0f)
                await UniTask.Delay(
                    TimeSpan.FromSeconds(remaining),
                    cancellationToken: ct);

            _inputBlocked = false;
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
