using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using HarmonyLib;
using SaveTransformer.Mod;
using StationeersMods.Interface;
using UnityEngine;
using TMPro;

[StationeersMod("BetterChatUI", "BetterChatUI", "0.2.0")]
public class BetterChatUI : ModBehaviour
{
    private new ContentHandler contentHandler;
    private ChatUIController chatUI;

    public override void OnLoaded(ContentHandler contentHandler)
    {
        UnityEngine.Debug.Log("BetterChatUI says: Hello World!");
        this.contentHandler = contentHandler;

        Harmony harmony = new Harmony("BetterChatUI");
        harmony.PatchAll();

        PrefabPatch.prefabs = contentHandler.prefabs;
        UnityEngine.Debug.Log("BetterChatUI Loaded with " + contentHandler.prefabs.Count + " prefab(s)");

        // Create persistent UI manager
        var gameObject = new GameObject("BetterChatUIController");
        chatUI = gameObject.AddComponent<ChatUIController>();
        chatUI.Initialize(this, contentHandler);
        UnityEngine.Object.DontDestroyOnLoad(gameObject);
    }
}

// ────────────────────────────────────────────────────────────────────────────────
// Patch: Catch every chat message
// ────────────────────────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(ChatMessage), nameof(ChatMessage.PrintToConsole))]
public static class ChatMessagePrintPatch
{
    [HarmonyPostfix]
    static void Postfix(ChatMessage __instance)
    {
        var ui = Object.FindObjectOfType<ChatUIController>();
        if (ui != null)
        {
            string formatted = $"<color=#FFAA00>{__instance.DisplayName}</color>: {__instance.ChatText}";
            ui.AddMessage(formatted);
            UnityEngine.Debug.Log($"[ChatPatch] Added: {formatted}");
        }
    }
}

// ────────────────────────────────────────────────────────────────────────────────
// Main UI Controller (manages the 5 panels)
// ────────────────────────────────────────────────────────────────────────────────
public class ChatUIController : MonoBehaviour
{
    private ContentHandler contentHandler; // ← Add this field!

    private GameObject uiInstance;
    private TextMeshProUGUI[] messageTexts = new TextMeshProUGUI[5];
    private CanvasGroup[] panelGroups = new CanvasGroup[5];

    private string[] messageQueue = new string[5]; // Simple ring buffer
    private int currentIndex = 0;

    public void Initialize(BetterChatUI modInstance, ContentHandler content)
    {
        contentHandler = content; // ← Assign it here!
        LoadAndCreateUI();
    }

    private void LoadAndCreateUI()
    {
        if (contentHandler == null)
        {
            Debug.LogError("BetterChatUI: contentHandler is null in ChatUIController!");
            return;
        }

        var uiPrefab = contentHandler.prefabs.ReverseFind(p => p.name == "ChatMessage");
        if (uiPrefab == null)
        {
            Debug.LogError("BetterChatUI: Could not find prefab 'ChatMessage'!");
            return;
        }

        uiInstance = Object.Instantiate(uiPrefab, transform);
        uiInstance.SetActive(true);

        // Find the Canvas first (optional but safer)
        var canvasTransform = uiInstance.transform.Find("Canvas");
        if (canvasTransform == null)
        {
            Debug.LogError("BetterChatUI: Could not find 'Canvas' under root!");
            return;
        }

        // Now find panels under Canvas
        for (int i = 0; i < 5; i++)
        {
            int panelNum = i + 1;
            var panel = canvasTransform.Find($"Panel{panelNum}");
            if (panel == null)
            {
                Debug.LogWarning($"Panel{panelNum} not found under Canvas!");
                continue;
            }

            panelGroups[i] = panel.GetComponent<CanvasGroup>();
            if (panelGroups[i] == null)
            {
                panelGroups[i] = panel.gameObject.AddComponent<CanvasGroup>();
            }

            var text = panel.Find($"Message{panelNum}")?.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                messageTexts[i] = text;
                text.text = "";
                panel.gameObject.SetActive(false); // Start hidden
            }
            else
            {
                Debug.LogWarning($"Message{panelNum} not found under Panel{panelNum}!");
            }
        }
    }

    public void AddMessage(string message)
    {
        // Shift messages up (oldest gets overwritten)
        for (int i = 4; i > 0; i--)
        {
            messageQueue[i] = messageQueue[i - 1];
        }
        messageQueue[0] = message;

        // Update UI
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        for (int i = 0; i < 5; i++)
        {
            int panelIndex = i;
            string msg = messageQueue[panelIndex];

            if (!string.IsNullOrEmpty(msg))
            {
                messageTexts[panelIndex].text = msg;
                panelGroups[panelIndex].alpha = 1f;
                panelGroups[panelIndex].gameObject.SetActive(true);

                // Start fade-out coroutine for this panel
                StopCoroutine($"FadePanel_{panelIndex}");
                StartCoroutine(FadePanel(panelIndex, 10f));
            }
            else
            {
                panelGroups[panelIndex].gameObject.SetActive(false);
            }
        }
    }

    private System.Collections.IEnumerator FadePanel(int panelIndex, float delay)
    {
        yield return new WaitForSeconds(delay);

        CanvasGroup group = panelGroups[panelIndex];
        float fadeTime = 1f;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            group.alpha = 1f - (timer / fadeTime);
            yield return null;
        }

        group.gameObject.SetActive(false);
        // Optional: Clear text after fade
        messageTexts[panelIndex].text = "";
    }
}