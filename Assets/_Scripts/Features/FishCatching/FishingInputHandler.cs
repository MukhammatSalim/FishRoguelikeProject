using System;
using UnityEngine;

namespace FishRoguelike.Features.FishCatching
{
    /// <summary>
    /// Listens for arrow key input and fires events during the fishing minigame.
    /// Attach to any active GameObject. Enable/disable via SetActive().
    /// </summary>
    public class FishingInputHandler : MonoBehaviour
    {
        public event Action<ArrowDirection> OnArrowPressed;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))    OnArrowPressed?.Invoke(ArrowDirection.Up);
            if (Input.GetKeyDown(KeyCode.DownArrow))  OnArrowPressed?.Invoke(ArrowDirection.Down);
            if (Input.GetKeyDown(KeyCode.LeftArrow))  OnArrowPressed?.Invoke(ArrowDirection.Left);
            if (Input.GetKeyDown(KeyCode.RightArrow)) OnArrowPressed?.Invoke(ArrowDirection.Right);
        }

        public void SetActive(bool active) => enabled = active;
    }
}
