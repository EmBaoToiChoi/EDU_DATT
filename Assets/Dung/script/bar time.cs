using UnityEngine;
using UnityEngine.UI;

public class bartime : MonoBehaviour
{
    [Tooltip("Total duration of the timer in seconds.")]
    public float totalTime = 10f;

    [Tooltip("Current remaining time for the timer.")]
    public float currentTime = 10f;

    [Tooltip("If true, the timer will shrink using the Image fill amount instead of scale.")]
    public bool useFillAmount = true;

    private RectTransform rectTransform;
    private Image image;
    private Image[] childImages;
    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        image = GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
        }

        childImages = GetComponentsInChildren<Image>(true);

        if (image != null && useFillAmount)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 1f;
        }

        if (rectTransform != null)
        {
            originalScale = rectTransform.localScale;
        }
        ApplyProgress(1f);
    }

    public void SetDuration(float duration)
    {
        totalTime = Mathf.Max(0.01f, duration);
        currentTime = totalTime;
        ApplyProgress(1f);
    }

    public void SetTimeRemaining(float remainingTime)
    {
        currentTime = Mathf.Max(0f, remainingTime);
        float ratio = totalTime > 0f ? Mathf.Clamp01(currentTime / totalTime) : 0f;
        ApplyProgress(ratio);
    }

    public void ResetTimer(float duration)
    {
        SetDuration(duration);
    }

    private void ApplyProgress(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        if (useFillAmount)
        {
            if (image != null)
            {
                image.fillAmount = ratio;
            }

            if (childImages != null)
            {
                foreach (var childImage in childImages)
                {
                    if (childImage == image) continue;
                    if (childImage.type == Image.Type.Simple || childImage.type == Image.Type.Tiled || childImage.type == Image.Type.Sliced)
                    {
                        childImage.color = new Color(childImage.color.r, childImage.color.g, childImage.color.b, Mathf.Lerp(0.3f, 1f, ratio));
                    }
                }
            }
            return;
        }

        if (rectTransform != null)
        {
            Vector3 newScale = originalScale;
            newScale.x = Mathf.Max(0.01f, ratio) * originalScale.x;
            rectTransform.localScale = newScale;
        }
    }
}
