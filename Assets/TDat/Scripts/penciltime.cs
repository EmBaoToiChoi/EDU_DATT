using UnityEngine;
using UnityEngine.UI;

public class PencilTimer : MonoBehaviour
{
    public RectTransform body;
    public RectTransform tip;
    public RectTransform stopTarget;

    public float maxTime = 10f;
    private float currentTime;
    private bool isStopped;

    private float originalWidth;
    private Vector2 tipOriginalAnchoredPos;
    private float tipOffsetFromBodyRight;

    void Start()
    {
        currentTime = maxTime;
        originalWidth = body.sizeDelta.x;
        tipOriginalAnchoredPos = tip.anchoredPosition;
        tipOffsetFromBodyRight = tipOriginalAnchoredPos.x - (body.anchoredPosition.x + originalWidth);
    }

    void Update()
    {
        if (isStopped)
            return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            float percent = currentTime / maxTime;

            // Giảm chiều dài thân
            body.sizeDelta = new Vector2(originalWidth * percent, body.sizeDelta.y);

            // Di chuyển đầu bút theo thân: giữ khoảng cách cố định tới mép phải thân
            float tipX = body.anchoredPosition.x + body.sizeDelta.x + tipOffsetFromBodyRight;
            tip.anchoredPosition = new Vector2(tipX, tipOriginalAnchoredPos.y);

            if (stopTarget != null && IsOverlapping(tip, stopTarget))
            {
                isStopped = true;
            }
        }
    }

    private bool IsOverlapping(RectTransform a, RectTransform b)
    {
        Rect rectA = GetWorldRect(a);
        Rect rectB = GetWorldRect(b);
        return rectA.Overlaps(rectB);
    }

    private Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 min = corners[0];
        Vector3 max = corners[0];
        for (int i = 1; i < corners.Length; i++)
        {
            min = Vector3.Min(min, corners[i]);
            max = Vector3.Max(max, corners[i]);
        }
        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }
}