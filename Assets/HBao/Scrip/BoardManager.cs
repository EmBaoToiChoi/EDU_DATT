using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nhập thư viện TMPro để quản lý hiển thị chữ điểm số

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int rows = 6;
    public int cols = 10;

    [Header("Element Database")]
    [Tooltip("Danh sách các Prefab chất hóa học tự thiết kế (Cu, Zn, Br, Al...)")]
    [SerializeField] private GameObject[] elementPrefabs; 

    [Header("References")]
    public Transform boardParent;
    public Slider expSlider; // Thanh Kinh Nghiệm

    [Header("Hearts Settings")]
    [SerializeField] private HeartUI[] heartIcons;       // Mảng 3 trái tim UI
    [SerializeField] private GameObject gameOverPanel;   // Panel khi thua cuộc
    [SerializeField] private Button replayButton;        // Nút chơi lại trên Panel Thua (mới)
    [SerializeField] private Button exitButton;          // Nút thoát trên Panel Thua (mới)
    [SerializeField] private GameObject lobbyMenuUI;     // Menu Lobby (để quay lại nếu nhấn thoát trong game)
    [SerializeField] private GameObject winPanel;        // Panel khi chiến thắng (mới)
    [SerializeField] private Button winReplayButton;     // Nút chơi lại trên Panel Thắng (mới)
    [SerializeField] private Button winExitButton;       // Nút thoát trên Panel Thắng (mới)

    [Header("Gameplay Audio Clips")]
    [SerializeField] private AudioClip matchSound;       // Âm thanh khi nối đúng
    [SerializeField] private AudioClip errorSound;       // Âm thanh khi chọn sai / mất mạng
    [SerializeField] private AudioClip gameOverSound;    // Âm thanh khi thua cuộc
    [SerializeField] private AudioClip winSound;         // Âm thanh khi chiến thắng (mới)

    [Header("Score Settings")]
    [SerializeField] private TextMeshProUGUI gameplayScoreText; // Text điểm hiển thị trong màn chơi
    [SerializeField] private TextMeshProUGUI winScoreText;      // Text điểm hiển thị trên Panel Thắng
    [SerializeField] private TextMeshProUGUI loseScoreText;     // Text điểm hiển thị trên Panel Thua

    [Header("Timer Settings")]
    [SerializeField] private float totalTime = 180f;             // Thời gian giới hạn chơi (giây, mặc định 90s = 1p30s)
    private float remainingTime;                                // Thời gian còn lại

    [Header("Combo Settings")]
    [SerializeField] private float comboThreshold = 3.0f;       // Zeit tối đa giữa 2 lần ghép để tính combo (giây)
    [SerializeField] private Image comboImage;                  // Image UI hiển thị hình ảnh Combo
    [SerializeField] private Sprite comboSprite;                // Hình ảnh Combo x2 duy nhất
    [SerializeField] private AudioClip comboSound;              // Âm thanh hiệu ứng Combo duy nhất

    private int[,] matrix;
    private TileController firstSelected = null;
    private int currentExp = 0;
    private int maxExp = 0;
    private bool isGameOver = false;

    private int currentScore = 0; // Điểm số hiện tại của màn chơi
    private float lastMatchTime = -100f; // Thời điểm ghép đúng trước đó
    private int currentCombo = 0; // Cấp combo hiện tại (0 -> base, 1 -> combo x2, 2 -> combo x3)
    private Coroutine comboCoroutine;
    private Vector3 comboOriginalScale = Vector3.one; // Lưu tỷ lệ RectTransform gốc từ Editor (mới)

    private int currentHearts = 3;
    private Vector2[] heartStartPositions;
    private Color heartOriginalColor;

    void Start()
    {
        Application.targetFrameRate = 60;
        matrix = new int[rows + 2, cols + 2];

        GenerateBoardLogic();

        // 1 Cặp nối đúng = 1 EXP. Max EXP = Tống số ô chia 2.
        maxExp = (rows * cols) / 2;
        if (expSlider != null)
        {
            expSlider.minValue = 0;
            expSlider.maxValue = 100f;
            expSlider.value = 100f;
        }
        remainingTime = totalTime;

        // Lưu vị trí và màu sắc ban đầu của các trái tim để reset khi chơi lại
        currentHearts = 3;
        if (heartIcons != null && heartIcons.Length > 0)
        {
            heartStartPositions = new Vector2[heartIcons.Length];
            if (heartIcons[0] != null)
            {
                Image heartImg = heartIcons[0].GetComponent<Image>();
                if (heartImg != null) heartOriginalColor = heartImg.color;
            }
            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] != null)
                {
                    heartStartPositions[i] = heartIcons[i].GetComponent<RectTransform>().anchoredPosition;
                    heartIcons[i].gameObject.SetActive(false); // Ẩn hình ảnh trái tim
                }
            }
        }

        // Đăng ký sự kiện click cho các nút trên Panel Thua cuộc
        if (replayButton != null) replayButton.onClick.AddListener(ResetGame);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

        // Đăng ký sự kiện click cho các nút trên Panel Chiến thắng
        if (winReplayButton != null) winReplayButton.onClick.AddListener(ResetGame);
        if (winExitButton != null) winExitButton.onClick.AddListener(OnExitClicked);

        // Khởi tạo điểm ban đầu
        currentScore = 0;
        UpdateScoreUI();

        // Lưu tỷ lệ (Scale) gốc của Combo Image được thiết lập từ Editor
        if (comboImage != null)
        {
            comboOriginalScale = comboImage.transform.localScale;
        }
    }

    void Update()
    {
        if (isGameOver) return;

        // Chỉ đếm ngược thời gian khi đang trong màn chơi chính (chờ game bắt đầu và chưa Game Over)
        if (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            
            if (expSlider != null)
            {
                expSlider.value = (remainingTime / totalTime) * 100f;
            }

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                if (expSlider != null) expSlider.value = 0f;
                TriggerGameOver(false); // Thua cuộc do hết thời gian
            }
        }
    }

    void GenerateBoardLogic()
    {
        int totalPlayableTiles = rows * cols;
        List<int> elementIDs = new List<int>();

        int numPairs = totalPlayableTiles / 2;
        for (int i = 0; i < numPairs; i++)
        {
            // Lấy ngẫu nhiên ID từ danh sách Prefab bạn nạp vào
            int randomID = Random.Range(0, elementPrefabs.Length);
            elementIDs.Add(randomID);
            elementIDs.Add(randomID);
        }

        // Xáo trộn (Shuffle)
        for (int i = elementIDs.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            int temp = elementIDs[i];
            elementIDs[i] = elementIDs[r];
            elementIDs[r] = temp;
        }

        // Lấy các cài đặt từ GridLayoutGroup để dựng lưới
        GridLayoutGroup gridLayout = boardParent.GetComponent<GridLayoutGroup>();
        Vector2 cellSize = new Vector2(100f, 100f);
        Vector2 spacing = Vector2.zero;
        RectOffset padding = null;

        if (gridLayout != null)
        {
            cellSize = gridLayout.cellSize;
            spacing = gridLayout.spacing;
            padding = gridLayout.padding;
            gridLayout.enabled = false; // Tắt layout group tự động để không ghi đè kích thước/tỉ lệ của Prefab
        }

        // Sử dụng trực tiếp cellSize từ GridLayoutGroup để đồng bộ kích thước tất cả các ô chất


        // Tính toán tổng kích thước lưới chưa co giãn để căn chỉnh
        float totalWidth = cols * cellSize.x + (cols - 1) * spacing.x;
        float totalHeight = rows * cellSize.y + (rows - 1) * spacing.y;

        float gridScale = 1f;
        float startX = 0f;
        float startY = 0f;

        RectTransform parentRect = boardParent as RectTransform;
        if (parentRect != null && parentRect.rect.width > 0 && parentRect.rect.height > 0)
        {
            float parentWidth = parentRect.rect.width;
            float parentHeight = parentRect.rect.height;

            float padLeft = padding != null ? padding.left : 0f;
            float padRight = padding != null ? padding.right : 0f;
            float padTop = padding != null ? padding.top : 0f;
            float padBottom = padding != null ? padding.bottom : 0f;

            float netParentWidth = parentWidth - padLeft - padRight;
            float netParentHeight = parentHeight - padTop - padBottom;

            // Đảm bảo không âm hoặc lỗi chia cho 0
            if (netParentWidth <= 0f) netParentWidth = parentWidth;
            if (netParentHeight <= 0f) netParentHeight = parentHeight;

            // Tính tỉ lệ thu nhỏ tự động nếu lưới bị tràn ra ngoài vùng chứa
            if (totalWidth > netParentWidth || totalHeight > netParentHeight)
            {
                float scaleX = netParentWidth / totalWidth;
                float scaleY = netParentHeight / totalHeight;
                gridScale = Mathf.Min(scaleX, scaleY);
            }

            // Áp dụng tỉ lệ co giãn vào các kích thước của lưới
            float scaledCellWidth = cellSize.x * gridScale;
            float scaledCellHeight = cellSize.y * gridScale;
            float scaledSpacingX = spacing.x * gridScale;
            float scaledSpacingY = spacing.y * gridScale;

            float scaledTotalWidth = cols * scaledCellWidth + (cols - 1) * scaledSpacingX;
            float scaledTotalHeight = rows * scaledCellHeight + (rows - 1) * scaledSpacingY;

            // Xác định vị trí bắt đầu dựa trên childAlignment của Grid ban đầu
            TextAnchor alignment = gridLayout != null ? gridLayout.childAlignment : TextAnchor.MiddleCenter;

            // Tính startX
            if (alignment == TextAnchor.UpperLeft || alignment == TextAnchor.MiddleLeft || alignment == TextAnchor.LowerLeft)
            {
                startX = -parentWidth / 2f + padLeft + scaledCellWidth / 2f;
            }
            else if (alignment == TextAnchor.UpperRight || alignment == TextAnchor.MiddleRight || alignment == TextAnchor.LowerRight)
            {
                startX = parentWidth / 2f - padRight - scaledTotalWidth + scaledCellWidth / 2f;
            }
            else // MiddleCenter, UpperCenter, LowerCenter
            {
                startX = -parentWidth / 2f + padLeft + (netParentWidth - scaledTotalWidth) / 2f + scaledCellWidth / 2f;
            }

            // Tính startY
            if (alignment == TextAnchor.UpperLeft || alignment == TextAnchor.UpperCenter || alignment == TextAnchor.UpperRight)
            {
                startY = parentHeight / 2f - padTop - scaledCellHeight / 2f;
            }
            else if (alignment == TextAnchor.LowerLeft || alignment == TextAnchor.LowerCenter || alignment == TextAnchor.LowerRight)
            {
                startY = -parentHeight / 2f + padBottom + scaledTotalHeight - scaledCellHeight / 2f;
            }
            else // MiddleLeft, MiddleCenter, MiddleRight
            {
                startY = parentHeight / 2f - padTop - (netParentHeight - scaledTotalHeight) / 2f - scaledCellHeight / 2f;
            }
        }
        else
        {
            // Dự phòng nếu không có RectTransform hoặc kích thước chưa được khởi tạo
            startX = -totalWidth / 2f + cellSize.x / 2f;
            startY = totalHeight / 2f - cellSize.y / 2f;
        }

        // Cập nhật lại các khoảng cách và cell size theo tỉ lệ gridScale
        float finalCellWidth = cellSize.x * gridScale;
        float finalCellHeight = cellSize.y * gridScale;
        float finalSpacingX = spacing.x * gridScale;
        float finalSpacingY = spacing.y * gridScale;

        int index = 0;
        for (int r = 1; r <= rows; r++)
        {
            for (int c = 1; c <= cols; c++)
            {
                int id = elementIDs[index];
                matrix[r, c] = id + 1; // +1 để phân biệt với 0 (ô trống)

                // Tạo ra prefab tương ứng với ID chất đó
                GameObject go = Instantiate(elementPrefabs[id], boardParent);
                
                // Đảm bảo giữ nguyên kích thước (sizeDelta) và áp dụng tỉ lệ thu nhỏ của lưới lên scale
                RectTransform goRect = go.GetComponent<RectTransform>();
                RectTransform prefabRect = elementPrefabs[id].GetComponent<RectTransform>();
                if (goRect != null && prefabRect != null)
                {
                    Vector2 originalPivot = prefabRect.pivot;

                    // Đặt neo ở chính giữa để tính toán vị trí dễ dàng
                    goRect.anchorMin = new Vector2(0.5f, 0.5f);
                    goRect.anchorMax = new Vector2(0.5f, 0.5f);
                    goRect.pivot = originalPivot;
                    
                    // Đồng bộ hóa kích thước của tất cả các ô theo kích thước ô lưới (cellSize)
                    goRect.sizeDelta = cellSize;
                    
                    Vector3 targetScale = new Vector3(gridScale, gridScale, 1f);
                    goRect.localScale = targetScale;

                    // Tính vị trí tâm ô (r, c)
                    float posX = startX + (c - 1) * (finalCellWidth + finalSpacingX);
                    float posY = startY - (r - 1) * (finalCellHeight + finalSpacingY);

                    // Điều chỉnh vị trí theo Pivot của prefab (sử dụng targetScale mới để không bị lệch)
                    float offsetX = (originalPivot.x - 0.5f) * cellSize.x * targetScale.x;
                    float offsetY = (originalPivot.y - 0.5f) * cellSize.y * targetScale.y;

                    goRect.anchoredPosition = new Vector2(posX + offsetX, posY + offsetY);
                }

                TileController tile = go.GetComponent<TileController>();
                tile.SetupTile(r, c, id + 1, this);

                index++;
            }
        }
    }

    public void SelectTile(TileController clickedTile)
    {
        if (isGameOver) return;

        if (firstSelected == clickedTile)
        {
            firstSelected.SetSelectedVisual(false);
            firstSelected = null;
            return;
        }

        if (firstSelected == null)
        {
            firstSelected = clickedTile;
            firstSelected.SetSelectedVisual(true);
        }
        else
        {
            bool isMatch = false;

            if (firstSelected.elementID == clickedTile.elementID)
            {
                Vector2Int p1 = new Vector2Int(firstSelected.x, firstSelected.y);
                Vector2Int p2 = new Vector2Int(clickedTile.x, clickedTile.y);

                // Dù bị kẹt hay bị vây quanh vẫn kết nối và khớp được (Không dùng thuật toán tìm đường đi)
                isMatch = true;
                matrix[p1.x, p1.y] = 0;
                matrix[p2.x, p2.y] = 0;

                // Gọi hiệu ứng thay vì ẩn đi lập tức
                firstSelected.PlayMatchEffect();
                clickedTile.PlayMatchEffect();

                // Phát âm thanh ghép đúng
                if (AudioManager.Instance != null && matchSound != null)
                {
                    AudioManager.Instance.PlaySFX(matchSound);
                }

                // Tăng cấp độ
                GainExp();
            }

            if (!isMatch)
            {
                // Chọn sai hoặc không tìm được đường đi nối 2 ô -> Trừ tim
                firstSelected.SetSelectedVisual(false);
                DeductHeart();
            }

            firstSelected = null;
        }
    }

    private void GainExp()
    {
        currentExp++;

        // Tính toán combo dựa trên khoảng thời gian với lần ghép đúng trước đó
        float timeSinceLastMatch = Time.time - lastMatchTime;
        int pointsAdded = 1;

        if (timeSinceLastMatch <= comboThreshold)
        {
            // Ghép nhanh -> Đạt combo, cộng 2 điểm (+2 điểm là cao nhất)
            pointsAdded = 2;
        }
        else
        {
            // Quá thời gian -> Nhận 1 điểm cơ bản
            pointsAdded = 1;
        }
        lastMatchTime = Time.time;

        currentScore += pointsAdded;

        UpdateScoreUI();
        UpdateComboUI(pointsAdded);

        if (currentExp >= maxExp)
        {
            TriggerGameOver(true);
        }
    }

    // --- THUẬT TOÁN TÌM ĐƯỜNG KIỂU MỚI (CHỐNG KẸT LỖI 100%) ---
    struct Node
    {
        public int x, y, segments; // Đếm số đoạn thẳng (tối đa 3 đoạn = 2 lần rẽ)
        public Node(int x, int y, int segments) { this.x = x; this.y = y; this.segments = segments; }
    }

    // --- THUẬT TOÁN TÌM ĐƯỜNG PIKACHU CHUẨN (100% KHÔNG LỖI) ---

    // Đổi tên hàm gọi ở phần Check Logic trong SelectTile thành hàm này
    private bool CheckPath(Vector2Int p1, Vector2Int p2)
    {
        if (CheckLine(p1, p2)) return true;       // Nối đường thẳng
        if (CheckRect(p1, p2)) return true;       // Nối chữ L (1 lần rẽ)
        if (CheckMoreLine(p1, p2)) return true;   // Nối chữ U, Z (2 lần rẽ)
        return false;
    }

    // 1. Kiểm tra đường thẳng
    private bool CheckLine(Vector2Int p1, Vector2Int p2)
    {
        if (p1.x == p2.x) // Cùng hàng
        {
            int y1 = Mathf.Min(p1.y, p2.y);
            int y2 = Mathf.Max(p1.y, p2.y);
            for (int y = y1 + 1; y < y2; y++)
                if (matrix[p1.x, y] > 0) return false; // Vướng vật cản
            return true;
        }
        if (p1.y == p2.y) // Cùng cột
        {
            int x1 = Mathf.Min(p1.x, p2.x);
            int x2 = Mathf.Max(p1.x, p2.x);
            for (int x = x1 + 1; x < x2; x++)
                if (matrix[x, p1.y] > 0) return false; // Vướng vật cản
            return true;
        }
        return false;
    }

    // 2. Kiểm tra chữ L (1 lần rẽ)
    private bool CheckRect(Vector2Int p1, Vector2Int p2)
    {
        Vector2Int p3 = new Vector2Int(p1.x, p2.y); // Góc vuông 1
        if (matrix[p3.x, p3.y] == 0 && CheckLine(p1, p3) && CheckLine(p2, p3)) return true;

        Vector2Int p4 = new Vector2Int(p2.x, p1.y); // Góc vuông 2
        if (matrix[p4.x, p4.y] == 0 && CheckLine(p1, p4) && CheckLine(p2, p4)) return true;

        return false;
    }

    // 3. Kiểm tra chữ U, Z (2 lần rẽ)
    private bool CheckMoreLine(Vector2Int p1, Vector2Int p2)
    {
        // Quét dọc theo tất cả các cột
        for (int y = 0; y < cols + 2; y++)
        {
            Vector2Int p3 = new Vector2Int(p1.x, y);
            Vector2Int p4 = new Vector2Int(p2.x, y);
            if (matrix[p3.x, p3.y] == 0 && matrix[p4.x, p4.y] == 0)
            {
                if (CheckLine(p1, p3) && CheckLine(p3, p4) && CheckLine(p4, p2)) return true;
            }
        }

        // Quét ngang theo tất cả các hàng
        for (int x = 0; x < rows + 2; x++)
        {
            Vector2Int p3 = new Vector2Int(x, p1.y);
            Vector2Int p4 = new Vector2Int(x, p2.y);
            if (matrix[p3.x, p3.y] == 0 && matrix[p4.x, p4.y] == 0)
            {
                if (CheckLine(p1, p3) && CheckLine(p3, p4) && CheckLine(p4, p2)) return true;
            }
        }
        return false;
    }

    private void DeductHeart()
    {
        if (isGameOver) return;

        // Chỉ phát âm thanh báo chọn sai, không trừ tim và không game over
        if (AudioManager.Instance != null && errorSound != null)
        {
            AudioManager.Instance.PlaySFX(errorSound);
        }
    }

    private void TriggerGameOver(bool isWin)
    {
        isGameOver = true;

        if (isWin)
        {
            Debug.Log("LEVEL UP! Chúc mừng hoàn thành bài học!");
            if (winPanel != null)
            {
                winPanel.SetActive(true);
                // Hiệu ứng hiện các nút của Panel Thắng tuần tự
                StartCoroutine(AnimatePanelButtons(new Button[] { winReplayButton, winExitButton }));
            }
            if (AudioManager.Instance != null && winSound != null)
            {
                AudioManager.Instance.PlaySFX(winSound);
            }
        }
        else
        {
            Debug.Log("GAME OVER! Bạn đã hết mạng chơi.");
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                // Hiệu ứng hiện các nút của Panel Thua tuần tự
                StartCoroutine(AnimatePanelButtons(new Button[] { replayButton, exitButton }));
            }
            if (AudioManager.Instance != null && gameOverSound != null)
            {
                AudioManager.Instance.PlaySFX(gameOverSound);
            }
        }
    }

    // Reset lại game (gọi khi bấm chơi lại)
    public void ResetGame()
    {
        // 1. Hủy toàn bộ các ô cũ trên bàn chơi
        if (boardParent != null)
        {
            foreach (Transform child in boardParent)
            {
                Destroy(child.gameObject);
            }
        }

        // 2. Tạo ma trận mới và sinh bảng chơi mới
        matrix = new int[rows + 2, cols + 2];
        GenerateBoardLogic();

        // 3. Khôi phục các trạng thái ban đầu
        currentHearts = 3;
        isGameOver = false;
        currentExp = 0;
        currentScore = 0; // Reset điểm về 0
        currentCombo = 0; // Reset combo về 0
        lastMatchTime = -100f;
        firstSelected = null;
        remainingTime = totalTime;
        if (expSlider != null) expSlider.value = 100f;
        UpdateScoreUI();

        if (comboImage != null)
        {
            comboImage.gameObject.SetActive(false); // Ẩn hình combo
        }

        // 4. Reset hiển thị 3 quả tim
        if (heartIcons != null)
        {
            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] != null)
                {
                    heartIcons[i].ResetHeart(heartOriginalColor, heartStartPositions[i]);
                }
            }
        }

        // 5. Ẩn Panel Thua & Thắng
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
    }

    private void OnExitClicked()
    {
        // Nếu có gán lobbyMenuUI thì quay về Lobby Menu, ngược lại sẽ thoát ứng dụng
        if (lobbyMenuUI != null)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            gameObject.SetActive(false); // Ẩn Canvas chơi game hiện tại (nếu script nằm trên Canvas này)
            lobbyMenuUI.SetActive(true); // Hiện Menu Lobby
        }
        else
        {
            Debug.Log("BoardManager: Thoát game!");
            Application.Quit();
        }
    }

    private void UpdateScoreUI()
    {
        if (gameplayScoreText != null) gameplayScoreText.text = "Điểm: " + currentScore;
        if (winScoreText != null) winScoreText.text = "Điểm: " + currentScore;
        if (loseScoreText != null) loseScoreText.text = "Điểm: " + currentScore;
    }

    private void UpdateComboUI(int pointsAdded)
    {
        if (comboImage != null)
        {
            if (pointsAdded == 2)
            {
                // Chạy hiệu ứng xuất hiện và tan biến của hình ảnh Combo duy nhất
                if (comboCoroutine != null) StopCoroutine(comboCoroutine);
                comboCoroutine = StartCoroutine(AnimateComboImageRoutine(comboSprite, comboSound));
            }
            else
            {
                comboImage.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator AnimateComboImageRoutine(Sprite comboSprite, AudioClip comboSound)
    {
        // Gán hình ảnh tương ứng nếu có
        if (comboSprite != null) comboImage.sprite = comboSprite;
        
        comboImage.gameObject.SetActive(true);
        comboImage.transform.localScale = Vector3.zero;
        
        CanvasGroup cg = comboImage.GetComponent<CanvasGroup>();
        if (cg == null) cg = comboImage.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Phát âm thanh combo qua AudioManager (VFX)
        if (AudioManager.Instance != null && comboSound != null)
        {
            AudioManager.Instance.PlaySFX(comboSound);
        }

        // 1. Hiệu ứng xuất hiện (Scale up + Fade in)
        float elapsed = 0f;
        float fadeInDuration = 0.2f;
        while (elapsed < fadeInDuration)
        {
            if (isGameOver) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            
            // Dùng EaseOutBack để phóng to nảy nhẹ hình ảnh Combo dựa trên Scale gốc của Editor
            float scaleMultiplier = EaseOutBack(t) * 1.1f;
            comboImage.transform.localScale = new Vector3(comboOriginalScale.x * scaleMultiplier, comboOriginalScale.y * scaleMultiplier, comboOriginalScale.z);
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        comboImage.transform.localScale = comboOriginalScale;
        cg.alpha = 1f;

        // 2. Thời gian đứng yên hiển thị
        yield return new WaitForSeconds(0.8f);

        // 3. Hiệu ứng tan biến (Fade out + Trôi lên trên nhẹ)
        elapsed = 0f;
        float fadeOutDuration = 0.3f;
        Vector3 startPos = comboImage.transform.localPosition;
        Vector3 targetPos = startPos + new Vector3(0f, 50f, 0f);

        while (elapsed < fadeOutDuration)
        {
            if (isGameOver) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            comboImage.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        // Tắt và reset lại vị trí, tỉ lệ cũ
        comboImage.gameObject.SetActive(false);
        comboImage.transform.localPosition = startPos;
        comboImage.transform.localScale = comboOriginalScale;
    }

    // Hiệu ứng hiện các nút trên Panel tuần tự
    private IEnumerator AnimatePanelButtons(Button[] buttons)
    {
        float delayBetween = 0.2f;

        // Lưu lại scale gốc của từng nút thiết lập từ Editor
        Vector3[] originalScales = new Vector3[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                originalScales[i] = buttons[i].transform.localScale;
                if (originalScales[i] == Vector3.zero) originalScales[i] = Vector3.one;
                
                buttons[i].transform.localScale = Vector3.zero;
            }
        }

        // Cho từng nút xuất hiện tuần tự
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                StartCoroutine(AnimateButtonEntrance(buttons[i].GetComponent<RectTransform>(), originalScales[i]));
                yield return new WaitForSeconds(delayBetween);
            }
        }
    }

    private IEnumerator AnimateButtonEntrance(RectTransform btnTransform, Vector3 targetScale)
    {
        btnTransform.localScale = Vector3.zero;
        btnTransform.gameObject.SetActive(true);

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            // Thoát coroutine nếu game bị reset đột ngột
            if (!isGameOver || btnTransform == null || !btnTransform.gameObject.activeInHierarchy)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scaleMultiplier = EaseOutBack(t);
            btnTransform.localScale = new Vector3(targetScale.x * scaleMultiplier, targetScale.y * scaleMultiplier, targetScale.z);
            yield return null;
        }

        if (btnTransform != null) btnTransform.localScale = targetScale;
    }

    // Hàm EaseOutBack tạo chuyển động nảy nhẹ cho nút bấm và hình ảnh
    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}