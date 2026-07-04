using UnityEngine;
using UnityEngine.EventSystems;

public class DragDropPiece : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Các Thành Phần Quản Lý (Để tính tọa độ chuẩn)")]
    public Canvas mainCanvas;         
    public RectTransform khungXep;    

    [Header("Ô Nhận Mục Tiêu")]
    public RectTransform targetSlot;  
    public float SnapDistance = 50f;  

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Transform originalParent; 
    private Vector3 startPosition;    
    private Vector3 startScale;       
    private Vector2 startSizeDelta;   
    private bool isLocked = false; 
    
    // ĐÂY RỒI: Biến kiểm tra xem người chơi có thực sự đang kéo hay không
    private bool isDragging = false; 

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        originalParent = transform.parent;
        startPosition = rectTransform.anchoredPosition; 
        startScale = rectTransform.localScale; 
        startSizeDelta = rectTransform.sizeDelta;

        if (mainCanvas == null) mainCanvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData) 
    {
        if (isLocked) return;
        transform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        
        isDragging = true; // Xác nhận: Người chơi ĐÃ BẮT ĐẦU KÉO mảnh ghép đi
        canvasGroup.alpha = 0.7f;       
        canvasGroup.blocksRaycasts = false; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.transform as RectTransform, 
            eventData.position, 
            mainCanvas.worldCamera, 
            out localPoint
        );
        
        rectTransform.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // TÍNH TOÁN KHOẢNG CÁCH CHUẨN (Dùng Anchored Position thay vì World Position để tránh lỗi Scale màn hình)
        // Ta cần tính khoảng cách dựa trên vị trí thực tế của TargetSlot quy đổi về tọa độ thế giới
        float distance = Vector2.Distance(rectTransform.position, targetSlot.position);

        // ĐIỀU KIỆN SỬA LỖI: Phải ĐANG KÉO (isDragging == true) và khoảng cách phải nhỏ hơn vùng hút
        if (isDragging && distance <= SnapDistance)
        {
            isLocked = true; 
            isDragging = false; // Reset trạng thái kéo

            transform.SetParent(targetSlot, false);

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;

            rectTransform.sizeDelta = targetSlot.sizeDelta;
            rectTransform.localScale = Vector3.one; 

            Debug.Log($"[Thành công] {gameObject.name} đã vào ô {targetSlot.name} vừa khít 100%!");
            
            QuizManager quizManager = FindObjectOfType<QuizManager>();
            if (quizManager != null)
            {
                quizManager.OnPiecePlacedCorrectly();
            }
        }
        else
        {
            // Nếu chỉ click (không kéo) hoặc kéo trượt -> Trả về vị trí cũ
            isDragging = false; // Reset trạng thái kéo
            
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = startPosition;
            rectTransform.sizeDelta = startSizeDelta;
            rectTransform.localScale = startScale;
            
            Debug.Log($"{gameObject.name} trả về vị trí cũ (Không kéo hoặc kéo trượt).");
        }
    }
}