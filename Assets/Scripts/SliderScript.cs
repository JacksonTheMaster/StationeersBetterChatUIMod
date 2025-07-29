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
        [SerializeField] private TextMeshProUGUI _SliderValue;
        // Start is called before the first frame update
        void Start()
        {
            _slider.onValueChanged.AddListener((v) =>
            {
                _SliderValue.text = v.ToString();
            }
            );
        }
    }
}
