using UnityEngine;

namespace FishRoguelike.Features.FishCatching
{
    /// <summary>
    /// All tunable parameters for the fish-catching minigame.
    /// Create via Assets → Create → FishRoguelike → Fish Catch Config.
    /// </summary>
    [CreateAssetMenu(
        menuName = "FishRoguelike/Fish Catch Config",
        fileName = "FishCatchConfig")]
    public class FishCatchConfig : ScriptableObject
    {
        // ── Hook Phase ────────────────────────────────────────────────────────────
        [Header("Hook Phase")]
        [Tooltip("Arrows the player must press to hook the fish before the main round starts. " +
                 "Set to 0 to skip the hook phase.")]
        [Min(0)] public int hookSequenceLength = 2;

        [Tooltip("Delay between each arrow appearing during the hook sequence animation.")]
        [Min(0f)] public float hookDelayPerArrow = 0.35f;

        [Tooltip("Seconds the player has to complete the hook sequence.")]
        [Min(0.5f)] public float hookInputTimeLimit = 4f;

        [Tooltip("Wrong presses allowed during the hook phase before the fish escapes.")]
        [Min(0)] public int hookMaxMistakes = 1;

        // ── Preview ───────────────────────────────────────────────────────────────
        [Header("Preview")]
        [Tooltip("Seconds the full catching sequence is shown before input is enabled. " +
                 "Set to 0 to start immediately after the sequence appears.")]
        [Min(0f)] public float previewDuration = 2f;

        // ── Catch Sequence ────────────────────────────────────────────────────────
        [Header("Catch Sequence")]
        [Tooltip("Number of arrows in the main catching sequence.")]
        [Range(1, 12)] public int sequenceLength = 4;

        [Tooltip("Seconds the player has to complete the full sequence after preview ends.")]
        [Min(0.5f)] public float inputTimeLimit = 5f;

        [Tooltip("Wrong presses allowed during the catch phase. " +
                 "Exceeding this fails the catch.")]
        [Min(0)] public int maxMistakes = 2;

        // ── Wrong Input ───────────────────────────────────────────────────────────
        [Header("Wrong Input")]
        [Tooltip("How long (seconds) the arrow icon stays red after a wrong press.")]
        [Min(0f)] public float wrongFlashDuration = 0.3f;

        [Tooltip("Total seconds input is blocked after a wrong press " +
                 "(must be ≥ wrongFlashDuration to avoid the icon snapping back before the cooldown ends).")]
        [Min(0f)] public float wrongInputCooldown = 0.5f;

        // ── Result Display ────────────────────────────────────────────────────────
        [Header("Result Display")]
        [Tooltip("Seconds to pause after a phase ends so the player sees the final icon state.")]
        [Min(0f)] public float resultDisplayDuration = 0.6f;
    }
}
