using MoonSharp.VsCodeDebugger.SDK;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro; // Required for TextMeshProUGUI
using UnityEngine;
using UnityEngine.Networking;


namespace SaveTransformer.Mod
{
    public class Interaction : MonoBehaviour
    {
        public SavegameSelection savegameSelection; // save folder name, eg Europa3 in SelectedSavegame, and path to save folder, eg C:\Users\JXSN\Documents\My Games\Stationeers\saves\Europa3 in SelectedSavegameFolderPath
        public WorldTypeSelection worldTypeSelection; // world type to migrate to, eg Lunar Mare
        public TransformPresetSelection transformPresetSelection; // Preset to simplify the "advanced options" and generally selected types to remove etc on the web ui. eg Essential, Complex, Experimental
        public TextMeshProUGUI XSliderValue; // STRING value of the XOffset, eg "1040"
        public TextMeshProUGUI YSliderValue; // STRING value of the YOffset eg "100"
        public TextMeshProUGUI ZSliderValue; // STRING value of the ZOffset eg "-1040"
        [SerializeField] private TextMeshProUGUI logOutput; // Placeholder for future UI logging
        [SerializeField] private CanvasGroup[] beforeTransformState;
        [SerializeField] private CanvasGroup[] whileTransformState;
        [SerializeField] private CanvasGroup[] afterTransformState;
        [SerializeField] private CanvasGroup[] helpState;

        // Preset configurations mirroring web UI defaults and placeholders for Complex/Experimental
        private readonly Dictionary<string, (string[] elements, string[] types, string[] prefabs, bool[] logging)> _presets = new()
        {
            {
                "Essential",
                (
                    // Default elements from web UI, only checked ones
                    new[] { "Rooms", "PipeNetworks", "CableNetworks", "ChuteNetworks", "LandingPadNetworks", "RocketNetworks", "RoboticArmNetworks", "AllThings", "Atmospheres", "Rockets" },
                    // Default types from web UI, only checked ones
                    new[] { "DynamicGasCanisterSaveData", "MagazineSaveData", "LaunchPadSaveData", "ModularRocketSaveData", "AutonomousRocketSaveData" },
                    // Default prefabs from web UI
                    new[] { "WeaponRifleEnergy", "WeaponPistolEnergy" },
                    // Logging options (disabled for Essential to keep it simple)
                    new[] { false, false, false, false }
                )
            },
            {
                "Complex",
                (
                    // Placeholder: Same as Essential plus some additional elements
                    new[] { "Rooms", "PipeNetworks", "CableNetworks", "ChuteNetworks", "LandingPadNetworks", "RocketNetworks", "RoboticArmNetworks", "AllThings", "Atmospheres", "Rockets", "DynamicThings" },
                    // Placeholder: Same as Essential plus additional types
                    new[] { "DynamicGasCanisterSaveData", "MagazineSaveData", "LaunchPadSaveData", "ModularRocketSaveData", "AutonomousRocketSaveData", "DynamicCrateSaveData" },
                    // Placeholder: Same as Essential plus additional prefabs
                    new[] { "WeaponRifleEnergy", "WeaponPistolEnergy", "DeprecatedTool" },
                    // Logging options (some enabled for debugging)
                    new[] { true, true, false, false }
                )
            },
            {
                "Experimental",
                (
                    // Placeholder: All available elements for maximum compatibility
                    new[] { "Rooms", "PipeNetworks", "CableNetworks", "ChuteNetworks", "LandingPadNetworks", "RocketNetworks", "RoboticArmNetworks", "AllThings", "Atmospheres", "Rockets", "DynamicThings", "LegacySystems" },
                    // Placeholder: Minimal types to remove for experimental compatibility
                    new[] { "DynamicGasCanisterSaveData", "MagazineSaveData" },
                    // Placeholder: Minimal prefabs to remove
                    new[] { "WeaponRifleEnergy" },
                    // Logging options (all enabled for maximum debugging)
                    new[] { true, true, true, true }
                )
            }
        };

