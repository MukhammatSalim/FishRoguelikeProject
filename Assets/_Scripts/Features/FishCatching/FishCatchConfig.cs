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
        [Header("Sequence")]
        [Tooltip("Number of arrows in the sequence.")]
        [Range(1, 12)]
        public int sequenceLength = 4;

        [Tooltip("Seconds the player has to complete the full sequence after it is shown.")]
        [Min(1f)]
        public float inputTimeLimit = 5f;

        [Tooltip("Delay in seconds between each arrow appearing on screen.")]
        [Min(0f)]
        public float delayPerArrow = 0.4f;

        [Header("Mistakes")]
        [Tooltip("How many wrong key presses are allowed before the catch fails. " +
                 "Set to 0 for zero-tolerance (first mistake fails immediately).")]
        [Min(0)]
        public int maxMistakes = 2;

        [Tooltip("Seconds the wrong-arrow icon stays red before returning to pending.")]
        [Min(0f)]
        public float wrongFlashDuration = 0.3f;

        [Header("Result Display")]
        [Tooltip("Seconds to pause after success/fail so the player sees the final state.")]
        [Min(0f)]
        public float resultDisplayDuration = 0.6f;
    }
}
