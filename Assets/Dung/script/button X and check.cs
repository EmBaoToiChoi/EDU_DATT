using UnityEngine;
using UnityEngine.EventSystems;

public class buttonXandcheck : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Buttons")]
    [Tooltip("Button X on the left side")]
    public RectTransform buttonX;

    [Tooltip("Button Check on the right side")]
    public RectTransform buttonCheck;

    [Header("Animation")]
    [Tooltip("Scale multiplier when dragging to the side")]
    public float scaleOnDrag = 1.35f;

    [Tooltip("Max tilt angle when dragging")]
    public float tiltAngle = 18f;

    [Tooltip("How much drag distance is needed to reach full effect")]
    public float dragSensitivity = 180f;

    private Vector2 startPointerPosition;
    private bool dragging;
    private Vector3 originalButtonXScale;
    private Vector3 originalButtonCheckScale;
    private Quaternion originalButtonXRotation;
    private Quaternion originalButtonCheckRotation;

    private void Awake()
    {
        CacheOriginalState();
    }

    private void CacheOriginalState()
    {
        if (buttonX != null)
        {
            originalButtonXScale = buttonX.localScale;
            originalButtonXRotation = buttonX.localRotation;
        }

        if (buttonCheck != null)
        {
            originalButtonCheckScale = buttonCheck.localScale;
            originalButtonCheckRotation = buttonCheck.localRotation;
        }
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
        float dragRatio = Mathf.Clamp(delta.x / dragSensitivity, -1f, 1f);
        UpdateButtons(dragRatio);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        ResetButtons();
    }

    private void UpdateButtons(float dragRatio)
    {
        float normalized = Mathf.Clamp01(Mathf.Abs(dragRatio));
        float targetScale = 1f + (scaleOnDrag - 1f) * normalized;
        float tilt = dragRatio * tiltAngle;

        if (buttonX != null)
        {
            float scale = dragRatio < 0f ? targetScale : 1f;
            buttonX.localScale = originalButtonXScale * scale;
            buttonX.localRotation = Quaternion.Euler(0f, 0f, dragRatio < 0f ? tilt : 0f);
        }

        if (buttonCheck != null)
        {
            float scale = dragRatio > 0f ? targetScale : 1f;
            buttonCheck.localScale = originalButtonCheckScale * scale;
            buttonCheck.localRotation = Quaternion.Euler(0f, 0f, dragRatio > 0f ? -tilt : 0f);
        }
    }

    private void ResetButtons()
    {
        if (buttonX != null)
        {
            buttonX.localScale = originalButtonXScale;
            buttonX.localRotation = originalButtonXRotation;
        }

        if (buttonCheck != null)
        {
            buttonCheck.localScale = originalButtonCheckScale;
            buttonCheck.localRotation = originalButtonCheckRotation;
        }
    }
}
