using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.15f;
    [SerializeField] private bool enableWiggle = true;
    [SerializeField] private float wiggleAngle = 4f;       // Góc lắc lư nhẹ (độ)
    [SerializeField] private float wiggleSpeed = 20f;      // Tốc độ lắc lư nhanh hay chậm

    [Header("Click Settings")]
    [SerializeField] private float clickScale = 0.9f;

    private Vector3 originalScale = Vector3.one;
    private Coroutine scaleCoroutine;
    private Coroutine wiggleCoroutine;
    private bool isHovered = false;

    void Awake()
    {
        // Lưu kích thước gốc ban đầu của nút
        originalScale = transform.localScale;
        if (originalScale == Vector3.zero)
        {
            originalScale = Vector3.one;
        }
    }

    void OnDisable()
    {
        // Trả nút về trạng thái bình thường nếu Panel bị ẩn đột ngột
        ResetButtonState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        
        // Phóng to nhẹ khi rê chuột vào
        StartScaleTransition(originalScale * hoverScale);
        
        // Bắt đầu hiệu ứng lắc lư (rung)
        if (enableWiggle)
        {
            if (wiggleCoroutine != null) StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = StartCoroutine(WiggleRoutine());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        
        // Thu nhỏ về lại kích thước ban đầu
        StartScaleTransition(originalScale);
        
        // Dừng lắc lư
        if (wiggleCoroutine != null)
        {
            StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = null;
        }
        transform.localRotation = Quaternion.identity;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Thu nhỏ lại một chút khi ấn giữ chuột để tạo cảm giác nhấn nút vật lý
        StartScaleTransition(originalScale * clickScale);

        // Phát âm thanh click chuột thông qua AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSound();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Khi thả chuột ra
        Vector3 target = isHovered ? (originalScale * hoverScale) : originalScale;
        StartScaleTransition(target);
    }

    private void StartScaleTransition(Vector3 targetScale)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(targetScale));
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / scaleDuration);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private IEnumerator WiggleRoutine()
    {
        while (isHovered)
        {
            // Sử dụng hàm Sin xoay đều để tạo chuyển động rung lắc liên tục
            float angle = Mathf.Sin(Time.time * wiggleSpeed) * wiggleAngle;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        transform.localRotation = Quaternion.identity;
    }

    private void ResetButtonState()
    {
        isHovered = false;
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        if (wiggleCoroutine != null) StopCoroutine(wiggleCoroutine);
        
        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity;
    }
}
