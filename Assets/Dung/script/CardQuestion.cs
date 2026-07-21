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
    public float swipeThreshold = 120f;
    public float swipeAnimationDuration = 0.35f;

    [Header("Swipe feedback buttons")]
    public RectTransform rejectButton;
    public RectTransform acceptButton;
    [Tooltip("Optional animation prefab/object shown when dragging the card toward one side.")]
    public GameObject swipeFeedbackAnimationPrefab;
    [Tooltip("Object that scales when dragging left/right to reject/accept.")]
    public GameObject rejectHoldObject;
    [Tooltip("Object that scales when dragging left/right to reject/accept.")]
    public GameObject acceptHoldObject;
    [Tooltip("How much the button scales while dragging.")]
    public float buttonScaleOnDrag = 1.35f;
    [Tooltip("How quickly the button reaches the full scale while dragging.")]
    public float buttonScaleSpeed = 10f;

    private RectTransform rectTransform;
    private Vector2 startPointerPosition;
    private Vector3 originalPosition;
    private Vector3 rejectHoldOriginalScale = Vector3.one;
    private GameObject activeSwipeAnimation;
    private int activeSwipeDirection = 0;
    private Vector3 acceptHoldOriginalScale = Vector3.one;
    private bool dragging;
    private bool animating;
    private Coroutine animateCoroutine;
    private int pendingChoice = -1;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;

        if (rejectHoldObject != null)
        {
            rejectHoldOriginalScale = rejectHoldObject.transform.localScale == Vector3.zero ? Vector3.one : rejectHoldObject.transform.localScale;
        }

        if (acceptHoldObject != null)
        {
            acceptHoldOriginalScale = acceptHoldObject.transform.localScale == Vector3.zero ? Vector3.one : acceptHoldObject.transform.localScale;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        startPointerPosition = eventData.position;

        if (rejectButton != null)
        {
            rejectButton.gameObject.SetActive(true);
        }

        if (acceptButton != null)
        {
            acceptButton.gameObject.SetActive(true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        Vector2 delta = eventData.position - startPointerPosition;
        rectTransform.anchoredPosition = originalPosition + new Vector3(delta.x, delta.y, 0f);

        float tilt = Mathf.Clamp(delta.x / 320f, -1f, 1f) * 18f;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);

        float dragRatio = Mathf.Clamp(delta.x / swipeThreshold, -1f, 1f);
        UpdateSwipeButtons(dragRatio);

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
            StartSwipeAnimation(horizontal > 0 ? 1 : -1);
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.localRotation = Quaternion.identity;
            ResetSwipeButtons();
            if (statusText != null)
            {
                statusText.text = "Kéo thẻ để trả lời";
            }
        }
    }

    private void UpdateSwipeButtons(float dragRatio)
    {
        float normalized = Mathf.Clamp01(Mathf.Abs(dragRatio));
        float targetScale = 1f + (buttonScaleOnDrag - 1f) * normalized;
        float tilt = Mathf.Lerp(0f, 10f, normalized);

        bool isLeft = dragRatio < -0.05f;
        bool isRight = dragRatio > 0.05f;

        if (rejectButton != null)
        {
            float rejectScale = isLeft ? targetScale : 1f;
            rejectButton.localScale = Vector3.one * rejectScale;
            rejectButton.localRotation = Quaternion.Euler(0f, 0f, isLeft ? -tilt : 0f);
        }

        if (acceptButton != null)
        {
            float acceptScale = isRight ? targetScale : 1f;
            acceptButton.localScale = Vector3.one * acceptScale;
            acceptButton.localRotation = Quaternion.Euler(0f, 0f, isRight ? tilt : 0f);
        }

        if (rejectHoldObject != null)
        {
            float rejectScale = isLeft ? targetScale : 1f;
            rejectHoldObject.transform.localScale = rejectHoldOriginalScale * rejectScale;
        }

        if (acceptHoldObject != null)
        {
            float acceptScale = isRight ? targetScale : 1f;
            acceptHoldObject.transform.localScale = acceptHoldOriginalScale * acceptScale;
        }

        if (isLeft)
        {
            PlaySwipeFeedbackAnimation(rejectButton, -1);
        }
        else if (isRight)
        {
            PlaySwipeFeedbackAnimation(acceptButton, 1);
        }
    }

    private void ResetSwipeButtons()
    {
        if (rejectButton != null)
        {
            rejectButton.localScale = Vector3.one;
            rejectButton.localRotation = Quaternion.identity;
        }

        if (acceptButton != null)
        {
            acceptButton.localScale = Vector3.one;
            acceptButton.localRotation = Quaternion.identity;
        }

        if (rejectHoldObject != null)
        {
            rejectHoldObject.transform.localScale = rejectHoldOriginalScale;
        }

        if (acceptHoldObject != null)
        {
            acceptHoldObject.transform.localScale = acceptHoldOriginalScale;
        }

        if (activeSwipeAnimation != null)
        {
            Destroy(activeSwipeAnimation);
            activeSwipeAnimation = null;
        }

        activeSwipeDirection = 0;
    }

    private void PlaySwipeFeedbackAnimation(RectTransform targetButton, int direction)
    {
        if (swipeFeedbackAnimationPrefab == null || targetButton == null) return;
        if (activeSwipeAnimation != null && activeSwipeDirection == direction) return;

        if (activeSwipeAnimation != null)
        {
            Destroy(activeSwipeAnimation);
        }

        activeSwipeAnimation = Instantiate(swipeFeedbackAnimationPrefab, targetButton, false);
        activeSwipeAnimation.transform.localPosition = Vector3.zero;
        activeSwipeAnimation.transform.localRotation = Quaternion.identity;
        activeSwipeAnimation.transform.localScale = Vector3.one;
        activeSwipeAnimation.SetActive(true);

        if (activeSwipeAnimation.TryGetComponent(out Animator animator))
        {
            animator.enabled = true;
            animator.Play(0, -1, 0f);
        }

        if (activeSwipeAnimation.TryGetComponent(out Animation animationComponent))
        {
            animationComponent.Play();
        }

        if (activeSwipeAnimation.TryGetComponent(out ParticleSystem particleSystem))
        {
            particleSystem.Play(true);
        }

        activeSwipeDirection = direction;
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

    private void HideSwipeFeedback()
    {
        if (rejectButton != null)
        {
            rejectButton.gameObject.SetActive(false);
        }

        if (acceptButton != null)
        {
            acceptButton.gameObject.SetActive(false);
        }

        if (activeSwipeAnimation != null)
        {
            Destroy(activeSwipeAnimation);
            activeSwipeAnimation = null;
        }

        activeSwipeDirection = 0;
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
            HideSwipeFeedback();
            onChoice?.Invoke(choiceToReport);
        }
        animating = false;
        animateCoroutine = null;
    }
}
