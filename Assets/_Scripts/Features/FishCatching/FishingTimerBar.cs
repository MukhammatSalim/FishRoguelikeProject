using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace FishRoguelike.Features.FishCatching
{
    /// <summary>
    /// Visual timer bar driven by FishCatchingController.
    ///
    /// Inspector setup:
    ///   background — root GameObject of the whole bar (shown/hidden by the controller).
    ///   fillImage  — Image component with ImageType = Filled; the controller sets fillAmount.
    ///
    /// The bar fills 0→1 during "look" phases (hook show, preview) and
    /// drains 1→0 during input phases as the timer counts down.
    /// </summary>
    public class FishingTimerBar : MonoBehaviour
    {
        [SerializeField] private GameObject background;
        [SerializeField] private Image      fillImage;

        // ── Public API ────────────────────────────────────────────────────────────

        public void Show() => background.SetActive(true);
        public void Hide() => background.SetActive(false);

        /// <summary>Sets fillAmount directly (0 = empty, 1 = full).</summary>
        public void SetFill(float t) => fillImage.fillAmount = Mathf.Clamp01(t);

        /// <summary>
        /// Smoothly animates fillAmount from <paramref name="from"/> to <paramref name="to"/>
        /// over <paramref name="duration"/> seconds.
        /// Fire-and-forget: call with .Forget(). Stops silently on cancellation.
        /// </summary>
        public async UniTaskVoid AnimateFill(
            float from, float to, float duration, CancellationToken ct)
        {
            if (duration <= 0f)
            {
                SetFill(to);
                return;
            }

            SetFill(from);
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    ct.ThrowIfCancellationRequested();
                    SetFill(Mathf.Lerp(from, to, elapsed / duration));
                    elapsed += Time.deltaTime;
                    await UniTask.Yield(ct);
                }

                SetFill(to);
            }
            catch (OperationCanceledException) { /* stopped externally — do nothing */ }
        }
    }
}
