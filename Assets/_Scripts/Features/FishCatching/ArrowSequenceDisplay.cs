using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FishRoguelike.Features.FishCatching
{
    /// <summary>
    /// Manages the on-screen display of the arrow sequence.
    /// Spawns ArrowIcon prefabs inside arrowContainer one by one with a delay.
    /// </summary>
    public class ArrowSequenceDisplay : MonoBehaviour
    {
        [SerializeField] private Transform  arrowContainer;
        [SerializeField] private ArrowIcon  arrowIconPrefab;

        private readonly List<ArrowIcon> _icons = new();

        /// <summary>
        /// Spawns arrows one at a time with <paramref name="delayPerArrow"/> seconds between each.
        /// </summary>
        public async UniTask ShowSequenceAnimated(
            IReadOnlyList<ArrowDirection> sequence,
            float delayPerArrow,
            System.Threading.CancellationToken ct = default)
        {
            ClearIcons();

            foreach (var direction in sequence)
            {
                ct.ThrowIfCancellationRequested();

                var icon = Instantiate(arrowIconPrefab, arrowContainer);
                icon.Setup(direction, ArrowIconState.Pending);
                _icons.Add(icon);

                await UniTask.Delay(
                    System.TimeSpan.FromSeconds(delayPerArrow),
                    cancellationToken: ct);
            }
        }

        /// <summary>Shows all arrows instantly (no animation).</summary>
        public void ShowSequenceInstant(IReadOnlyList<ArrowDirection> sequence)
        {
            ClearIcons();
            foreach (var direction in sequence)
            {
                var icon = Instantiate(arrowIconPrefab, arrowContainer);
                icon.Setup(direction, ArrowIconState.Pending);
                _icons.Add(icon);
            }
        }

        public void SetArrowState(int index, ArrowIconState state)
        {
            if (index >= 0 && index < _icons.Count)
                _icons[index].SetState(state);
        }

        public void ClearIcons()
        {
            foreach (var icon in _icons)
                if (icon != null) Destroy(icon.gameObject);
            _icons.Clear();
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
