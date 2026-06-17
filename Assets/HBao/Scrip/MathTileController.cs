using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class MathTileController : MonoBehaviour
{
    public int x;
    public int y;
    public int elementID; // ID để so khớp cặp (Question & Answer)

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtName; // Hiện chữ (nếu có)
    [SerializeField] private Image iconImage;         // Hiện hình ảnh riêng (nếu có)
    [SerializeField] private Image bgImage;           // Hình nền/viền của ô

    private MathBoardManager boardManager;
    private Button btn;

    public void CopyFromOldTile(TileController oldTile)
    {
        this.txtName = oldTile.GetTxtName();
        this.iconImage = oldTile.GetIconImage();
        this.bgImage = oldTile.GetBgImage();
    }

    public void SetupTile(int x, int y, int id, MathBoardManager manager)
    {
        this.x = x; this.y = y;
        this.elementID = id;
        this.boardManager = manager;

        btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnTileClicked);
        }
    }

    void OnTileClicked()
    {
        Debug.Log($"[MathTileController] Ô đáp án được click: vị trí ({x}, {y}), ID={elementID}");
        if (boardManager != null)
        {
            boardManager.SelectTile(this);
        }
        else
        {
            Debug.LogError("[MathTileController] boardManager bị null! Hãy chắc chắn SetupTile đã được gọi đúng cách.");
        }
    }

    public void SetSelectedVisual(bool isSelected)
    {
        // Nhấp nháy màu vàng xanh khi đang được chọn
        if (bgImage != null)
        {
            bgImage.color = isSelected ? new Color(0.8f, 1f, 0.8f) : Color.white;
        }
    }

    // --- HIỆU ỨNG RỤNG/THU NHỎ KHI NỐI ĐÚNG ---
    public void PlayMatchEffect()
    {
        // Khóa nút ngay lập tức để người chơi không bấm nhầm vào nữa
        if (btn != null) btn.interactable = false;
        // Bắt đầu hiệu ứng biến mất
        StartCoroutine(DisappearRoutine());
    }

    IEnumerator DisappearRoutine()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            transform.Rotate(0, 0, 15f * Time.deltaTime * 60f);
            yield return null;
        }

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;               // Tàng hình hoàn toàn
        cg.blocksRaycasts = false;   // Không cản chuột / cảm ứng
        cg.interactable = false;     // Không cho bấm nữa
    }
}
