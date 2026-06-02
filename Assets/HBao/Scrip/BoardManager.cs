using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nhập thư viện TMPro để quản lý hiển thị chữ điểm số

// Cấu trúc dữ liệu để gom Tên và Ảnh của 1 chất hóa học
[System.Serializable]
public class ElementData
{
    public string elementName;
    public Sprite elementSprite;
}

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int rows = 6;
    public int cols = 10;

    [Header("Element Database")]
    // Danh sách Tên + Ảnh của các chất (nhập từ Inspector)
    public ElementData[] elementDatabase;

    [Header("References")]
    public GameObject tilePrefab;
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
            expSlider.maxValue = maxExp;
            expSlider.value = 0;
        }

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

    void GenerateBoardLogic()
    {
        int totalPlayableTiles = rows * cols;
        List<int> elementIDs = new List<int>();

        int numPairs = totalPlayableTiles / 2;
        for (int i = 0; i < numPairs; i++)
        {
            // Lấy ngẫu nhiên ID từ danh sách Database bạn nạp vào
            int randomID = Random.Range(0, elementDatabase.Length);
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

        GridLayoutGroup gridLayout = boardParent.GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = cols;

        int index = 0;
        for (int r = 1; r <= rows; r++)
        {
            for (int c = 1; c <= cols; c++)
            {
                int id = elementIDs[index];
                matrix[r, c] = id + 1; // +1 để phân biệt với 0 (ô trống)

                GameObject go = Instantiate(tilePrefab, boardParent);
                TileController tile = go.GetComponent<TileController>();
                // Gửi dữ liệu Tên + Ảnh vào ô
                tile.SetupTile(r, c, id + 1, elementDatabase[id], this);

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

                // Sử dụng thuật toán quét thẳng dòng (Line-Search BFS) mới
                if (CheckPath(p1, p2))
                {
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
        if (expSlider != null) expSlider.value = currentExp;

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

        currentHearts--;

        // Kích hoạt hiệu ứng rơi tim tương ứng
        int heartIndex = currentHearts;
        if (heartIcons != null && heartIndex >= 0 && heartIndex < heartIcons.Length)
        {
            if (heartIcons[heartIndex] != null)
            {
                heartIcons[heartIndex].PlayFallEffect();
            }
        }

        // Phát âm thanh khi chọn sai
        if (AudioManager.Instance != null && errorSound != null)
        {
            AudioManager.Instance.PlaySFX(errorSound);
        }

        // Kiểm tra điều kiện thua cuộc
        if (currentHearts <= 0)
        {
            TriggerGameOver(false);
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
        if (expSlider != null) expSlider.value = 0;
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

        // Khởi tạo scale của các nút về 0
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null) buttons[i].transform.localScale = Vector3.zero;
        }

        // Cho từng nút xuất hiện tuần tự
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                StartCoroutine(AnimateButtonEntrance(buttons[i].GetComponent<RectTransform>()));
                yield return new WaitForSeconds(delayBetween);
            }
        }
    }

    private IEnumerator AnimateButtonEntrance(RectTransform btnTransform)
    {
        btnTransform.localScale = Vector3.zero;
        btnTransform.gameObject.SetActive(true);

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = EaseOutBack(t);
            btnTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        btnTransform.localScale = Vector3.one;
    }

    // Hàm EaseOutBack tạo chuyển động nảy nhẹ cho nút bấm và hình ảnh
    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}