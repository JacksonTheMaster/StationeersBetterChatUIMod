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
    private ContentHandler contentHandler;

    private GameObject uiInstance;
    private TextMeshProUGUI[] messageTexts = new TextMeshProUGUI[5];
    private CanvasGroup[] panelGroups = new CanvasGroup[5];

    public void Initialize(BetterChatUI modInstance, ContentHandler content)
    {
        contentHandler = content;
        LoadAndCreateUI();
    }

    private void LoadAndCreateUI()
    {
        if (contentHandler == null)
        {
            Debug.LogError("BetterChatUI: contentHandler is null!");
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

        var canvasTransform = uiInstance.transform.Find("Canvas");
        if (canvasTransform == null)
        {
            Debug.LogError("BetterChatUI: Could not find 'Canvas' under root!");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            int panelNum = i + 1;
            var panel = canvasTransform.Find($"Panel{panelNum}");
            if (panel == null)
            {
                Debug.LogWarning($"Panel{panelNum} not found!");
                continue;
            }

            panelGroups[i] = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();

            var text = panel.Find($"Message{panelNum}")?.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                messageTexts[i] = text;
                text.text = "";
                panel.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"Message{panelNum} not found!");
            }
        }
    }

    public void AddMessage(string message)
    {
        // Stop all ongoing fade coroutines to prevent conflicts
        for (int i = 0; i < 5; i++)
        {
            if (fadeCoroutines[i] != null)
            {
                StopCoroutine(fadeCoroutines[i]);
                fadeCoroutines[i] = null;
            }
        }

        // Shift messages down (text only)
        for (int i = 4; i > 0; i--)
        {
            messageTexts[i].text = messageTexts[i - 1].text;
        }

        // Add new message to top (Panel1 / index 0)
        messageTexts[0].text = message;

        // Update visibility and start fresh fades for all visible panels
        UpdateDisplayWithFreshFades();
    }

    private void UpdateDisplayWithFreshFades()
    {
        for (int i = 0; i < 5; i++)
        {
            string msg = messageTexts[i].text;

            if (!string.IsNullOrEmpty(msg))
            {
                panelGroups[i].alpha = 1f;
                panelGroups[i].gameObject.SetActive(true);

                // Start a fresh 10-second fade for this panel
                fadeCoroutines[i] = StartCoroutine(FadePanel(i, 10f));
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
        messageTexts[panelIndex].text = "";
        fadeCoroutines[panelIndex] = null;
    }

    // Add this field at the top of the class (next to other arrays)
    private Coroutine[] fadeCoroutines = new Coroutine[5];
}