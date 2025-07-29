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
            UnityEngine.Debug.LogWarning("SaveTransformerMod: UI prefab 'SaveTransformerUI' not found. Creating fallback UI.");
            CreateFallbackUI();
            return;
        }

        UnityEngine.Debug.Log("SaveTransformerMod: UI prefab loaded successfully");
    }

    private void CreateFallbackUI()
    {
        // Create a basic Canvas-based UI as fallback
        var canvasGO = new GameObject("SaveTransformerCanvas");
        canvasGO.transform.SetParent(transform);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Render on top

        var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // Create the main panel
        var panelGO = new GameObject("MainPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        var panel = panelGO.AddComponent<Image>();
        panel.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 300);
        panelRect.anchoredPosition = Vector2.zero;

        // Add title text
        CreateText("Title", "SaveTransformerMod UI", panelGO.transform, new Vector2(0, 100), 20);

        // Add info texts
        screenSizeText = CreateText("ScreenSize", $"Screen: {Screen.width}x{Screen.height}", panelGO.transform, new Vector2(0, 50), 14);
        timeText = CreateText("Time", $"Time: {Time.time:F2}", panelGO.transform, new Vector2(0, 20), 14);

        // Add close button
        closeButton = CreateButton("CloseButton", "Close", panelGO.transform, new Vector2(0, -100));
        closeButton.onClick.AddListener(() => mod.CloseUI());

        uiInstance = canvasGO;
        uiInstance.SetActive(false);
    }

    private Text CreateText(string name, string content, Transform parent, Vector2 position, int fontSize)
    {
        var textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        var text = textGO.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        var rectTransform = textGO.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300, 30);
        rectTransform.anchoredPosition = position;

        return text;
    }

    private Button CreateButton(string name, string text, Transform parent, Vector2 position)
    {
        var buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        var button = buttonGO.AddComponent<Button>();
        var image = buttonGO.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Add button text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        var buttonText = textGO.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.fontSize = 16;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(120, 30);
        textRect.anchoredPosition = Vector2.zero;

        var buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(120, 40);
        buttonRect.anchoredPosition = position;

        return button;
    }

    private void Update()
    {
        if (mod != null)
        {
            mod.CheckInput();

            // Update UI visibility
            if (uiInstance != null)
            {
                bool shouldBeActive = mod.isUIOpen;
                if (uiInstance.activeSelf != shouldBeActive)
                {
                    uiInstance.SetActive(shouldBeActive);
                }

                // Update dynamic text if UI is open
                if (shouldBeActive)
                {
                    UpdateDynamicContent();
                }
            }
        }
    }

    private void UpdateDynamicContent()
    {
        if (screenSizeText != null)
            screenSizeText.text = $"Screen: {Screen.width}x{Screen.height}";

        if (timeText != null)
            timeText.text = $"Time: {Time.time:F2}";
    }

    private void Start()
    {
        // If we have a prefab, instantiate it when the component starts
        if (uiPrefab != null && uiInstance == null)
        {
            uiInstance = Instantiate(uiPrefab);
            uiInstance.transform.SetParent(transform, false);
            uiInstance.SetActive(false);

            // Find and hook up UI components from your prefab
            HookupPrefabComponents();
        }
    }

    private void HookupPrefabComponents()
    {
        // Find your UI components by name or tag
        // Adjust these names to match your prefab's hierarchy

        var closeBtn = uiInstance.GetComponentInChildren<Button>();
        if (closeBtn != null && closeBtn.name == "CloseButton")
        {
            closeButton = closeBtn;
            closeButton.onClick.AddListener(() => mod.CloseUI());
        }

        // Find text components
        var textComponents = uiInstance.GetComponentsInChildren<Text>();
        foreach (var text in textComponents)
        {
            if (text.name == "ScreenSizeText")
                screenSizeText = text;
            else if (text.name == "TimeText")
                timeText = text;
        }

        UnityEngine.Debug.Log("SaveTransformerMod: UI components hooked up");
    }
}