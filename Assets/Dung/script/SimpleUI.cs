using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SimpleUI : MonoBehaviour
{
    private Canvas uiCanvas;
    private GameObject activeCardContainer;
    private Action<QuestionResult> currentResultCallback;
    private Question currentQuestion;
    private float timeRemaining;
    private bool questionActive;

    [Tooltip("Animation object shown when the swipe result is correct.")]
    public GameObject correctAnimationObject;

    [Tooltip("Animation object shown when the swipe result is incorrect.")]
    public GameObject incorrectAnimationObject;

    [Tooltip("Prefab used to display the correct answer when the player answers incorrectly.")]
    public GameObject correctAnswerDisplayPrefab;

    [Tooltip("Single animation object used for both correct and incorrect swipe results when the dedicated fields are not set.")]
    public GameObject swipeAnimationObject;

    [Tooltip("Optional per-card animation prefab shown when the swipe result is correct.")]
    public GameObject questionCorrectResultAnimationPrefab;

    [Tooltip("Optional per-card animation prefab shown when the swipe result is incorrect.")]
    public GameObject questionIncorrectResultAnimationPrefab;

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

    public void ShowQuestion(Question question, float timeLimit, Action<QuestionResult> resultCallback, QuestionPresentationMode mode)
    {
        EnsureUI();

        if (question == null)
        {
            resultCallback?.Invoke(QuestionResult.Timeout);
            return;
        }

        DestroyActiveCard();

        activeCardContainer = CreateCardUI(question);
        currentQuestion = question;
        currentResultCallback = resultCallback;
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
        QuestionResult result;
        if (choice < 0 || currentQuestion == null)
        {
            result = QuestionResult.Timeout;
        }
        else
        {
            bool swipedRight = (choice == 1);
            result = (swipedRight && currentQuestion.isMatch) || (!swipedRight && !currentQuestion.isMatch)
                ? QuestionResult.Correct
                : QuestionResult.Incorrect;
        }

        ShowResultAnimation(result, currentQuestion);
        DestroyActiveCard();
        currentResultCallback?.Invoke(result);
        currentResultCallback = null;
        currentQuestion = null;
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

    private void ShowResultAnimation(QuestionResult result, Question question)
    {
        GameObject animationObject = null;
        if (result == QuestionResult.Correct)
        {
            animationObject = question?.correctResultAnimationPrefab ?? correctAnimationObject;
        }
        else if (result == QuestionResult.Incorrect)
        {
            animationObject = question?.incorrectResultAnimationPrefab ?? incorrectAnimationObject;
        }

        if (animationObject == null)
        {
            animationObject = swipeAnimationObject;
        }
        if (animationObject == null) return;

        Canvas canvas = uiCanvas ?? UnityEngine.Object.FindAnyObjectByType<Canvas>();
        GameObject instance = Instantiate(animationObject);
        instance.SetActive(true);

        RectTransform rt = instance.GetComponent<RectTransform>();

        // Parent to canvas so UI animations render in screen space.
        if (canvas != null)
        {
            instance.transform.SetParent(canvas.transform, false);
            instance.transform.SetAsLastSibling();
        }

        // Preserve per-card prefab layout if the instantiated animation came from a card-specific prefab.
        bool preservePrefabLayout = (question != null && (animationObject == question.correctResultAnimationPrefab || animationObject == question.incorrectResultAnimationPrefab));

        bool isFallbackSwipe = animationObject == swipeAnimationObject;

        if (rt != null && canvas != null)
        {
            if (!preservePrefabLayout)
            {
                if (isFallbackSwipe)
                {
                    // Keep generic swipe animation centered
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(260f, 260f);
                }
                else
                {
                    // Global correct/incorrect animations should appear at screen sides
                    Vector2 sideAnchor = result == QuestionResult.Incorrect ? new Vector2(0.1f, 0.5f) : new Vector2(0.9f, 0.5f);
                    rt.anchorMin = sideAnchor;
                    rt.anchorMax = sideAnchor;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = result == QuestionResult.Incorrect ? new Vector2(-80f, 0f) : new Vector2(80f, 0f);
                    rt.sizeDelta = new Vector2(260f, 260f);
                }
            }

            if (rt.localScale == Vector3.zero)
            {
                rt.localScale = Vector3.one;
            }
        }
        else if (canvas == null)
        {
            // World-space fallback: position at screen side/center when not preserving prefab layout
            instance.transform.SetParent(null);
            Camera cam = Camera.main ?? Camera.current;
            if (cam != null)
            {
                float xFactor = isFallbackSwipe ? 0.5f : (result == QuestionResult.Incorrect ? 0.25f : 0.75f);
                if (preservePrefabLayout)
                {
                    // keep prefab world position (do nothing)
                }
                else
                {
                    Vector3 screenCenter = new Vector3(Screen.width * xFactor, Screen.height * 0.5f, 10f);
                    instance.transform.position = cam.ScreenToWorldPoint(screenCenter);
                }
            }

            if (instance.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                spriteRenderer.sortingOrder = 32767;
            }
        }

        if (instance.TryGetComponent(out ParticleSystem particleSystem))
        {
            particleSystem.Play(true);
        }

        if (instance.TryGetComponent(out Animator animator))
        {
            animator.enabled = true;
            animator.Play(0, -1, 0f);
        }

        if (instance.TryGetComponent(out Animation animationComponent))
        {
            animationComponent.Play();
        }

        if (result == QuestionResult.Incorrect && question != null)
        {
            ShowCorrectAnswerFeedback(question);
        }

        Destroy(instance, 2f);
    }

    private void ShowCorrectAnswerFeedback(Question question)
    {
        if (correctAnswerDisplayPrefab == null) return;

        GameObject feedback = Instantiate(correctAnswerDisplayPrefab, uiCanvas != null ? uiCanvas.transform : null, false);
        feedback.SetActive(true);

        RectTransform rt = feedback.GetComponent<RectTransform>();
        if (rt != null)
        {
            // Center the feedback in the middle of the screen so the text/image appears clearly
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            if (rt.sizeDelta == Vector2.zero)
            {
                rt.sizeDelta = new Vector2(700f, 220f);
            }
            if (rt.localScale == Vector3.zero)
            {
                rt.localScale = Vector3.one;
            }
        }

        string answerText = question != null ? question.correctAnswerText : string.Empty;
        bool hasSprite = question != null && question.correctAnswerSprite != null;

        var textComponents = feedback.GetComponentsInChildren<TextMeshProUGUI>(true);
        Image imageComponent = null;

        // Prefer child named LabelImage
        var labelTransform = feedback.transform.Find("LabelImage");
        if (labelTransform != null)
        {
            imageComponent = labelTransform.GetComponent<Image>();
        }

        // Otherwise prefer any child Image (not the root)
        if (imageComponent == null)
        {
            var imgs = feedback.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                if (img.gameObject == feedback) continue; // skip root for now
                imageComponent = img;
                break;
            }
        }

        // Fallback to root Image if nothing else found
        if (imageComponent == null)
        {
            imageComponent = feedback.GetComponent<Image>();
        }

        if (hasSprite)
        {
            // Hide all text components
            foreach (var t in textComponents) { t.text = string.Empty; t.enabled = false; }

            // Try to find a child Image whose sprite already matches the desired sprite
            Image matchingImage = null;
            var allImages = feedback.GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                if (img.sprite == question.correctAnswerSprite)
                {
                    matchingImage = img;
                    break;
                }
            }

            // If none matched by reference, try matching by name
            if (matchingImage == null && question.correctAnswerSprite != null)
            {
                foreach (var img in allImages)
                {
                    if (img.sprite != null && img.sprite.name == question.correctAnswerSprite.name)
                    {
                        matchingImage = img;
                        break;
                    }
                }
            }

            if (matchingImage != null)
            {
                // Disable other child images (but keep root background if present)
                foreach (var img in allImages)
                {
                    if (img == matchingImage) continue;
                    if (img.gameObject == feedback) continue;
                    img.enabled = false;
                }
                matchingImage.enabled = true;
                matchingImage.sprite = question.correctAnswerSprite;
                matchingImage.color = Color.white;
                matchingImage.preserveAspect = true;
            }
            else if (imageComponent != null)
            {
                // No matching child found; use the selected imageComponent as the single display
                var allImgs = feedback.GetComponentsInChildren<Image>(true);
                foreach (var img in allImgs)
                {
                    if (img.gameObject == feedback) continue;
                    img.enabled = false;
                }
                imageComponent.sprite = question.correctAnswerSprite;
                imageComponent.color = Color.white;
                imageComponent.preserveAspect = true;
                imageComponent.enabled = true;
            }
            else
            {
                Debug.LogWarning("ShowCorrectAnswerFeedback: no Image found in prefab to show correctAnswerSprite.");
            }
        }
        else
        {
            // Show text; do not disable root image (background). Disable only non-root images to avoid hiding prefab background.
            var allImages = feedback.GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                if (img.gameObject == feedback) continue; // keep root
                img.enabled = false;
            }
            foreach (var t in textComponents)
            {
                t.text = answerText;
                t.enabled = true;
            }
        }

        Destroy(feedback, 2f);
    }
}
