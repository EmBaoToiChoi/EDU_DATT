using System;
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

    private RectTransform rectTransform;
    private Vector2 startPointerPosition;
    private Vector3 originalPosition;
    private bool dragging;

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
            int choice = horizontal > 0 ? 1 : 0;
            onChoice?.Invoke(choice);
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
            if (statusText != null)
            {
                statusText.text = "Kéo thẻ để trả lời";
            }
        }
    }
}
