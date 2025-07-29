using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

namespace SaveTransformer.Mod
{
    public class SavegameSelection : MonoBehaviour
    {
        public string SelectedSavegame;      // Folder name only
        public string SelectedSavegameFolderPath;  // Full path to folder

        void Start()
        {
            var dropdown = GetComponent<TMP_Dropdown>();
            dropdown.ClearOptions();

            // Path to Stationeers saves folder
            string saveRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games", "Stationeers", "saves");

            List<string> validSaveFolders = new();

            if (Directory.Exists(saveRoot))
            {
                string[] subfolders = Directory.GetDirectories(saveRoot);

                foreach (var folder in subfolders)
                {
                    try
                    {
                        string[] files = Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly);

                        bool hasWorldXml = Array.Exists(files, f => Path.GetFileName(f).Equals("world.xml", StringComparison.OrdinalIgnoreCase));
                        bool hasSaveFile = Array.Exists(files, f => f.EndsWith(".save", StringComparison.OrdinalIgnoreCase));

                        if (hasWorldXml || hasSaveFile)
                        {
                            validSaveFolders.Add(Path.GetFileName(folder));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to scan folder '{folder}': {e.Message}");
                    }
                }

                // Sort folder names alphabetically
                validSaveFolders.Sort(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                Debug.LogWarning($"Save directory not found: {saveRoot}");
            }

            if (validSaveFolders.Count > 0)
            {
                dropdown.AddOptions(validSaveFolders);
                dropdown.value = 0;
                DropdownItemSelected(dropdown);
            }
            else
            {
                dropdown.AddOptions(new List<string> { "(No saves found)" });
                dropdown.interactable = false;
            }

            dropdown.onValueChanged.AddListener(delegate { DropdownItemSelected(dropdown); });
        }

        void DropdownItemSelected(TMP_Dropdown dropdown)
        {
            int index = dropdown.value;
            SelectedSavegame = dropdown.options[index].text;

            // Build full path to selected save folder
            string saveRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games", "Stationeers", "saves");

            SelectedSavegameFolderPath = Path.Combine(saveRoot, SelectedSavegame);

            Debug.Log($"Selected Savegame: {SelectedSavegame} at {SelectedSavegameFolderPath}");
        }
    }
}
