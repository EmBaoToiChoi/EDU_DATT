using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler 
{
    public MainGameManager manager;
    public RectTransform targetSlot; // Kéo thả các ô Slot tương ứng vào đây
    
    public bool isUnlocked = false;
    private bool isSnapped = false;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 startWorldPos; // Lưu vị trí thế giới lúc đầu

    void Awake() {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start() {
        // Lưu lại vị trí ban đầu chuẩn xác khi bắt đầu game
        startWorldPos = transform.position;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (!isUnlocked) manager.ShowQuiz(this);
    }

    public void Unlock() { 
        isUnlocked = true; 
        GetComponent<Image>().color = Color.white; // Biến đổi từ xám sang trắng khi giải đúng tiếng Anh
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (!isUnlocked || isSnapped) return;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData) {
        if (!isUnlocked || isSnapped) return;
        // Mảnh ghép chạy mượt theo đúng vị trí con trỏ chuột trên màn hình
        transform.position = eventData.position; 
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (!isUnlocked || isSnapped) return;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Tính khoảng cách vật lý thực tế giữa Mảnh ghép và Ô đích trên màn hình
        float screenDistance = Vector3.Distance(transform.position, targetSlot.position);

        // Nếu khoảng cách nhỏ hơn một khoảng vừa phải (gần chạm trúng)
        if (screenDistance < 70f) {
            transform.position = targetSlot.position; // Tự động hút chặt vào tâm ô đích
            isSnapped = true;
            GetComponent<Image>().color = Color.green; // Đổi sang màu xanh lá báo hiệu hoàn thành
        } else {
            transform.position = startWorldPos; // Thả trượt sẽ tự động bay về vị trí ban đầu bên trái
        }
    }
}