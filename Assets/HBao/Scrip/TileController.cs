using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections; // Cần thiết để dùng Coroutine

public class TileController : MonoBehaviour
{
    public int x;
    public int y;
    public int elementID;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtName; // Hiện chữ (có thể bỏ trống nếu chỉ dùng ảnh)
    [SerializeField] private Image iconImage;         // Hiện hình ảnh riêng của chất đó
    [SerializeField] private Image bgImage;           // Hình nền/viền của ô

    public TextMeshProUGUI GetTxtName() => txtName;
    public Image GetIconImage() => iconImage;
    public Image GetBgImage() => bgImage;

    private BoardManager boardManager;
    private Button btn;

    public void SetupTile(int x, int y, int id, BoardManager manager)
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

        if (txtName != null)
        {
            if (!string.IsNullOrEmpty(txtName.text) && txtName.text.Contains("(H2O) là nước"))
            {
                txtName.text = "(H2O)\nlà nước";
            }

            txtName.transform.localScale = Vector3.one;
            txtName.margin = Vector4.zero;

            RectTransform rect = txtName.rectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.30f, 0.15f);
                rect.anchorMax = new Vector2(0.88f, 0.85f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            txtName.enableAutoSizing = true;
            txtName.fontSizeMin = 14f;
            txtName.fontSizeMax = 24f;
            txtName.fontStyle = FontStyles.Bold;
            txtName.alignment = TextAlignmentOptions.Center;
            txtName.raycastTarget = false;
        }
    }

    void OnTileClicked()
    {
        boardManager.SelectTile(this);
    }

    public void SetSelectedVisual(bool isSelected)
    {
        // Nhấp nháy màu vàng xanh khi đang được chọn
        bgImage.color = isSelected ? new Color(0.8f, 1f, 0.8f) : Color.white;
    }

    // --- HIỆU ỨNG RỤNG/THU NHỎ KHI NỐI ĐÚNG ---
    public void PlayMatchEffect()
    {
        // Khóa nút ngay lập tức để người chơi không bấm nhầm vào nữa
        btn.interactable = false;
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

        // BỎ DÒNG NÀY: gameObject.SetActive(false);
        // THAY BẰNG ĐOẠN CODE TÀNG HÌNH DƯỚI ĐÂY:

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;               // Tàng hình hoàn toàn
        cg.blocksRaycasts = false;   // Không cản chuột / cảm ứng
        cg.interactable = false;     // Không cho bấm nữa
    }
}