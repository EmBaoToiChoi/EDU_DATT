using System;
using System.Collections;
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
    private float currentTimeLimit;
    private bool questionActive;
    private RectTransform rejectButtonRect;
    private RectTransform acceptButtonRect;

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

    [Header("Fallback animal feedback")]
    [Tooltip("Optional list of animal animation prefabs to show for correct answers when no specific animation is assigned.")]
    public GameObject[] correctFallbackFeedbackPrefabs;

    [Tooltip("Optional list of animal animation prefabs to show for incorrect answers when no specific animation is assigned.")]
    public GameObject[] incorrectFallbackFeedbackPrefabs;

    [Tooltip("If enabled, a random prefab will be picked from the fallback list for each result.")]
    public bool useRandomFallbackAnimal = true;

    [Tooltip("Object containing the sprite and optional animation for the X button. Drag a GameObject with Image/SpriteRenderer and Animator here.")]
    public GameObject rejectButtonVisualObject;

    [Tooltip("Object containing the sprite and optional animation for the check button. Drag a GameObject with Image/SpriteRenderer and Animator here.")]
    public GameObject acceptButtonVisualObject;

    [Tooltip("Position of the reject button relative to the card center.")]
    public Vector2 rejectButtonPosition = new Vector2(-70f, -240f);

    [Tooltip("Position of the accept button relative to the card center.")]
    public Vector2 acceptButtonPosition = new Vector2(70f, -240f);

    [Tooltip("Optional animation prefab/object shown when dragging the card toward one side.")]
    public GameObject swipeFeedbackAnimationPrefab;

    [Tooltip("Audio clip played when the answer is correct.")]
    public AudioClip correctAnswerAudio;

    [Tooltip("Audio clip played when the answer is incorrect.")]
    public AudioClip incorrectAnswerAudio;

    [Tooltip("Optional UI object shown as the timer image on the card.")]
    public GameObject timerDisplayObject;

    void Awake()
    {
        EnsureUI();
    }

    void Update()
    {
        if (!questionActive) return;

        timeRemaining -= Time.deltaTime;

        if (activeCardContainer != null)
        {
            var timerBar = activeCardContainer.GetComponentInChildren<bartime>(true);
            if (timerBar != null)
            {
                timerBar.SetTimeRemaining(timeRemaining);
            }
        }

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

        currentQuestion = question;
        currentResultCallback = resultCallback;
        currentTimeLimit = Mathf.Max(0.1f, timeLimit);
        timeRemaining = currentTimeLimit;
        questionActive = true;

        activeCardContainer = CreateCardUI(question);
        if (activeCardContainer != null)
        {
            var timerBar = activeCardContainer.GetComponentInChildren<bartime>(true);
            if (timerBar != null)
            {
                timerBar.ResetTimer(currentTimeLimit);
                timerBar.SetTimeRemaining(timeRemaining);
            }
        }
    }

    public void SubmitChoice(int choice)
    {
        if (!questionActive) return;

        questionActive = false;
        
        // Animate button scale based on choice
        if (choice == 1 && acceptButtonRect != null)
        {
            StartCoroutine(AnimateButtonScale(acceptButtonRect));
            PlayButtonAnimation(acceptButtonVisualObject, acceptButtonRect);
        }
        else if (choice == -1 && rejectButtonRect != null)
        {
            StartCoroutine(AnimateButtonScale(rejectButtonRect));
            PlayButtonAnimation(rejectButtonVisualObject, rejectButtonRect);
        }

        SubmitChoiceInternal(choice);
    }

    private void PlayButtonAnimation(GameObject visualSource, RectTransform buttonRect)
    {
        if (buttonRect == null) return;

        // Prefer animator on a child instance (created by ApplyButtonVisual)
        var childAnimator = buttonRect.GetComponentInChildren<Animator>(true);
        if (childAnimator != null && childAnimator.runtimeAnimatorController != null)
        {
            childAnimator.Play(0);
            return;
        }

        // Fallback: copy runtime controller to button GameObject Animator
        var buttonObject = buttonRect.gameObject;
        var animator = buttonObject.GetComponent<Animator>() ?? buttonObject.AddComponent<Animator>();

        if (visualSource != null)
        {
            var sourceAnimator = visualSource.GetComponent<Animator>();
            if (sourceAnimator != null && sourceAnimator.runtimeAnimatorController != null)
            {
                animator.runtimeAnimatorController = sourceAnimator.runtimeAnimatorController;
            }
        }

        if (animator.runtimeAnimatorController != null)
        {
            animator.Play(0);
        }
    }

    private IEnumerator AnimateButtonScale(RectTransform buttonRect)
    {
        Vector3 startScale = buttonRect.localScale;
        Vector3 targetScale = startScale * 1.5f;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            buttonRect.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }

        buttonRect.localScale = targetScale;
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

        PlayAnswerAudio(result);
        ShowResultAnimation(result, currentQuestion);
        StartCoroutine(WaitThenFinishResult(result));
    }

    private IEnumerator WaitThenFinishResult(QuestionResult result)
    {
        yield return new WaitForSeconds(2.1f);
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

        // First, try to find the PersistentOverlayCanvas created by GamePlay
        GamePlay gamePlay = FindObjectOfType<GamePlay>();
        if (gamePlay != null)
        {
            // Access GamePlay's overlay canvas through GetOrCreateOverlayCanvas (if accessible)
            // For now, let's find any active screen-space overlay canvas
            Canvas[] allCanvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
            foreach (var canvas in allCanvases)
            {
                if (canvas == null) continue;
                if (!canvas.gameObject.activeInHierarchy) continue;
                if (canvas.name == "PersistentOverlayCanvas")
                {
                    uiCanvas = canvas;
                    break;
                }
            }
        }

        // Prefer an active screen-space overlay canvas if one exists.
        if (uiCanvas == null)
        {
            Canvas[] allCanvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
            foreach (var canvas in allCanvases)
            {
                if (canvas == null) continue;
                if (!canvas.gameObject.activeInHierarchy) continue;
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    uiCanvas = canvas;
                    break;
                }
            }
        }

        // If no active overlay canvas found, create one
        if (uiCanvas == null)
        {
            GameObject canvasObject = new GameObject("PersistentOverlayCanvas");
            uiCanvas = canvasObject.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.overrideSorting = true;
            uiCanvas.sortingOrder = 1000;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            uiCanvas.gameObject.SetActive(true);
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.overrideSorting = true;
            if (uiCanvas.sortingOrder < 1000)
            {
                uiCanvas.sortingOrder = 1000;
            }
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
        overlayImage.raycastTarget = false;
        var overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.transform.SetAsFirstSibling();  // Ensure Overlay renders behind everything

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
        dragCard.buttonScaleOnDrag = 1.12f;

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

        // Create Reject (X) button on the left
        GameObject rejectButton = new GameObject("RejectButton", typeof(RectTransform), typeof(Image));
        rejectButton.transform.SetParent(container.transform, false);
        rejectButtonRect = rejectButton.GetComponent<RectTransform>();
        rejectButtonRect.anchorMin = new Vector2(0.5f, 0.15f);
        rejectButtonRect.anchorMax = new Vector2(0.5f, 0.15f);
        rejectButtonRect.pivot = new Vector2(0.5f, 0.5f);
        rejectButtonRect.sizeDelta = new Vector2(100f, 100f);
        rejectButtonRect.anchoredPosition = rejectButtonPosition;
        var rejectImage = rejectButton.GetComponent<Image>();
        ApplyButtonVisual(rejectImage, rejectButton, rejectButtonVisualObject, new Color(1f, 0.3f, 0.3f, 0.8f));
        rejectImage.raycastTarget = false;

        // Create Accept (Check) button on the right
        GameObject acceptButton = new GameObject("AcceptButton", typeof(RectTransform), typeof(Image));
        acceptButton.transform.SetParent(container.transform, false);
        acceptButtonRect = acceptButton.GetComponent<RectTransform>();
        acceptButtonRect.anchorMin = new Vector2(0.5f, 0.15f);
        acceptButtonRect.anchorMax = new Vector2(0.5f, 0.15f);
        acceptButtonRect.pivot = new Vector2(0.5f, 0.5f);
        acceptButtonRect.sizeDelta = new Vector2(100f, 100f);
        acceptButtonRect.anchoredPosition = acceptButtonPosition;
        var acceptImage = acceptButton.GetComponent<Image>();
        ApplyButtonVisual(acceptImage, acceptButton, acceptButtonVisualObject, new Color(0.3f, 1f, 0.3f, 0.8f));
        acceptImage.raycastTarget = false;

        dragCard.rejectButton = rejectButtonRect;
        dragCard.acceptButton = acceptButtonRect;
        dragCard.swipeFeedbackAnimationPrefab = swipeFeedbackAnimationPrefab;

        if (timerDisplayObject != null)
        {
            GameObject timerObject = Instantiate(timerDisplayObject, card.transform, false);
            timerObject.name = "TimerDisplay";
            timerObject.SetActive(true);

            RectTransform timerRect = timerObject.GetComponent<RectTransform>();
            if (timerRect == null)
            {
                timerRect = timerObject.AddComponent<RectTransform>();
            }

            timerRect.anchorMin = new Vector2(0.5f, 1f);
            timerRect.anchorMax = new Vector2(0.5f, 1f);
            timerRect.pivot = new Vector2(0.5f, 1f);
            timerRect.sizeDelta = new Vector2(120f, 120f);
            timerRect.anchoredPosition = new Vector2(0f, -40f);
            timerRect.SetAsLastSibling();

            Image timerImage = timerObject.GetComponent<Image>();
            if (timerImage == null)
            {
                var childImages = timerObject.GetComponentsInChildren<Image>(true);
                if (childImages.Length > 0)
                {
                    timerImage = childImages[0];
                }
            }

            if (timerImage != null)
            {
                Sprite timerSprite = null;
                if (timerDisplayObject.TryGetComponent(out Image sourceImage) && sourceImage.sprite != null)
                {
                    timerSprite = sourceImage.sprite;
                }
                else if (timerDisplayObject.TryGetComponent(out SpriteRenderer sourceSpriteRenderer) && sourceSpriteRenderer.sprite != null)
                {
                    timerSprite = sourceSpriteRenderer.sprite;
                }

                if (timerSprite != null)
                {
                    timerImage.sprite = timerSprite;
                }

                timerImage.color = Color.white;
                timerImage.preserveAspect = true;
                timerImage.raycastTarget = false;
            }

            var timerBar = timerObject.GetComponent<bartime>() ?? timerObject.AddComponent<bartime>();
            timerBar.SetDuration(currentTimeLimit);
            timerBar.SetTimeRemaining(currentTimeLimit);
        }

        return container;
    }

    private void ApplyButtonVisual(Image targetImage, GameObject buttonObject, GameObject visualSource, Color fallbackColor)
    {
        if (targetImage == null || buttonObject == null) return;

        if (visualSource != null)
        {
            // Instantiate a copy of the provided visual object under the button so its Animator/child layout works.
            GameObject instance = Instantiate(visualSource, buttonObject.transform, false);
            instance.SetActive(true);

            RectTransform instRect = instance.GetComponent<RectTransform>();
            if (instRect != null)
            {
                instRect.anchorMin = new Vector2(0.5f, 0.5f);
                instRect.anchorMax = new Vector2(0.5f, 0.5f);
                instRect.pivot = new Vector2(0.5f, 0.5f);
                instRect.sizeDelta = targetImage.rectTransform.sizeDelta;
                instRect.anchoredPosition = Vector2.zero;
            }

            // If the visual instance has an Image with a sprite, sync it to the target Image and disable underlying image so animation visuals are visible.
            var instImage = instance.GetComponent<Image>();
            if (instImage != null && instImage.sprite != null)
            {
                targetImage.sprite = instImage.sprite;
                targetImage.color = Color.white;
                targetImage.preserveAspect = true;
                targetImage.enabled = false; // show instance's image/animation instead
            }

            // Play Animator if present on the instance
            var instAnimator = instance.GetComponent<Animator>();
            if (instAnimator != null && instAnimator.runtimeAnimatorController != null)
            {
                instAnimator.Play(0);
            }

            return;
        }

        targetImage.color = fallbackColor;
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

    private void PlayAnswerAudio(QuestionResult result)
    {
        if (result == QuestionResult.Correct && correctAnswerAudio != null)
        {
            AudioSource.PlayClipAtPoint(correctAnswerAudio, Camera.main ? Camera.main.transform.position : Vector3.zero);
        }
        else if (result == QuestionResult.Incorrect && incorrectAnswerAudio != null)
        {
            AudioSource.PlayClipAtPoint(incorrectAnswerAudio, Camera.main ? Camera.main.transform.position : Vector3.zero);
        }
    }

    private void ShowResultAnimation(QuestionResult result, Question question)
    {
        GameObject animationObject = null;
        if (result == QuestionResult.Correct)
        {
            animationObject = question?.correctResultAnimationPrefab
                ?? questionCorrectResultAnimationPrefab
                ?? correctAnimationObject;
        }
        else if (result == QuestionResult.Incorrect)
        {
            animationObject = question?.incorrectResultAnimationPrefab
                ?? questionIncorrectResultAnimationPrefab
                ?? incorrectAnimationObject;
        }

        if (animationObject == null)
        {
            animationObject = swipeAnimationObject;
        }
        if (animationObject == null)
        {
            animationObject = GetFallbackFeedbackObject(result);
        }
        if (animationObject == null) return;

        Canvas canvas = uiCanvas ?? UnityEngine.Object.FindAnyObjectByType<Canvas>();

        // Instantiate and then copy prefab transform/RectTransform values to preserve editor layout.
        GameObject instance = Instantiate(animationObject);
        instance.SetActive(true);

        RectTransform prefabRT = animationObject.GetComponent<RectTransform>();
        RectTransform rt = instance.GetComponent<RectTransform>();
        bool isUIPrefab = prefabRT != null || instance.GetComponent<Canvas>() != null;

        if (isUIPrefab && canvas != null)
        {
            // Add animation directly to Canvas with high sorting order, not to CardUI
            instance.transform.SetParent(canvas.transform, false);
            instance.transform.SetAsLastSibling();  // Ensure animation renders on top
            instance.transform.localPosition = animationObject.transform.localPosition;
            instance.transform.localRotation = animationObject.transform.localRotation;
            instance.transform.localScale = animationObject.transform.localScale;

            if (prefabRT != null && rt != null)
            {
                rt.anchorMin = prefabRT.anchorMin;
                rt.anchorMax = prefabRT.anchorMax;
                rt.pivot = prefabRT.pivot;
                rt.sizeDelta = prefabRT.sizeDelta;
                rt.anchoredPosition = prefabRT.anchoredPosition;
                rt.localPosition = prefabRT.localPosition;
                rt.localRotation = prefabRT.localRotation;
                rt.localScale = prefabRT.localScale;
            }
            else if (prefabRT == null && rt == null)
            {
                instance.transform.localPosition = animationObject.transform.localPosition;
                instance.transform.localScale = animationObject.transform.localScale;
                instance.transform.localRotation = animationObject.transform.localRotation;
            }

            Canvas animCanvas = instance.GetComponent<Canvas>();
            if (animCanvas != null)
            {
                animCanvas.overrideSorting = true;
                animCanvas.sortingOrder = 32767;
            }
        }
        else
        {
            // Non-UI prefab: keep it in the scene root so SpriteRenderers and world-space objects can display normally.
            instance.transform.SetParent(null);
            instance.transform.localScale = animationObject.transform.localScale;
            instance.transform.localRotation = animationObject.transform.localRotation;
            instance.transform.position = animationObject.transform.position;

            Camera cam = Camera.main ?? Camera.current;
            if (cam != null)
            {
                Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, cam.nearClipPlane + 1f);
                instance.transform.position = cam.ScreenToWorldPoint(screenCenter);
            }
        }

        bool isFallbackSwipe = animationObject == swipeAnimationObject;
        bool preservePrefabLayout = !isFallbackSwipe;

        if (rt != null && canvas != null)
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
            else if (!preservePrefabLayout)
            {
                Vector2 sideAnchor = result == QuestionResult.Incorrect ? new Vector2(0.1f, 0.5f) : new Vector2(0.9f, 0.5f);
                rt.anchorMin = sideAnchor;
                rt.anchorMax = sideAnchor;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = result == QuestionResult.Incorrect ? new Vector2(-80f, 0f) : new Vector2(80f, 0f);
                rt.sizeDelta = new Vector2(260f, 260f);
            }

            if (rt.localScale == Vector3.zero)
            {
                rt.localScale = Vector3.one;
            }

            // Ensure SpriteRenderer renders above overlay
            if (instance.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                spriteRenderer.sortingOrder = 32767;
            }
        }
        else if (canvas == null)
        {
            instance.transform.SetParent(null);
            Camera cam = Camera.main ?? Camera.current;
            if (cam != null && !preservePrefabLayout)
            {
                Vector3 screenCenter = new Vector3(Screen.width * (isFallbackSwipe ? 0.5f : (result == QuestionResult.Incorrect ? 0.25f : 0.75f)), Screen.height * 0.5f, 10f);
                instance.transform.position = cam.ScreenToWorldPoint(screenCenter);
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

        // Ensure all child Images and SpriteRenderers are enabled and visible
        foreach (Image img in instance.GetComponentsInChildren<Image>())
        {
            img.enabled = true;
            if (img.color.a < 0.1f)
                img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
        }

        foreach (SpriteRenderer sr in instance.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.enabled = true;
            if (sr.color.a < 0.1f)
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
        }

        if (question != null)
        {
            ShowCorrectAnswerFeedback(question);
        }

        Destroy(instance, 2f);
    }

    private GameObject GetFallbackFeedbackObject(QuestionResult result)
    {
        GameObject[] candidates = result == QuestionResult.Correct
            ? correctFallbackFeedbackPrefabs
            : incorrectFallbackFeedbackPrefabs;

        if (candidates == null || candidates.Length == 0)
        {
            return null;
        }

        int index = useRandomFallbackAnimal ? UnityEngine.Random.Range(0, candidates.Length) : 0;
        GameObject selected = candidates[Mathf.Clamp(index, 0, candidates.Length - 1)];
        Debug.Log($"SimpleUI: selected fallback feedback prefab [{index}] = {selected?.name ?? "null"} for result {result}");
        return selected;
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
