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

    private BoardManager boardManager;
    private Button btn;

    public void SetupTile(int x, int y, int id, ElementData data, BoardManager manager)
    {
        this.x = x; this.y = y;
        this.elementID = id;
        this.boardManager = manager;

        // Gán Tên và Hình ảnh từ Database
        if (txtName != null) txtName.text = data.elementName;
        if (iconImage != null && data.elementSprite != null) iconImage.sprite = data.elementSprite;

        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnTileClicked);
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
        float duration = 0.3f; // Thời gian hiệu ứng (0.3 giây)
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Thu nhỏ dần về 0
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            // Xoay vòng vòng
            transform.Rotate(0, 0, 15f * Time.deltaTime * 60f);
            
            yield return null;
        }

        // Xong hiệu ứng thì ẩn hoàn toàn
        gameObject.SetActive(false);
    }
}