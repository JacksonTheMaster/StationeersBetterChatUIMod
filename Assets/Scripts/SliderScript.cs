using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SaveTransformer.Mod
{
    public class SliderScript : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_InputField _inputField;

        void Start()
        {
            // Initialize slider and input field listeners
            _slider.onValueChanged.AddListener(OnSliderValueChanged);
            _inputField.onEndEdit.AddListener(OnInputFieldEndEdit);

            // Set initial value to ensure it's even
            float initialValue = Mathf.Floor(_slider.value / 2) * 2; // Round down to nearest even number
            _slider.value = initialValue;
            UpdateSliderValue(initialValue);
        }

        private void OnSliderValueChanged(float value)
        {
            // Round down to nearest even number
            float evenValue = Mathf.Floor(value / 2) * 2;
            _slider.value = evenValue; // Update slider to ensure it reflects even value
            UpdateSliderValue(evenValue);
        }

        private void OnInputFieldEndEdit(string text)
        {
            // Parse input and enforce even number
            if (float.TryParse(text, out float value))
            {
                float evenValue = Mathf.Floor(value / 2) * 2; // Round down to nearest even number
                _slider.value = evenValue; // Update slider
                UpdateSliderValue(evenValue);
            }
            else
            {
                // If input is invalid, reset to current slider value
                UpdateSliderValue(_slider.value);
            }
        }

        private void UpdateSliderValue(float value)
        {
            // Update both the displayed text and input field
            _inputField.text = value.ToString("F0"); // Sync input field
        }
    }
}