using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler 
{
    public MainGameManager manager;
    [Header("Piece Settings")]
    public int pieceID; 

    [HideInInspector] 
    public RectTransform targetSlot; 
    
    public bool isUnlocked = false;
    private bool isSnapped = false; // Biến kiểm tra mảnh đã được lắp trúng ô chưa
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 startWorldPos; 

    void Awake() {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start() {
        startWorldPos = transform.position;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (!isUnlocked) manager.ShowQuiz(this);
    }

    public void Unlock() { 
        isUnlocked = true; 
        GetComponent<Image>().color = Color.white; 
    }

    // Hàm tạo cổng truy cập để GameManager kiểm tra trạng thái lắp ráp
    public bool IsSnappedPiece() {
        return isSnapped;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (!isUnlocked || isSnapped) return;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling(); 
    }

    public void OnDrag(PointerEventData eventData) {
        if (!isUnlocked || isSnapped) return;
        rectTransform.anchoredPosition += eventData.delta; 
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (!isUnlocked || isSnapped || targetSlot == null) return; 

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        float screenDistance = Vector3.Distance(transform.position, targetSlot.position);

        if (screenDistance < 70f) {
            transform.position = targetSlot.position; 
            isSnapped = true;
            GetComponent<Image>().color = Color.green; 

            // CẬP NHẬT MỚI: Báo cho GameManager biết mảnh này đã ráp xong để check điều kiện Win màn
            if (manager != null) {
                manager.CheckGameComplete();
            }
        } else {
            transform.position = startWorldPos; 
        }
    }
}