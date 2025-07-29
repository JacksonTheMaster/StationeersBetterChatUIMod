using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SaveTransformer.Mod
{
    public class WorldTypeSelection : MonoBehaviour
    {
        public string SelectedWorldType;

        void Start()
        {
            var dropdown = GetComponent<TMP_Dropdown>();

            // Set initial selection
            dropdown.value = 0;
            DropdownItemSelected(dropdown);

            // Listen for changes
            dropdown.onValueChanged.AddListener(delegate { DropdownItemSelected(dropdown); });
        }

        void DropdownItemSelected(TMP_Dropdown dropdown)
        {
            int index = dropdown.value;
            SelectedWorldType = dropdown.options[index].text;
            Debug.Log($"Selected WorldType: {SelectedWorldType}");
        }
    }
}
