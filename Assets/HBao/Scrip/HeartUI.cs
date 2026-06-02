using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HeartUI : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image heartImage;
    private bool isFallen = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        heartImage = GetComponent<Image>();
    }

    public void PlayFallEffect()
    {
        if (isFallen) return;
        isFallen = true;

        StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        float duration = 0.8f;
        float elapsed = 0f;

        Vector2 startPosition = rectTransform.anchoredPosition;
        float startRotation = 0f;
        float targetRotation = Random.Range(-45f, 45f); // Xoay ngẫu nhiên một góc nhỏ khi rơi

        Color startColor = heartImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Công thức rơi tự do mô phỏng trọng lực (t * t)
            float yOffset = -600f * (t * t); 
            rectTransform.anchoredPosition = startPosition + new Vector2(0f, yOffset);

            // Xoay nhẹ
            float rot = Mathf.Lerp(startRotation, targetRotation, t);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);

            // Mờ dần
            heartImage.color = Color.Lerp(startColor, targetColor, t);

            yield return null;
        }

        // Ẩn hẳn trái tim sau khi rơi xong
        gameObject.SetActive(false);
    }

    // Reset lại trạng thái tim ban đầu khi chơi lại game
    public void ResetHeart(Color originalColor, Vector2 startPosition)
    {
        isFallen = false;
        gameObject.SetActive(true);
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (heartImage == null) heartImage = GetComponent<Image>();

        rectTransform.anchoredPosition = startPosition;
        rectTransform.localRotation = Quaternion.identity;
        heartImage.color = originalColor;
    }
}
