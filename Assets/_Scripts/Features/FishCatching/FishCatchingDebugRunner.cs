using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FishRoguelike.Features.FishCatching
{
    /// <summary>
    /// Lets you test the full fish-catching flow without any UI.
    /// All state is printed to the Unity Console.
    ///
    /// Minimal scene setup (no canvas needed):
    ///   1. Create an empty GameObject.
    ///   2. Add FishCatchingController, FishingInputHandler, and this component.
    ///   3. Assign the same FishCatchConfig to both this and the controller.
    ///   4. Leave sequenceDisplay unassigned on the controller (null = no UI).
    ///   5. Press the startKey at runtime, or right-click → "Debug: Start Fishing".
    /// </summary>
    [RequireComponent(typeof(FishCatchingController))]
    [RequireComponent(typeof(FishingInputHandler))]
    public class FishCatchingDebugRunner : MonoBehaviour
    {
        [Header("Debug Settings")]
        [Tooltip("Key to start a fishing round at runtime.")]
        [SerializeField] private KeyCode startKey = KeyCode.F5;

        private FishCatchingController _controller;

        // ── Unity ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _controller = GetComponent<FishCatchingController>();

            _controller.OnHookPhaseStarted  += LogHookPhase;
            _controller.OnCatchPhaseStarted += LogCatchPhase;
            _controller.OnMistake           += LogMistake;
            _controller.OnFishCaught        += LogCaught;
            _controller.OnFishEscaped       += LogEscaped;
        }

        private void Update()
        {
            if (Input.GetKeyDown(startKey))
                StartFishing();
        }

        private void OnDestroy()
        {
            if (_controller == null) return;
            _controller.OnHookPhaseStarted  -= LogHookPhase;
            _controller.OnCatchPhaseStarted -= LogCatchPhase;
            _controller.OnMistake           -= LogMistake;
            _controller.OnFishCaught        -= LogCaught;
            _controller.OnFishEscaped       -= LogEscaped;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts a fishing round. Also available via right-click → "Debug: Start Fishing".
        /// </summary>
        [ContextMenu("Debug: Start Fishing")]
        public void StartFishing()
        {
            Debug.Log("[FishDebug] ══ Starting fishing round ══");
            _controller.StartFishing().Forget();
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void LogHookPhase(IReadOnlyList<ArrowDirection> sequence)
        {
            Debug.Log($"[FishDebug] ── HOOK phase ──  sequence: {Format(sequence)}  " +
                      $"(press arrow keys to match)");
        }

        private void LogCatchPhase(IReadOnlyList<ArrowDirection> sequence)
        {
            Debug.Log($"[FishDebug] ── CATCH phase ──  sequence: {Format(sequence)}  " +
                      $"(memorise, then press arrow keys)");
        }

        private static void LogMistake(int used, int max)
        {
            string bar = MistakeBar(used, max);
            Debug.LogWarning($"[FishDebug] Wrong key!  mistakes: {used}/{max}  {bar}");
        }

        private static void LogCaught()  => Debug.Log("[FishDebug] ✓ FISH CAUGHT!");
        private static void LogEscaped() => Debug.Log("[FishDebug] ✗ Fish escaped.");

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string Format(IReadOnlyList<ArrowDirection> seq) =>
            string.Join(" → ", seq.Select(Arrow));

        private static string Arrow(ArrowDirection d) => d switch
        {
            ArrowDirection.Up    => "↑",
            ArrowDirection.Down  => "↓",
            ArrowDirection.Left  => "←",
            ArrowDirection.Right => "→",
            _                   => "?"
        };

        private static string MistakeBar(int used, int max)
        {
            if (max <= 0) return "";
            int filled = Mathf.Clamp(used, 0, max + 1);
            return "[" + new string('█', filled) + new string('░', max + 1 - filled) + "]";
        }
    }
}
