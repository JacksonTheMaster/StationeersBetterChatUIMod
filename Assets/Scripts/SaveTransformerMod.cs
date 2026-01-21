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
        PrefabPatch.prefabs = contentHandler.prefabs;
        harmony.PatchAll();
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

    public void CloseUI()
    {
        isUIOpen = false;
        UnityEngine.Debug.Log("SaveTransformerMod UI closed");
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
    }

    private void LoadUIPrefab()
    {
        // Try to load your UI prefab from the mod's assets
        // Replace "SaveTransformerUI" with your actual prefab name
        uiPrefab = contentHandler.prefabs.ReverseFind(p => p.name == "SaveTransformerUI");

        if (uiPrefab == null)
        {
            UnityEngine.Debug.LogWarning("SaveTransformerMod: UI prefab 'SaveTransformerUI' not found.");
            return;
        }

        UnityEngine.Debug.Log("SaveTransformerMod: UI prefab loaded successfully");
    }
}