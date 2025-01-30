using HarmonyLib;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StubosShowXP.Patches;
public class ShowXPGain : MonoBehaviour
{
    private static GameObject notificationParent;
    private static ShowXPGain instance;
    private static float notificationHeight = 25f;
    private static float spacingBetweenNotifications = 2f;
    private static int maxNotifications = 30;
    private static readonly Color xpTextColor = new Color(0.7f, 1f, 0.3f, 1f);
    private static float lastNotificationTime = 0f;
    private static float notificationCooldown = 0.1f;

    private class NotificationItem
    {
        public GameObject gameObject { get; set; }
        public TextMeshProUGUI text { get; set; }
        public float currentAlpha = 1f;
    }

    private static readonly Queue<NotificationItem> activeNotifications = new();

    private static void CreateNotificationParent()
    {
        notificationParent = new GameObject("XPNotificationParent");
        instance = notificationParent.AddComponent<ShowXPGain>();

        var canvas = notificationParent.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = notificationParent.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static NotificationItem CreateNotification(string message)
    {
        var notificationObj = new GameObject("Notification");
        notificationObj.transform.SetParent(notificationParent.transform, false);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(notificationObj.transform, false);

        var text = textObj.AddComponent<TextMeshProUGUI>();

        var gameFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
            .FirstOrDefault(f => f.name == "Erika Ormig SDF Menu");

        if (gameFont != null)
        {
            text.font = gameFont;
        }

        text.fontSize = 18;
        text.color = xpTextColor;
        text.text = message;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Truncate;
        text.alignment = TextAlignmentOptions.Right;

        var rectTransform = notificationObj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = notificationObj.AddComponent<RectTransform>();
        }
        rectTransform.anchorMin = new Vector2(1, 0.5f);
        rectTransform.anchorMax = new Vector2(1, 0.5f);
        rectTransform.pivot = new Vector2(1, 0.5f);
        rectTransform.sizeDelta = new Vector2(300, notificationHeight);

        var textRectTransform = textObj.GetComponent<RectTransform>();
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.sizeDelta = Vector2.zero;
        textRectTransform.anchoredPosition = Vector2.zero;

        return new NotificationItem
        {
            gameObject = notificationObj,
            text = text
        };
    }

    private void UpdateNotificationPositions()
    {
        try
        {
            int index = 0;
            foreach (var notification in activeNotifications.Where(n => n?.gameObject != null))
            {
                var rectTransform = notification.gameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    var targetY = 100 - (index * (notificationHeight + spacingBetweenNotifications));
                    rectTransform.anchoredPosition = new Vector2(-20, targetY);
                    index++;
                }
            }
        }
        catch (System.Exception e)
        {
            Plugin.Log.LogError($"Error in UpdateNotificationPositions: {e}");
        }
    }

    private IEnumerator FadeOutNotification(NotificationItem notification)
    {
        if (notification == null || notification.text == null) yield break;

        float duration = 4f;
        float fadeTime = 1f;

        yield return new WaitForSeconds(duration);

        float startTime = Time.time;
        while (Time.time - startTime < fadeTime)
        {
            if (notification == null || notification.text == null) break;

            float t = (Time.time - startTime) / fadeTime;
            notification.currentAlpha = 1f - t;
            notification.text.color = new Color(xpTextColor.r, xpTextColor.g, xpTextColor.b, notification.currentAlpha);
            yield return null;
        }

        if (notification?.gameObject != null)
        {
            activeNotifications.Dequeue();
            Destroy(notification.gameObject);
            UpdateNotificationPositions();
        }
    }

    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.IncreaseSkill))]
    [HarmonyPostfix]
    static void Postfix(PlayerStats __instance, PlayerStats.PlayerSkillType Type, float amount, bool notification)
    {
        if (notification) return;
        if (amount < 0.001f) return;

        if (Time.time - lastNotificationTime < notificationCooldown) return;

        lastNotificationTime = Time.time;

        if (notificationParent == null)
        {
            try
            {
                CreateNotificationParent();
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Failed to create UI parent: {e}");
                return;
            }
        }

        try
        {
            while (activeNotifications.Count >= maxNotifications)
            {
                var oldNotification = activeNotifications.Dequeue();
                if (oldNotification?.gameObject != null)
                    Destroy(oldNotification.gameObject);
            }

            var newNotification = CreateNotification($"+{amount:F3} {Type} XP");
            if (newNotification != null && instance != null)
            {
                activeNotifications.Enqueue(newNotification);
                instance.UpdateNotificationPositions();
                instance.StartCoroutine(instance.FadeOutNotification(newNotification));
            }
        }
        catch (System.Exception e)
        {
            Plugin.Log.LogError($"Failed to create notification: {e}");
        }
    }
}