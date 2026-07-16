using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SimpleUI : MonoBehaviour
{
    private Canvas uiCanvas;
    private GameObject activeCardContainer;
    private Action<int> currentChoiceCallback;
    private float timeRemaining;
    private bool questionActive;

    void Awake()
    {
        EnsureUI();
    }

    void Update()
    {
        if (!questionActive) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            questionActive = false;
            SubmitChoiceInternal(-1);
        }
    }

    public void ShowQuestion(Question question, float timeLimit, Action<int> resultCallback, QuestionPresentationMode mode)
    {
        EnsureUI();

        if (question == null)
        {
            resultCallback?.Invoke(-1);
            return;
        }

        DestroyActiveCard();

        activeCardContainer = CreateCardUI(question);
        currentChoiceCallback = resultCallback;
        timeRemaining = Mathf.Max(0.1f, timeLimit);
        questionActive = true;
    }

    public void SubmitChoice(int choice)
    {
        if (!questionActive) return;

        questionActive = false;
        SubmitChoiceInternal(choice);
    }

    private void SubmitChoiceInternal(int choice)
    {
        DestroyActiveCard();
        currentChoiceCallback?.Invoke(choice);
        currentChoiceCallback = null;
    }

    private void DestroyActiveCard()
    {
        if (activeCardContainer != null)
        {
            Destroy(activeCardContainer);
            activeCardContainer = null;
        }
    }

    private void EnsureUI()
    {
        if (uiCanvas != null) return;

        uiCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (uiCanvas == null)
        {
            GameObject canvasObject = new GameObject("SimpleUICanvas");
            uiCanvas = canvasObject.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            if (uiCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                uiCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }

    private GameObject CreateCardUI(Question question)
    {
        GameObject container = new GameObject("CardQuestionUI", typeof(RectTransform));
        container.transform.SetParent(uiCanvas.transform, false);
        var containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        GameObject overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(container.transform, false);
        var overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.55f);
        overlayImage.raycastTarget = true;
        var overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        GameObject card = new GameObject("CardPanel", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(container.transform, false);
        var cardImage = card.GetComponent<Image>();
        cardImage.color = new Color(1f, 1f, 1f, 0.98f);
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(540f, 760f);
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;

        var dragCard = card.AddComponent<CardQuestion>();
        dragCard.onChoice = SubmitChoice;
        dragCard.swipeThreshold = 120f;

        // Use the provided sprite directly as the card artwork (no extra white background created by script)
        if (question.cardImage != null)
        {
            cardImage.sprite = question.cardImage;
            cardImage.color = Color.white;
            cardImage.preserveAspect = true;
        }
        else
        {
            // fallback: keep light panel background when no sprite provided
            cardImage.color = new Color(1f, 1f, 1f, 0.98f);
        }

        GameObject promptImageObj = null;
        Image promptImage = null;

        if (question.promptSprite != null)
        {
            promptImageObj = new GameObject("PromptImage", typeof(RectTransform), typeof(Image));
            promptImageObj.transform.SetParent(card.transform, false);
            promptImage = promptImageObj.GetComponent<Image>();
            promptImage.sprite = question.promptSprite;
            promptImage.preserveAspect = true;
            promptImage.color = Color.white;
            var promptImgRect = promptImageObj.GetComponent<RectTransform>();

            // If the Question includes a promptAnchor RectTransform, copy its layout properties
            if (question.promptAnchor != null)
            {
                var a = question.promptAnchor;
                promptImgRect.anchorMin = a.anchorMin;
                promptImgRect.anchorMax = a.anchorMax;
                promptImgRect.pivot = a.pivot;
                promptImgRect.sizeDelta = a.sizeDelta;
                promptImgRect.anchoredPosition = a.anchoredPosition;
            }
            else
            {
                // default layout
                promptImgRect.anchorMin = new Vector2(0.5f, 1f);
                promptImgRect.anchorMax = new Vector2(0.5f, 1f);
                promptImgRect.pivot = new Vector2(0.5f, 1f);
                promptImgRect.sizeDelta = new Vector2(300f, 140f);
                promptImgRect.anchoredPosition = new Vector2(0f, -430f);
            }
        }
        else
        {
            Debug.LogWarning("SimpleUI: question.promptSprite is null — no prompt image will be shown.");
        }

        // No text UI: only images. Leave statusText null so CardQuestion won't show text.
        dragCard.statusText = null;
        dragCard.cardImage = cardImage;
        dragCard.promptImage = promptImage;

        return container;
    }

    private void CreateHeaderText(Transform parent, string name, string text, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject header = new GameObject(name, typeof(RectTransform));
        header.transform.SetParent(parent, false);
        var tmp = header.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = new Color(0.08f, 0.08f, 0.08f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        var rect = header.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }

    private void CreateFooterText(Transform parent, string name, string text, int fontSize, Vector2 anchor, Vector2 pivot, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = new Color(0.08f, 0.08f, 0.08f);
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(anchor.x < 0.5f ? -120f : 120f, 40f);
    }
}
