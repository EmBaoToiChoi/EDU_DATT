using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardQuestion : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Action<int> onChoice;
    public TextMeshProUGUI statusText;
    public Image cardImage;
    public Image promptImage;
    public GameObject correctAnimationObject;
    public GameObject incorrectAnimationObject;
    public float swipeThreshold = 120f;
    public float swipeAnimationDuration = 0.35f;

    private RectTransform rectTransform;
    private Vector2 startPointerPosition;
    private Vector3 originalPosition;
    private bool dragging;
    private bool animating;
    private Coroutine animateCoroutine;
    private int pendingChoice = -1;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        startPointerPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        Vector2 delta = eventData.position - startPointerPosition;
        rectTransform.anchoredPosition = originalPosition + new Vector3(delta.x, delta.y, 0f);

        float tilt = Mathf.Clamp(delta.x / 320f, -1f, 1f) * 18f;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);

        if (statusText != null)
        {
            if (delta.x > swipeThreshold * 0.5f)
            {
                statusText.text = "Quẹt phải = Đúng";
            }
            else if (delta.x < -swipeThreshold * 0.5f)
            {
                statusText.text = "Quẹt trái = Sai";
            }
            else
            {
                statusText.text = "Kéo thẻ để trả lời";
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        dragging = false;
        Vector2 delta = eventData.position - startPointerPosition;
        float horizontal = delta.x;
        if (Mathf.Abs(horizontal) >= swipeThreshold)
        {
            pendingChoice = horizontal > 0 ? 1 : 0;
            ShowSwipeAnimation(horizontal > 0 ? 1 : -1);
            StartSwipeAnimation(horizontal > 0 ? 1 : -1);
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.localRotation = Quaternion.identity;
            if (statusText != null)
            {
                statusText.text = "Kéo thẻ để trả lời";
            }
        }
    }

    private void ShowSwipeAnimation(int direction)
    {
        GameObject animationObject = direction > 0 ? correctAnimationObject : incorrectAnimationObject;
        if (animationObject == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        GameObject instance = Instantiate(animationObject, canvas.transform, false);
        RectTransform rt = instance.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = instance.AddComponent<RectTransform>();
        }

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = direction > 0
            ? new Vector2(420f, 0f)
            : new Vector2(-420f, 0f);
        rt.sizeDelta = new Vector2(220f, 220f);
        rt.localScale = Vector3.one;

        Destroy(instance, 0.7f);
    }

    private void StartSwipeAnimation(int direction)
    {
        if (animating) return;

        animating = true;
        if (animateCoroutine != null)
        {
            StopCoroutine(animateCoroutine);
        }
        animateCoroutine = StartCoroutine(AnimateSwipe(direction));
    }

    private IEnumerator AnimateSwipe(int direction)
    {
        float duration = swipeAnimationDuration;
        float elapsed = 0f;
        Vector3 startPos = rectTransform.anchoredPosition;
        Quaternion startRot = rectTransform.localRotation;
        float targetX = direction * 1400f;
        float targetY = 200f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rectTransform.anchoredPosition = Vector3.Lerp(startPos, new Vector3(targetX, targetY, 0f), eased);
            rectTransform.localRotation = Quaternion.Lerp(startRot, Quaternion.Euler(0f, 0f, direction * 25f), eased);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = new Vector3(targetX, targetY, 0f);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, direction * 25f);
        if (pendingChoice >= 0)
        {
            int choiceToReport = pendingChoice;
            pendingChoice = -1;
            onChoice?.Invoke(choiceToReport);
        }
        animating = false;
        animateCoroutine = null;
    }
}
