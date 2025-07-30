using Assets.Scripts;
using MoonSharp.VsCodeDebugger.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public TMP_InputField XSliderInput;
        public TMP_InputField YSliderInput;
        public TMP_InputField ZSliderInput; 
        public AudioSource transformSound;
        private string migratedFolderName;
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
                // Validate and parse input values
                if (!float.TryParse(XSliderInput.text, out float xOffset) ||
                    !float.TryParse(YSliderInput.text, out float yOffset) ||
                    !float.TryParse(ZSliderInput.text, out float zOffset))
                {
                    Debug.LogError("Invalid input: Please enter valid numeric values for X, Y, and Z offsets.");
                    logOutput.text = "Error: Invalid numeric input for offsets.";
                    return;
                }

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
                        group.alpha = 0f;
                        group.interactable = false;
                        group.blocksRaycasts = false;
                    }
                }

                foreach (CanvasGroup group in whileTransformState)
                {
                    if (group != null)
                    {
                        group.alpha = 1f;
                        group.interactable = true;
                        group.blocksRaycasts = true;
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

        public void TriggerLoad()
        {
            if (string.IsNullOrEmpty(migratedFolderName))
            {
                Debug.LogError("No migrated folder name available. Please transform a save first.");
                return;
            }
            ExecuteCommand($"load {migratedFolderName}");
        }

        public static void ExecuteCommand(string command)
        {
            try
            {

                var commandLineType = Type.GetType("Util.Commands.CommandLine, Assembly-CSharp");
                if (commandLineType == null)
                {
                    Debug.Log("CommandLine type not found in Assembly-CSharp.");
                    return;
                }

                var processMethod = commandLineType.GetMethod("Process", new[] { typeof(string) });
                if (processMethod == null)
                {
                    Debug.Log("Process(string) method not found in CommandLine.");
                    return;
                }

                string formattedCommand = command.StartsWith("-") ? command : "-" + command;
                processMethod.Invoke(null, new object[] { formattedCommand });
                Debug.Log($"Successfully executed command: {command}");
            }
            catch (TargetInvocationException tex)
            {
                Debug.Log($"Error executing command '{command}': {tex.Message}");
                if (tex.InnerException != null)
                    Debug.Log($"Inner error: {tex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                Debug.Log($"Unexpected error executing command '{command}': {ex.Message}");
            }
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
            bool isSaveFile = filePath.EndsWith(".save", StringComparison.OrdinalIgnoreCase);
            List<string> typesToRemove = types.ToList();
            if (isSaveFile)
            {
                typesToRemove.Add("HumanSaveData");
                Debug.Log("Detected .save file. Added HumanSaveData to types to remove.");
            }

            WWWForm form = new WWWForm();
            form.AddField("worldType", worldTypeSelection.SelectedWorldType);
            form.AddField("liftHeight", YSliderInput.text);
            form.AddField("xOffset", XSliderInput.text);
            form.AddField("zOffset", ZSliderInput.text);
            form.AddField("logAdjustments", logging[0] ? "true" : "false");
            form.AddField("logTrackedRefIds", logging[1] ? "true" : "false");
            form.AddField("logThingSaveDataRemovals", logging[2] ? "true" : "false");
            form.AddField("logCascades", logging[3] ? "true" : "false");
            form.AddField("elementsToMigrate", string.Join(", ", elements));
            form.AddField("typesToRemove", string.Join(", ", typesToRemove));
            form.AddField("prefabNamesToRemove", string.Join(", ", prefabs));

            byte[] fileBytes = File.ReadAllBytes(filePath);
            form.AddBinaryData("oldSaveFile", fileBytes, Path.GetFileName(filePath));

            using (UnityWebRequest request = UnityWebRequest.Post("https://sst.jxsn.dev/transform", form))
            {
                request.SetRequestHeader("User-Agent", "SST-Unity/1.0");
                Debug.Log("Sending transformation request to https://sst.jxsn.dev/transform");

                // Get the GameManager's MenuMusic
                AudioSource menuMusic = Assets.Scripts.Util.Singleton<GameManager>.Instance.MenuMusic;
                if (menuMusic != null) menuMusic.mute = true;
                transformSound.time = 8.4f;
                float startVolume = 1.0f; // Set a proper starting volume (e.g., 1.0 for full)
                transformSound.volume = 0f; // Start at zero for fade-in
                transformSound.Play();

                // Fade in transform sound
                float fadeInTime = 1.0f;
                for (float t = 0; t < fadeInTime; t += Time.deltaTime)
                {
                    transformSound.volume = startVolume * (t / fadeInTime);
                    yield return null;
                }
                transformSound.volume = startVolume; // Ensure full volume after fade-in

                yield return request.SendWebRequest();

                // Fade out transform sound
                float fadeTime = 2.0f; // Duration of fade in seconds
                for (float t = 0; t < fadeTime; t += Time.deltaTime)
                {
                    transformSound.volume = startVolume * (1 - t / fadeTime);
                    yield return null;
                }
                transformSound.Stop();
                transformSound.volume = startVolume; // Reset volume for next play

                // Restore audio
                if (menuMusic != null) menuMusic.mute = false;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"API request failed: {request.error}");
                    yield break;
                }

                string responseText = request.downloadHandler.text;
                TransformResponse response = null;
                bool parseSuccess = false;

                if (!string.IsNullOrEmpty(responseText))
                {
                    try
                    {
                        response = JsonUtility.FromJson<TransformResponse>(responseText);
                        parseSuccess = true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error parsing API response: {e.Message}");
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
                    yield break;
                }

                Debug.Log("Transformation successful. Logs:\n" + response.logs);

                using (UnityWebRequest downloadRequest = UnityWebRequest.Get("https://sst.jxsn.dev/" + response.downloadPath))
                {
                    downloadRequest.SetRequestHeader("User-Agent", "SST-Unity/1.0");
                    yield return downloadRequest.SendWebRequest();

                    if (downloadRequest.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Failed to download transformed file: {downloadRequest.error}");
                        yield break;
                    }

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

                        // Extract just the folder name (e.g., Europa3migrated or Europa3migrated1)
                        migratedFolderName = Path.GetFileName(saveFolder);
                        saveSuccess = true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error saving transformed file: {e.Message}");
                    }

                    if (saveSuccess)
                    {
                        foreach (CanvasGroup group in whileTransformState)
                        {
                            if (group != null)
                            {
                                group.alpha = 0f;
                                group.interactable = false;
                                group.blocksRaycasts = false;
                            }
                        }

                        foreach (CanvasGroup group in afterTransformState)
                        {
                            if (group != null)
                            {
                                group.alpha = 1f;
                                group.interactable = true;
                                group.blocksRaycasts = true;
                            }
                        }

                        Debug.Log($"Transformed file saved to: {savePath}");
                        logOutput.fontSize = 8;
                        logOutput.text = "Success!\n" + response.logs;
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