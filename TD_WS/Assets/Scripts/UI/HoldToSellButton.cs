using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;

namespace TowerDefense.UI
{
    public class HoldToSellButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float holdDuration = 1.0f;
        public Action OnHoldComplete;

        private bool isPointerDown = false;
        private float holdTimer = 0f;
        private Button button;
        private Image buttonImage;
        private TextMeshProUGUI buttonText;
        private int refundAmount = 0;

        private void Awake()
        {
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
            
            // Force a beautiful red color block state programmatically
            if (button != null)
            {
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = new Color(0.85f, 0.15f, 0.15f, 1f); // Rich Red
                colors.highlightedColor = new Color(1f, 0.25f, 0.25f, 1f); // Bright Red
                colors.pressedColor = new Color(0.65f, 0.1f, 0.1f, 1f); // Dark Red
                colors.selectedColor = new Color(0.85f, 0.15f, 0.15f, 1f); // Rich Red
                button.colors = colors;
            }

            if (buttonImage != null)
            {
                buttonImage.color = new Color(0.85f, 0.15f, 0.15f, 1f); // Red
            }
            if (buttonText != null)
            {
                buttonText.color = Color.white; // White
            }
        }

        public void Setup(int refund, Action onComplete)
        {
            refundAmount = refund;
            OnHoldComplete = onComplete;
            ResetButton();
        }

        private void Update()
        {
            if (isPointerDown)
            {
                holdTimer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(holdTimer / holdDuration);
                
                if (buttonText != null)
                {
                    buttonText.text = $"Holding... {Mathf.RoundToInt(progress * 100)}%";
                }

                if (holdTimer >= holdDuration)
                {
                    isPointerDown = false;
                    ResetButton();
                    OnHoldComplete?.Invoke();
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            isPointerDown = true;
            holdTimer = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CancelHold();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelHold();
        }

        private void CancelHold()
        {
            if (isPointerDown)
            {
                isPointerDown = false;
                ResetButton();
            }
        }

        public void ResetButton()
        {
            isPointerDown = false;
            holdTimer = 0f;
            if (buttonText != null)
            {
                buttonText.text = $"Hold 1 sec to Sell (+{refundAmount}G)";
            }
        }

        private void OnDisable()
        {
            CancelHold();
        }
    }
}