        public void TriggerTransformAction()
        {
            if (savegameSelection != null)
            {
                float xOffset = float.Parse(XSliderValue.text);
                float yOffset = float.Parse(YSliderValue.text);
                float zOffset = float.Parse(ZSliderValue.text);

                Debug.Log("Transformation request triggered");
                Debug.Log("SavegameSelection: " + savegameSelection.SelectedSavegame);
                Debug.Log("SavegameSelection Folder Path: " + savegameSelection.SelectedSavegameFolderPath);
                Debug.Log("WorldTypeSelection: " + worldTypeSelection.SelectedWorldType);
                Debug.Log("TransformPresetSelection: " + transformPresetSelection.SelectedTransformPreset);
                Debug.Log($"Offsets: X={xOffset}, Y={yOffset}, Z={zOffset}");
                logOutput.fontSize = 18;
                logOutput.text = "Transformation Started";
                StartCoroutine(SendAPIRequestAndAwaitResponse());

                foreach (CanvasGroup group in beforeTransformState)
                {
                    if (group != null)
                    {
                        group.alpha = 0f; // Make invisible
                        group.interactable = false; // Disable interactions
                        group.blocksRaycasts = false; // Disable raycasts (e.g., clicks)
                    }
                }

                foreach (CanvasGroup group in whileTransformState)
                {
                    if (group != null)
                    {
                        group.alpha = 1f; // Make visible
                        group.interactable = true; // Enable interactions
                        group.blocksRaycasts = true; // Enable raycasts
                    }
                }


            }
        }

        public void TriggerBackAction()
        {
            foreach (CanvasGroup group in afterTransformState)
            {
                if (group != null)
                {
                    group.alpha = 0f; // Make invisible
                    group.interactable = false; // Disable interactions
                    group.blocksRaycasts = false; // Disable raycasts (e.g., clicks)
                }
            }

            foreach (CanvasGroup group in helpState)
            {
                if (group != null)
                {
                    group.alpha = 0f; // Make invisible
                    group.interactable = false; // Disable interactions
                    group.blocksRaycasts = false; // Disable raycasts (e.g., clicks)
                }
            }

            foreach (CanvasGroup group in beforeTransformState)
            {
                if (group != null)
                {
                    group.alpha = 1f; // Make visible
                    group.interactable = true; // Enable interactions
                    group.blocksRaycasts = true; // Enable raycasts
                }
            }
        }
        public void TriggerHelpAction()
        {
            foreach (CanvasGroup group in beforeTransformState)
            {
                if (group != null)
                {
                    group.alpha = 0f; // Make invisible
                    group.interactable = false; // Disable interactions
                    group.blocksRaycasts = false; // Disable raycasts (e.g., clicks)
                }
            }

            foreach (CanvasGroup group in helpState)
            {
                if (group != null)
                {
                    group.alpha = 1f; // Make visible
                    group.interactable = true; // Enable interactions
                    group.blocksRaycasts = true; // Enable raycasts
                }
            }
        }
        public void TriggerDiscordLinkAction()
        {
            Application.OpenURL("https://discord.gg/8n3vN92MyJ");
        }

        public void TriggerFaqLinkAction()
        {
            Application.OpenURL("https://sst.jxsn.dev");
        }
        public string FindSaveFile()
        {
            if (string.IsNullOrEmpty(savegameSelection?.SelectedSavegameFolderPath))
            {
                Debug.LogError("No savegame folder path selected.");
                return null;
            }

            string folderPath = savegameSelection.SelectedSavegameFolderPath;
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError($"Savegame folder does not exist: {folderPath}");
                return null;
            }

            // Check for .save or world.xml at top level, ignoring subfolders
            string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".save", System.StringComparison.OrdinalIgnoreCase) || Path.GetFileName(f).Equals("world.xml", System.StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (files.Length == 0)
            {
                Debug.LogError($"No .save or world.xml file found in {folderPath}");
                return null;
            }

            if (files.Length > 1)
            {
                Debug.LogWarning($"Multiple save files found in {folderPath}. Using the first one: {files[0]}");
            }

            return files[0];
        }

        public IEnumerator SendAPIRequestAndAwaitResponse()
        {
            string filePath = FindSaveFile();
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("Cannot send API request: No valid save file found.");
                yield break;
            }

            string preset = transformPresetSelection.SelectedTransformPreset;
            if (!_presets.ContainsKey(preset))
            {
                Debug.LogError($"Invalid preset selected: {preset}. Available presets: {string.Join(", ", _presets.Keys)}");
                yield break;
            }

            var (elements, types, prefabs, logging) = _presets[preset];
            bool isSaveFile = filePath.EndsWith(".save", System.StringComparison.OrdinalIgnoreCase);
            List<string> typesToRemove = types.ToList();
            if (isSaveFile)
            {
                typesToRemove.Add("HumanSaveData");
                Debug.Log("Detected .save file. Added HumanSaveData to types to remove.");
            }

