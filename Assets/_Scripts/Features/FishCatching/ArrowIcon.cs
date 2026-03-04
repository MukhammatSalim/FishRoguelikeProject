using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishRoguelike.Features.FishCatching
{
    /// <summary>
    /// Visual representation of a single arrow in the fishing sequence.
    /// Uses TextMeshPro symbols as fallback; swap arrowText for a sprite if needed.
    /// </summary>
    public class ArrowIcon : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI arrowText;
        [SerializeField] private Image background;

        [Header("Colors")]
        [SerializeField] private Color pendingColor = Color.white;
        [SerializeField] private Color correctColor = Color.green;
        [SerializeField] private Color wrongColor   = Color.red;

        private static readonly string[] Symbols = { "↑", "↓", "←", "→" };

        public void Setup(ArrowDirection direction, ArrowIconState state)
        {
            arrowText.text = Symbols[(int)direction];
            SetState(state);
        }

        public void SetState(ArrowIconState state)
        {
            background.color = state switch
            {
                ArrowIconState.Correct => correctColor,
                ArrowIconState.Wrong   => wrongColor,
                _                      => pendingColor
            };
        }
    }
}
