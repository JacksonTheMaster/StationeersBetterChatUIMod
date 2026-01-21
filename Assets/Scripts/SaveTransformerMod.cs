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
    private SaveTransformerUI chatUI;

    public override void OnLoaded(ContentHandler contentHandler)
    {
        UnityEngine.Debug.Log("BetterChatUI says: Hello World!");
        this.contentHandler = contentHandler;

        Harmony harmony = new Harmony("BetterChatUI");
        harmony.PatchAll();

        PrefabPatch.prefabs = contentHandler.prefabs;
        UnityEngine.Debug.Log("BetterChatUI Loaded with " + contentHandler.prefabs.Count + " prefab(s)");

        // Create persistent manager
        var gameObject = new GameObject("BetterChatUIUI");
        chatUI = gameObject.AddComponent<SaveTransformerUI>();
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
        var ui = Object.FindObjectOfType<SaveTransformerUI>();
        if (ui != null)
        {
            string formatted = $"<color=#FFAA00>{__instance.DisplayName}</color>: {__instance.ChatText}";
            ui.AddChatMessage(formatted);
            UnityEngine.Debug.Log($"[ChatPatch] Added message: {formatted}");
        }
    }
}

public class SaveTransformerUI : MonoBehaviour
{
    private ContentHandler contentHandler;
    private GameObject messagePrefab;

    public void Initialize(BetterChatUI modInstance, ContentHandler content)
    {
        contentHandler = content;
        LoadMessagePrefab();
    }

    private void LoadMessagePrefab()
    {
        // Load the per-message prefab (Canvas → Panel → TMP Text)
        messagePrefab = contentHandler.prefabs.ReverseFind(p => p.name == "ChatMessage");
        if (messagePrefab == null)
        {
            Debug.LogError("BetterChatUI: Could not find prefab 'ChatMessage'!");
        }
        else
        {
            Debug.Log("BetterChatUI: ChatMessage prefab loaded successfully");
        }
    }

    public void AddChatMessage(string messageText)
    {
        if (messagePrefab == null)
            return;

        // Instantiate a FULL new Canvas per message
        GameObject msgCanvas = Object.Instantiate(messagePrefab);

        // Optional: Make it child of this manager (so it gets destroyed with mod unload)
        msgCanvas.transform.SetParent(transform);

        // Find the TMP Text inside
        var tmpText = msgCanvas.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = messageText;
            tmpText.ForceMeshUpdate();
        }
        else
        {
            Debug.LogWarning("No TextMeshProUGUI found in ChatMessageLine prefab!");
        }

        // Auto-fade and destroy after 10 seconds
        StartCoroutine(FadeAndDestroy(msgCanvas, 10f));
    }

    private System.Collections.IEnumerator FadeAndDestroy(GameObject msgObj, float delay)
    {
        // Wait display time
        yield return new WaitForSeconds(delay);

        // Get or add CanvasGroup for fading
        var canvasGroup = msgObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = msgObj.AddComponent<CanvasGroup>();

        // Fade out over 1 second
        float fadeTime = 1f;
        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = 1f - (timer / fadeTime);
            yield return null;
        }

        // Destroy the entire message canvas
        Object.Destroy(msgObj);
    }
}