            // Prepare form data
            WWWForm form = new WWWForm();
            form.AddField("worldType", worldTypeSelection.SelectedWorldType);
            form.AddField("liftHeight", YSliderValue.text);
            form.AddField("xOffset", XSliderValue.text);
            form.AddField("zOffset", ZSliderValue.text);
            form.AddField("logAdjustments", logging[0] ? "true" : "false");
            form.AddField("logTrackedRefIds", logging[1] ? "true" : "false");
            form.AddField("logThingSaveDataRemovals", logging[2] ? "true" : "false");
            form.AddField("logCascades", logging[3] ? "true" : "false");
            form.AddField("elementsToMigrate", string.Join(", ", elements));
            form.AddField("typesToRemove", string.Join(", ", typesToRemove));
            form.AddField("prefabNamesToRemove", string.Join(", ", prefabs));

            byte[] fileBytes = File.ReadAllBytes(filePath);
            form.AddBinaryData("oldSaveFile", fileBytes, Path.GetFileName(filePath));

            using (UnityWebRequest request = UnityWebRequest.Post("http://localhost:8080/transform", form))
            {
                request.SetRequestHeader("User-Agent", "SST-Unity/1.0");
                Debug.Log("Sending transformation request to http://localhost:8080/transform");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"API request failed: {request.error}");
                    // Placeholder for future UI error display
                    yield break;
                }

                string responseText = request.downloadHandler.text;

                // Parse the response outside of try-catch to avoid yield issues
                TransformResponse response = null;
                bool parseSuccess = false;

                if (!string.IsNullOrEmpty(responseText))
                {
                    try
                    {
                        response = JsonUtility.FromJson<TransformResponse>(responseText);
                        parseSuccess = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error parsing API response: {e.Message}");
                        // Placeholder for future UI error display
                        yield break;
                    }
                }

                if (!parseSuccess || response == null)
                {
                    Debug.LogError("Failed to parse API response");
                    yield break;
                }

                if (!response.success)
                {
                    Debug.LogError($"Transformation failed: {response.message}");
                    // Placeholder for future UI error display
                    yield break;
                }

                Debug.Log("Transformation successful. Logs:\n" + response.logs);

                // Download the transformed file
                using (UnityWebRequest downloadRequest = UnityWebRequest.Get("http://localhost:8080/" + response.downloadPath))
                {
                    downloadRequest.SetRequestHeader("User-Agent", "SST-Unity/1.0");
                    yield return downloadRequest.SendWebRequest();

                    if (downloadRequest.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Failed to download transformed file: {downloadRequest.error}");
                        yield break;
                    }

                    // Handle file saving outside of try-catch to avoid yield issues
                    bool saveSuccess = false;
                    string savePath = "";

                    try
                    {
                        // Determine save location
                        string parentDir = Directory.GetParent(savegameSelection.SelectedSavegameFolderPath).FullName;
                        string baseFolderName = $"{savegameSelection.SelectedSavegame}migrated";
                        string saveFolder = Path.Combine(parentDir, baseFolderName);
                        int counter = 1;
                        while (Directory.Exists(saveFolder))
                        {
                            saveFolder = Path.Combine(parentDir, $"{baseFolderName}{counter}");
                            counter++;
                        }

                        Directory.CreateDirectory(saveFolder);
                        savePath = Path.Combine(saveFolder, "world_migrated.SAVE");
                        File.WriteAllBytes(savePath, downloadRequest.downloadHandler.data);
                        saveSuccess = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error saving transformed file: {e.Message}");
                        // Placeholder for future UI error display
                    }

                    if (saveSuccess)
                    {
                        foreach (CanvasGroup group in whileTransformState)
                        {
                            if (group != null)
                            {
                                group.alpha = 0f; // Make invisible
                                group.interactable = false; // Disable interactions
                                group.blocksRaycasts = false; // Disable raycasts (e.g., clicks)
                            }
                        }

                        foreach (CanvasGroup group in afterTransformState)
                        {
                            if (group != null)
                            {
                                group.alpha = 1f; // Make visible
                                group.interactable = true; // Enable interactions
                                group.blocksRaycasts = true; // Enable raycasts
                            }
                        }

                        Debug.Log($"Transformed file saved to: {savePath}");
                        logOutput.fontSize = 8;

                        logOutput.text = "Success!\n"+response.logs;
                    }
                }
            }
        }

        [System.Serializable]
        private class TransformResponse
        {
            public bool success;
            public string message;
            public string downloadPath;
            public string logs;
        }
    }
}