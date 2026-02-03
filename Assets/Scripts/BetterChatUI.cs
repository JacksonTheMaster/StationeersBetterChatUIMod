using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using BepInEx.Configuration;
using HarmonyLib;
using StationeersMods.Interface;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[StationeersMod("BetterChatUI", "BetterChatUI", "1.0.2")]  // bump version for tracking
public class BetterChatUI : ModBehaviour
{
    private ConfigEntry<string> chatStyleConfig;
    private ConfigEntry<float> fadeDurationConfig;

    private ContentHandler contentHandler;
    public static ChatUIController Instance { get; private set; }  // ← static singleton reference

    public override void OnLoaded(ContentHandler contentHandler)
    {
        this.contentHandler = contentHandler;

        chatStyleConfig = Config.Bind(
            "General",
            "ChatStyle",
            "TopLeftNewestTop",
            new ConfigDescription(
                "Chat prefab/style to use. Acceptable values: TopLeftNewestTop, TopLeftNewestBottom, TopRightNewestTop, TopRightNewestBottom, TopLeftNewestTopClearBackground",
                new AcceptableValueList<string>("TopLeftNewestTop", "TopLeftNewestBottom", "TopRightNewestTop", "TopRightNewestBottom", "TopLeftNewestTopClearBackground")
            )
        );

        fadeDurationConfig = Config.Bind(
            "General",
            "MessageDuration",
            10f,
            new ConfigDescription("Message visible time before fade (seconds)", new AcceptableValueRange<float>(3f, 30f))
        );

        Debug.Log($"BetterChatUI v1.0.2 loaded | Style: {chatStyleConfig.Value} | Duration: {fadeDurationConfig.Value}s");

        // Set the static prefabs for the patch
        ChatUI.Mod.PrefabPatch.prefabs = contentHandler.prefabs;

        Harmony harmony = new Harmony("BetterChatUI");
        harmony.PatchAll();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance == null)
        {
            CreateChatUI();
        }
    }

    private void CreateChatUI()
    {
        if (Instance != null) return;  // already created

        var gameObject = new GameObject("BetterChatUIController");
        Instance = gameObject.AddComponent<ChatUIController>();
        Instance.Initialize(this, contentHandler, chatStyleConfig.Value, fadeDurationConfig.Value);
        Object.DontDestroyOnLoad(gameObject);

        Debug.Log("BetterChatUI controller created successfully");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

// ────────────────────────────────────────────────────────────────────────────────
// Patch: Catch chat messages safely
// ────────────────────────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(ChatMessage), nameof(ChatMessage.PrintToConsole))]
public static class ChatMessagePrintPatch
{
    [HarmonyPostfix]
    static void Postfix(ChatMessage __instance)
    {
        if (BetterChatUI.Instance == null)
        {
            return;
        }

        string formatted = $"<color=#FFAA00>{__instance.DisplayName}</color>: {__instance.ChatText}";
        BetterChatUI.Instance.AddMessage(formatted);

        // Debug.Log($"[BetterChatUI] Added: {formatted}");
    }
}

// ────────────────────────────────────────────────────────────────────────────────
// UI Controller
// ────────────────────────────────────────────────────────────────────────────────
public class ChatUIController : MonoBehaviour
{
    private ContentHandler contentHandler;
    private string currentPrefabName;
    private float fadeDuration;

    private GameObject uiInstance;
    private TextMeshProUGUI[] messageTexts = new TextMeshProUGUI[5];
    private CanvasGroup[] panelGroups = new CanvasGroup[5];
    private AudioSource notificationSound;

    private Coroutine[] fadeCoroutines = new Coroutine[5];

    public void Initialize(BetterChatUI mod, ContentHandler content, string prefabName, float fadeTime)
    {
        contentHandler = content;
        currentPrefabName = prefabName;
        fadeDuration = fadeTime;

        LoadAndCreateUI();
    }

    private void LoadAndCreateUI()
    {
        if (contentHandler?.prefabs == null)
        {
            Debug.LogError("BetterChatUI: contentHandler or prefabs list is null!");
            return;
        }

        // Safer prefab search: use FirstOrDefault + fallback chain
        GameObject uiPrefab = contentHandler.prefabs
            .FirstOrDefault(p => p != null && p.name == currentPrefabName);

        if (uiPrefab == null)
        {
            Debug.LogWarning($"BetterChatUI: Prefab '{currentPrefabName}' not found. Trying fallback...");
            uiPrefab = contentHandler.prefabs
                .FirstOrDefault(p => p != null && p.name == "TopLeftNewestTop");
        }

        if (uiPrefab == null)
        {
            Debug.LogError("BetterChatUI: No valid chat prefab found — UI disabled.");
            return;
        }

        uiInstance = Object.Instantiate(uiPrefab, transform);
        if (uiInstance == null)
        {
            Debug.LogError("BetterChatUI: Instantiate returned null!");
            return;
        }
        uiInstance.SetActive(true);

        // Sound (optional)
        var soundTransform = uiInstance.transform.Find("Sound");
        notificationSound = soundTransform?.GetComponent<AudioSource>();
        if (notificationSound == null)
        {
            Debug.LogWarning("BetterChatUI: No 'Sound' AudioSource found in prefab.");
        }

        // Canvas & panels
        var canvasTransform = uiInstance.transform.Find("Canvas");
        if (canvasTransform == null)
        {
            Debug.LogError("BetterChatUI: 'Canvas' not found in instantiated prefab!");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            int panelNum = i + 1;
            var panel = canvasTransform.Find($"Panel{panelNum}");
            if (panel == null)
            {
                Debug.LogWarning($"Panel{panelNum} missing — skipping.");
                continue;
            }

            panelGroups[i] = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();

            var textObj = panel.Find($"Message{panelNum}");
            messageTexts[i] = textObj?.GetComponent<TextMeshProUGUI>();
            if (messageTexts[i] == null)
            {
                Debug.LogWarning($"Message{panelNum} TextMeshProUGUI missing.");
            }
            else
            {
                messageTexts[i].text = "";
                panel.gameObject.SetActive(false);
            }
        }

        Debug.Log("BetterChatUI: UI setup complete.");
    }

    public void AddMessage(string message)
    {
        if (messageTexts[0] == null) return;  // UI not ready

        // Stop existing fades safely
        for (int i = 0; i < 5; i++)
        {
            if (fadeCoroutines[i] != null)
            {
                StopCoroutine(fadeCoroutines[i]);
                fadeCoroutines[i] = null;
            }
        }

        // Shift messages (with null guard)
        for (int i = 4; i > 0; i--)
        {
            if (messageTexts[i] != null && messageTexts[i - 1] != null)
            {
                messageTexts[i].text = messageTexts[i - 1].text;
            }
        }

        // New message at top
        if (messageTexts[0] != null)
        {
            messageTexts[0].text = message;
        }

        // Sound
        if (notificationSound != null && notificationSound.clip != null)
        {
            notificationSound.Play();
        }

        UpdateDisplayWithFreshFades();
    }

    private void UpdateDisplayWithFreshFades()
    {
        for (int i = 0; i < 5; i++)
        {
            if (messageTexts[i] == null || panelGroups[i] == null) continue;

            string msg = messageTexts[i].text;

            if (!string.IsNullOrEmpty(msg))
            {
                panelGroups[i].alpha = 1f;
                panelGroups[i].gameObject.SetActive(true);
                fadeCoroutines[i] = StartCoroutine(FadePanel(i, fadeDuration));
            }
            else
            {
                panelGroups[i].gameObject.SetActive(false);
                messageTexts[i].text = "";
            }
        }
    }

    private System.Collections.IEnumerator FadePanel(int panelIndex, float delay)
    {
        yield return new WaitForSeconds(delay);

        var group = panelGroups[panelIndex];
        if (group == null) yield break;

        float fadeTime = 1f;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            group.alpha = 1f - (timer / fadeTime);
            yield return null;
        }

        group.gameObject.SetActive(false);
        if (messageTexts[panelIndex] != null)
            messageTexts[panelIndex].text = "";

        fadeCoroutines[panelIndex] = null;
    }
}