using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct OriginalCardData
{
    public RectTransform rectTransform;
    public Vector2 originalPosition;
    public Vector2 originalSizeDelta;
}

public class CardSelectionManager : MonoBehaviour
{
    [Header("Danh sách 8 lá bài (Kéo thả từ Inspector)")]
    public List<RectTransform> allCards;
    
    [Header("Cấu hình khi phóng to (Focus)")]
    public Vector2 focusSize = new Vector2(800, 500); 
    public Vector2 focusPos = new Vector2(-200, -10);   

    [Header("Cấu hình 7 lá dạt sang phải")]
    public Vector2 otherSmallSize = new Vector2(120, 168); 
    
    [Header("Thời gian di chuyển (giây)")]
    public float duration = 0.35f; 

    private List<OriginalCardData> originalCardDataList = new List<OriginalCardData>();
    private Dictionary<RectTransform, Coroutine> runningCoroutines = new Dictionary<RectTransform, Coroutine>();

    private void Awake()
    {
        SaveOriginalCardsData();
    }

    private void SaveOriginalCardsData()
    {
        originalCardDataList.Clear();
        foreach (RectTransform card in allCards)
        {
            if (card != null)
            {
                OriginalCardData data = new OriginalCardData();
                data.rectTransform = card;
                data.originalPosition = card.anchoredPosition; 
                data.originalSizeDelta = card.sizeDelta;       
                
                // Ép Scale về 1 để tránh lỗi bóp méo ma trận tọa độ do (Scale Y = 6) gây ra
                card.localScale = Vector3.one; 

                originalCardDataList.Add(data);
            }
        }
    }

    // HÀM CHÍNH: Sẽ được gọi trực tiếp từ sự kiện OnClick của Button
    public void OnCardClick(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= allCards.Count) return;

        RectTransform selected = allCards[cardIndex];
        selected.SetAsLastSibling(); // Đưa lá bài được chọn lên trên cùng lớp hiển thị

        // 1. Cho lá bài được click bay sang trái và phóng to
        TriggerAnimation(selected, focusPos, focusSize);

        // 2. Thu nhỏ và dạt 7 lá còn lại sang bên phải (Xếp thành cụm 2 cột)
        int otherCount = 0;
        for (int i = 0; i < allCards.Count; i++)
        {
            if (i == cardIndex) continue;

            int r = otherCount / 2;
            int c = otherCount % 2;
            
            float x = 400 + (c * (otherSmallSize.x + 20)); 
            float y = 210 - (r * (otherSmallSize.y + 20));

            TriggerAnimation(allCards[i], new Vector2(x, y), otherSmallSize);
            otherCount++;
        }
    }

    private void TriggerAnimation(RectTransform card, Vector2 targetPos, Vector2 targetSize)
    {
        if (runningCoroutines.ContainsKey(card) && runningCoroutines[card] != null)
        {
            StopCoroutine(runningCoroutines[card]);
        }
        runningCoroutines[card] = StartCoroutine(AnimateCardUI(card, targetPos, targetSize));
    }

    private IEnumerator AnimateCardUI(RectTransform card, Vector2 targetPos, Vector2 targetSize)
    {
        Vector2 startPos = card.anchoredPosition;
        Vector2 startSize = card.sizeDelta;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timeElapsed / duration);

            card.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            card.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
            yield return null;
        }

        card.anchoredPosition = targetPos;
        card.sizeDelta = targetSize;
    }
}