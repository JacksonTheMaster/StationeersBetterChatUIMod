// Important: Add this using for the InventoryManager namespace
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using HarmonyLib;
using SaveTransformer.Mod;
using StationeersMods.Interface;
using UnityEngine;
using UnityEngine.UI;

[StationeersMod("SaveTransformerMod", "SaveTransformerMod", "0.2.0")]
public class SaveTransformerMod : ModBehaviour
{
    public bool isUIOpen = false;
    private new ContentHandler contentHandler;

    public override void OnLoaded(ContentHandler contentHandler)
    {
        UnityEngine.Debug.Log("SaveTransformerMod says: Hello World!");

        this.contentHandler = contentHandler;

        // Initialize Harmony for patching
        Harmony harmony = new Harmony("SaveTransformerMod");
        harmony.PatchAll();                     // This will now also patch our new patch class
        PrefabPatch.prefabs = contentHandler.prefabs;

        UnityEngine.Debug.Log("SaveTransformerMod Loaded with " + contentHandler.prefabs.Count + " prefab(s)");

        // Add a MonoBehaviour to handle input and UI management
        var gameObject = new GameObject("SaveTransformerModUI");
        gameObject.AddComponent<SaveTransformerUI>().Initialize(this, contentHandler);
        UnityEngine.Object.DontDestroyOnLoad(gameObject);
    }

    public void CheckInput()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
        {
            ToggleUI();
        }
    }

    public void ToggleUI()
    {
        isUIOpen = !isUIOpen;
        UnityEngine.Debug.Log($"SaveTransformerMod UI {(isUIOpen ? "opened" : "closed")}");
    }

    public void OpenUI()
    {
        isUIOpen = true;
        UnityEngine.Debug.Log("SaveTransformerMod UI opened");
    }

    public void CloseUI()
    {
        isUIOpen = false;
        UnityEngine.Debug.Log("SaveTransformerMod UI closed");
        // uiInstance?.SetActive(false);
    }
}

// ────────────────────────────────────────────────────────────────────────────────
// NEW: Patch ChatMessage.PrintToConsole() — this catches ALL messages on ALL clients
// ────────────────────────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(ChatMessage), nameof(ChatMessage.PrintToConsole))]
public static class ChatMessagePrintPatch
{
    [HarmonyPostfix]
    static void Postfix(ChatMessage __instance)
    {
        // Get the mod instance
        var mod = Object.FindObjectOfType<SaveTransformerMod>();
        if (mod != null)
        {
            // Toggle UI when ANY chat message prints to console
            mod.ToggleUI();  // or mod.isUIOpen = true; to only open
            UnityEngine.Debug.Log($"[ChatPatch] Caught message: {__instance.DisplayName}: {__instance.ChatText}");
        }
    }
}

public class SaveTransformerUI : MonoBehaviour
{
    private SaveTransformerMod mod;
    private ContentHandler contentHandler;
    private GameObject uiPrefab;
    private GameObject uiInstance;
    private Canvas canvas;

    // UI Components - assign these in the prefab or find them by name
    private Button closeButton;
    private Text screenSizeText;
    private Text timeText;

    public void Initialize(SaveTransformerMod modInstance, ContentHandler content)
    {
        mod = modInstance;
        contentHandler = content;
        LoadUIPrefab();

        // Optional: Create the actual UI instance right away (or on first toggle)
        if (uiPrefab != null)
        {
            uiInstance = Object.Instantiate(uiPrefab, transform);
            uiInstance.SetActive(false); // start closed
            canvas = uiInstance.GetComponent<Canvas>();
            // Find your buttons/texts here if needed
            // closeButton = uiInstance.transform.Find("CloseButton")?.GetComponent<Button>();
        }
    }

    private void LoadUIPrefab()
    {
        uiPrefab = contentHandler.prefabs.ReverseFind(p => p.name == "SaveTransformerUI");
        if (uiPrefab == null)
        {
            UnityEngine.Debug.LogWarning("SaveTransformerMod: UI prefab 'SaveTransformerUI' not found.");
            return;
        }
        UnityEngine.Debug.Log("SaveTransformerMod: UI prefab loaded successfully");
    }

    // Optional: If you want to handle UI visibility centrally
    private void Update()
    {
        if (uiInstance != null)
        {
            uiInstance.SetActive(mod.isUIOpen);
        }
    }
}