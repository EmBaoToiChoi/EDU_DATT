using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct MathQuestion
{
    public string questionPart1;     // Số/Hạng tử thứ nhất (ví dụ: "1")
    public string questionPart2;     // Phép tính (ví dụ: "+")
    public string questionPart3;     // Số/Hạng tử thứ hai (ví dụ: "1")
    public string questionPart4;     // Dấu kết quả (ví dụ: "=")
    public int correctAnswerIndex;   // Vị trí Index của prefab đáp án đúng trong mảng answerPrefabs
}

public class MathBoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int rows = 4;
    public int cols = 4;

    [Header("Answer Prefabs Database")]
    [Tooltip("Danh sách các Prefab con số đáp án (0, 1, 2, 3, 4...)")]
    [SerializeField] private GameObject[] answerPrefabs; 

    [Header("Questions Database")]
    [Tooltip("Danh sách các câu hỏi hiển thị")]
    [SerializeField] private MathQuestion[] questions = new MathQuestion[]
    {
        new MathQuestion { questionPart1 = "1", questionPart2 = "+", questionPart3 = "1", questionPart4 = "=", correctAnswerIndex = 1 }, // Đáp án là 2 (index 1 nếu prefab là 1, 2, 3...)
        new MathQuestion { questionPart1 = "5", questionPart2 = "+", questionPart3 = "3", questionPart4 = "=", correctAnswerIndex = 7 }, // Đáp án là 8 (index 7)
        new MathQuestion { questionPart1 = "10", questionPart2 = "-", questionPart3 = "4", questionPart4 = "=", correctAnswerIndex = 5 }, // Đáp án là 6 (index 5)
        new MathQuestion { questionPart1 = "12", questionPart2 = "+", questionPart3 = "3", questionPart4 = "=", correctAnswerIndex = 14 } // Đáp án là 15 (index 14)
    };

    [Header("Question UI References")]
    [SerializeField] private TextMeshProUGUI questionTextUI; // Text hiển thị câu hỏi gộp (nếu có)
    [SerializeField] private TextMeshProUGUI questionPart1UI; // Khung hiển thị số thứ nhất
    [SerializeField] private TextMeshProUGUI questionPart2UI; // Khung hiển thị phép tính (+, -, ...)
    [SerializeField] private TextMeshProUGUI questionPart3UI; // Khung hiển thị số thứ hai
    [SerializeField] private TextMeshProUGUI questionPart4UI; // Khung hiển thị dấu bằng (=)

    [Header("References")]
    public Transform boardParent;
    public Slider expSlider; // Thanh thời gian đếm ngược (Slider thời gian)

    [Header("Hearts Settings")]
    [SerializeField] private HeartUI[] heartIcons;       // Mảng 3 trái tim UI
    [SerializeField] private GameObject gameOverPanel;   // Panel khi thua cuộc
    [SerializeField] private Button replayButton;        // Nút chơi lại trên Panel Thua
    [SerializeField] private Button exitButton;          // Nút thoát trên Panel Thua
    [SerializeField] private GameObject lobbyMenuUI;     // Menu Lobby (để quay lại nếu nhấn thoát trong game)
    [SerializeField] private GameObject winPanel;        // Panel khi chiến thắng
    [SerializeField] private Button winReplayButton;     // Nút chơi lại trên Panel Thắng
    [SerializeField] private Button winExitButton;       // Nút thoát trên Panel Thắng

    [Header("Gameplay Audio Clips")]
    [SerializeField] private AudioClip matchSound;       // Âm thanh khi nối đúng
    [SerializeField] private AudioClip errorSound;       // Âm thanh khi chọn sai / mất mạng
    [SerializeField] private AudioClip gameOverSound;    // Âm thanh khi thua cuộc
    [SerializeField] private AudioClip winSound;         // Âm thanh khi chiến thắng

    [Header("Score Settings")]
    [SerializeField] private TextMeshProUGUI gameplayScoreText; // Text điểm hiển thị trong màn chơi
    [SerializeField] private TextMeshProUGUI winScoreText;      // Text điểm hiển thị trên Panel Thắng
    [SerializeField] private TextMeshProUGUI loseScoreText;     // Text điểm hiển thị trên Panel Thua

    [Header("Timer Settings")]
    [SerializeField] private float totalTime = 90f;             // Thời gian giới hạn chơi (giây, mặc định 90s = 1p30s)
    private float remainingTime;                                // Thời gian còn lại

    [Header("Combo Settings")]
    [SerializeField] private float comboThreshold = 3.0f;       // Zeit tối đa giữa 2 lần ghép để tính combo (giây)
    [SerializeField] private Image comboImage;                  // Image UI hiển thị hình ảnh Combo
    [SerializeField] private Sprite comboSprite;                // Hình ảnh Combo x2 duy nhất
    [SerializeField] private AudioClip comboSound;              // Âm thanh hiệu ứng Combo duy nhất

    private int[,] matrix;
    private MathTileController firstSelected = null;
    private int currentQuestionIndex = 0;
    private bool isGameOver = false;

    private int currentScore = 0; // Điểm số hiện tại của màn chơi
    private float lastMatchTime = -100f; // Thời điểm ghép đúng trước đó
    private int currentCombo = 0; 
    private Coroutine comboCoroutine;
    private Vector3 comboOriginalScale = Vector3.one; 

    private int currentHearts = 3;
    private Vector2[] heartStartPositions;
    private Color heartOriginalColor;

    private void ShuffleQuestions()
    {
        if (questions == null || questions.Length <= 1) return;
        for (int i = questions.Length - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            MathQuestion temp = questions[i];
            questions[i] = questions[r];
            questions[r] = temp;
        }
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        
        // Cài đặt ban đầu
        currentQuestionIndex = 0;
        isGameOver = false;
        currentHearts = 3;
        currentScore = 0;
        remainingTime = totalTime;

        if (expSlider != null)
        {
            expSlider.minValue = 0;
            expSlider.maxValue = 100f;
            expSlider.value = 100f;
        }

        // 1. Dựng lưới bàn chơi một lần duy nhất chứa tất cả đáp án và tự sinh các câu hỏi tương ứng
        GenerateBoard();

        // 2. Khởi tạo hiển thị câu hỏi đầu tiên
        LoadQuestion(currentQuestionIndex);

        // Lưu vị trí và màu sắc ban đầu của các trái tim để reset khi chơi lại
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

    void LoadQuestion(int questionIndex)
    {
        if (questions == null || questions.Length == 0)
        {
            Debug.LogError("MathBoardManager: Chưa cấu hình danh sách câu hỏi!");
            return;
        }

        if (questionIndex >= questions.Length)
        {
            // Đã trả lời hết toàn bộ các câu hỏi -> Chiến thắng!
            TriggerGameOver(true);
            return;
        }

        // 1. Cập nhật Text câu hỏi trên màn hình
        // 1. Cập nhật Text câu hỏi trên màn hình (cho cả 4 khung riêng biệt)
        if (questionPart1UI != null) questionPart1UI.text = questions[questionIndex].questionPart1;
        if (questionPart2UI != null) questionPart2UI.text = questions[questionIndex].questionPart2;
        if (questionPart3UI != null) questionPart3UI.text = questions[questionIndex].questionPart3;
        if (questionPart4UI != null) questionPart4UI.text = questions[questionIndex].questionPart4;

        // Giữ tương thích ngược với ô Text gộp cũ nếu có gán
        if (questionTextUI != null)
        {
            questionTextUI.text = questions[questionIndex].questionPart1 + " " + 
                                  questions[questionIndex].questionPart2 + " " + 
                                  questions[questionIndex].questionPart3 + " " + 
                                  questions[questionIndex].questionPart4;
        }

        firstSelected = null;
    }

    private MathQuestion GenerateDynamicQuestion(int id)
    {
        int targetValue = id + 1; // Giá trị đáp án (từ 1 đến 16)
        
        string part1 = "";
        string part2 = "";
        string part3 = "";
        string part4 = "=";

        // Chọn ngẫu nhiên phép cộng (+) hoặc trừ (-)
        int op = Random.Range(0, 2); // 0: cộng, 1: trừ

        if (targetValue == 1)
        {
            op = 0; // Để 1 luôn dùng phép cộng 0 + 1 hoặc 1 + 0
        }

        if (op == 0) // Phép cộng: A + B = targetValue
        {
            int a = Random.Range(0, targetValue); // A từ 0 đến targetValue - 1
            int b = targetValue - a;
            part1 = a.ToString();
            part2 = "+";
            part3 = b.ToString();
        }
        else // Phép trừ: A - B = targetValue
        {
            // A từ targetValue + 1 đến targetValue + 8
            int a = Random.Range(targetValue + 1, targetValue + 9);
            int b = a - targetValue;
            part1 = a.ToString();
            part2 = "-";
            part3 = b.ToString();
        }

        return new MathQuestion
        {
            questionPart1 = part1,
            questionPart2 = part2,
            questionPart3 = part3,
            questionPart4 = part4,
            correctAnswerIndex = id
        };
    }

    void GenerateBoard()
    {
        // 1. Xóa bàn chơi cũ nếu có
        if (boardParent != null)
        {
            foreach (Transform child in boardParent)
            {
                Destroy(child.gameObject);
            }
        }

        matrix = new int[rows + 2, cols + 2];

        int totalPlayableTiles = rows * cols;
        List<int> elementIDs = new List<int>();

        int numPairs = totalPlayableTiles / 2;

        if (answerPrefabs == null || answerPrefabs.Length == 0)
        {
            Debug.LogError("MathBoardManager: Chưa gán Answer Prefabs!");
            return;
        }

        // 2. Chọn ra các ID đáp án không trùng lặp để tạo bàn chơi và câu hỏi
        List<int> selectedIDs = new List<int>();
        List<int> availableIDs = new List<int>();
        for (int i = 0; i < answerPrefabs.Length; i++)
        {
            availableIDs.Add(i);
        }

        for (int i = 0; i < numPairs; i++)
        {
            int chosenID = 0;
            if (availableIDs.Count > 0)
            {
                int randIndex = Random.Range(0, availableIDs.Count);
                chosenID = availableIDs[randIndex];
                availableIDs.RemoveAt(randIndex);
            }
            else
            {
                chosenID = Random.Range(0, answerPrefabs.Length);
            }
            selectedIDs.Add(chosenID);

            // Mỗi đáp án thêm 2 lần để tạo thành cặp
            elementIDs.Add(chosenID);
            elementIDs.Add(chosenID);
        }

        // Trộn ngẫu nhiên thứ tự đáp án để tạo câu hỏi ngẫu nhiên
        List<int> shuffledIDsForQuestions = new List<int>(selectedIDs);
        for (int i = shuffledIDsForQuestions.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            int temp = shuffledIDsForQuestions[i];
            shuffledIDsForQuestions[i] = shuffledIDsForQuestions[r];
            shuffledIDsForQuestions[r] = temp;
        }

        // 3. Tạo danh sách câu hỏi dựa trên các đáp án đã chọn
        questions = new MathQuestion[shuffledIDsForQuestions.Count];
        for (int i = 0; i < shuffledIDsForQuestions.Count; i++)
        {
            questions[i] = GenerateDynamicQuestion(shuffledIDsForQuestions[i]);
        }

        // Xáo trộn vị trí các ô trên bàn chơi (Shuffle)
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
            gridLayout.enabled = false; 
        }

        // Tự động phát hiện kích thước lớn nhất của các Prefab đáp án
        if (answerPrefabs != null && answerPrefabs.Length > 0)
        {
            float maxW = 0f;
            float maxH = 0f;
            foreach (var prefab in answerPrefabs)
            {
                if (prefab != null)
                {
                    RectTransform rt = prefab.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        maxW = Mathf.Max(maxW, rt.sizeDelta.x * rt.localScale.x);
                        maxH = Mathf.Max(maxH, rt.sizeDelta.y * rt.localScale.y);
                    }
                }
            }
            if (maxW > 0 && maxH > 0)
            {
                cellSize = new Vector2(maxW, maxH);
            }
        }

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

            if (netParentWidth <= 0f) netParentWidth = parentWidth;
            if (netParentHeight <= 0f) netParentHeight = parentHeight;

            // Tính tỉ lệ thu nhỏ tự động nếu lưới bị tràn ra ngoài vùng chứa
            if (totalWidth > netParentWidth || totalHeight > netParentHeight)
            {
                float scaleX = netParentWidth / totalWidth;
                float scaleY = netParentHeight / totalHeight;
                gridScale = Mathf.Min(scaleX, scaleY);
            }

            float scaledCellWidth = cellSize.x * gridScale;
            float scaledCellHeight = cellSize.y * gridScale;
            float scaledSpacingX = spacing.x * gridScale;
            float scaledSpacingY = spacing.y * gridScale;

            float scaledTotalWidth = cols * scaledCellWidth + (cols - 1) * scaledSpacingX;
            float scaledTotalHeight = rows * scaledCellHeight + (rows - 1) * scaledSpacingY;

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
            else
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
            else 
            {
                startY = parentHeight / 2f - padTop - (netParentHeight - scaledTotalHeight) / 2f - scaledCellHeight / 2f;
            }
        }
        else
        {
            startX = -totalWidth / 2f + cellSize.x / 2f;
            startY = totalHeight / 2f - cellSize.y / 2f;
        }

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
                matrix[r, c] = id + 1; // Lưu ID trong ma trận

                GameObject go = Instantiate(answerPrefabs[id], boardParent);
                
                RectTransform goRect = go.GetComponent<RectTransform>();
                RectTransform prefabRect = answerPrefabs[id].GetComponent<RectTransform>();
                if (goRect != null && prefabRect != null)
                {
                    Vector2 originalSize = prefabRect.sizeDelta;
                    Vector3 originalScale = prefabRect.localScale;
                    Vector2 originalPivot = prefabRect.pivot;

                    goRect.anchorMin = new Vector2(0.5f, 0.5f);
                    goRect.anchorMax = new Vector2(0.5f, 0.5f);
                    goRect.pivot = originalPivot;
                    goRect.sizeDelta = originalSize;
                    
                    Vector3 targetScale = new Vector3(originalScale.x * gridScale, originalScale.y * gridScale, originalScale.z * gridScale);
                    goRect.localScale = targetScale;

                    float posX = startX + (c - 1) * (finalCellWidth + finalSpacingX);
                    float posY = startY - (r - 1) * (finalCellHeight + finalSpacingY);

                    float offsetX = (originalPivot.x - 0.5f) * originalSize.x * targetScale.x;
                    float offsetY = (originalPivot.y - 0.5f) * originalSize.y * targetScale.y;

                    goRect.anchoredPosition = new Vector2(posX + offsetX, posY + offsetY);
                }

                MathTileController tile = go.GetComponent<MathTileController>();
                if (tile == null)
                {
                    TileController oldTile = go.GetComponent<TileController>();
                    if (oldTile != null)
                    {
                        tile = go.AddComponent<MathTileController>();
                        tile.CopyFromOldTile(oldTile);
                        Destroy(oldTile);
                    }
                }

                if (tile != null)
                {
                    tile.SetupTile(r, c, id + 1, this);
                }

                index++;
            }
        }
    }

    public void SelectTile(MathTileController clickedTile)
    {
        if (isGameOver)
        {
            Debug.Log("[MathBoardManager] SelectTile: Game đã kết thúc, bỏ qua click.");
            return;
        }

        Debug.Log($"[MathBoardManager] Nhận sự kiện click từ ô ({clickedTile.x}, {clickedTile.y}) - ID: {clickedTile.elementID}");

        if (firstSelected == clickedTile)
        {
            Debug.Log("[MathBoardManager] Click trùng ô cũ -> Hủy chọn.");
            firstSelected.SetSelectedVisual(false);
            firstSelected = null;
            return;
        }

        if (firstSelected == null)
        {
            firstSelected = clickedTile;
            firstSelected.SetSelectedVisual(true);
            Debug.Log($"[MathBoardManager] Chọn ô thứ nhất: ({firstSelected.x}, {firstSelected.y}) - ID: {firstSelected.elementID}");
        }
        else
        {
            bool isMatch = false;
            int currentCorrectAnswerID = questions[currentQuestionIndex].correctAnswerIndex + 1;

            Debug.Log($"[MathBoardManager] So sánh: Ô thứ nhất ID={firstSelected.elementID} | Ô thứ hai ID={clickedTile.elementID} | Đáp án đúng ID={currentCorrectAnswerID}");

            // Kiểm tra: Hai ô được chọn phải có cùng ID VÀ phải là ID của ĐÁP ÁN ĐÚNG
            if (firstSelected.elementID == clickedTile.elementID && firstSelected.elementID == currentCorrectAnswerID)
            {
                // Dù bị kẹt hay bị vây quanh vẫn kết nối và khớp được (Không dùng thuật toán tìm đường đi)
                isMatch = true;
                Debug.Log("[MathBoardManager] GHÉP ĐÚNG! Đang tạo hiệu ứng kết nối...");
                
                matrix[firstSelected.x, firstSelected.y] = 0;
                matrix[clickedTile.x, clickedTile.y] = 0;

                firstSelected.PlayMatchEffect();
                clickedTile.PlayMatchEffect();

                if (AudioManager.Instance != null && matchSound != null)
                {
                    AudioManager.Instance.PlaySFX(matchSound);
                }

                // Cộng điểm và kiểm tra chuyển câu tiếp theo
                GainScoreAndCheckNextQuestion();
            }

            if (!isMatch)
            {
                // Chọn sai (Chọn 2 ô khác nhau, hoặc chọn 2 ô giống nhau nhưng KHÔNG PHẢI đáp án đúng của câu này)
                Debug.Log("[MathBoardManager] GHÉP SAI! Trừ 1 tim.");
                firstSelected.SetSelectedVisual(false);
                DeductHeart();
            }

            firstSelected = null;
        }
    }

    private void GainScoreAndCheckNextQuestion()
    {
        // Tính toán combo dựa trên khoảng thời gian với lần ghép đúng trước đó
        float timeSinceLastMatch = Time.time - lastMatchTime;
        int pointsAdded = 1;

        if (timeSinceLastMatch <= comboThreshold)
        {
            pointsAdded = 2; // Ghép nhanh -> Đạt combo, cộng 2 điểm
        }
        else
        {
            pointsAdded = 1;
        }
        lastMatchTime = Time.time;

        currentScore += pointsAdded;

        UpdateScoreUI();
        UpdateComboUI(pointsAdded);

        // Chuyển sang câu hỏi tiếp theo
        currentQuestionIndex++;
        
        // Chờ hiệu ứng biến mất kết thúc (khoảng 0.35s) trước khi sinh câu hỏi mới
        StartCoroutine(LoadNextQuestionRoutine(0.35f));
    }

    private IEnumerator LoadNextQuestionRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadQuestion(currentQuestionIndex);
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
            Debug.Log("LEVEL UP! Chúc mừng hoàn thành toàn bộ câu hỏi Toán!");
            if (winPanel != null)
            {
                winPanel.SetActive(true);
                StartCoroutine(AnimatePanelButtons(new Button[] { winReplayButton, winExitButton }));
            }
            if (AudioManager.Instance != null && winSound != null)
            {
                AudioManager.Instance.PlaySFX(winSound);
            }
        }
        else
        {
            Debug.Log("GAME OVER! Bạn đã thua cuộc.");
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
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
        currentQuestionIndex = 0;
        isGameOver = false;
        currentHearts = 3;
        currentScore = 0; 
        lastMatchTime = -100f;
        firstSelected = null;
        remainingTime = totalTime;

        if (expSlider != null) expSlider.value = 100f;
        UpdateScoreUI();

        if (comboImage != null)
        {
            comboImage.gameObject.SetActive(false); 
        }

        // Reset hiển thị 3 quả tim
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

        // Ẩn Panel Thua & Thắng
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        // Tạo lại lưới bàn chơi mới hoàn toàn và tự sinh các câu hỏi tương ứng
        GenerateBoard();

        // Tải câu hỏi đầu tiên
        LoadQuestion(currentQuestionIndex);
    }

    private void OnExitClicked()
    {
        if (lobbyMenuUI != null)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            gameObject.SetActive(false); // Ẩn Canvas chơi game hiện tại
            lobbyMenuUI.SetActive(true); // Hiện Menu Lobby
        }
        else
        {
            Debug.Log("MathBoardManager: Thoát game!");
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
        if (comboSprite != null) comboImage.sprite = comboSprite;
        
        comboImage.gameObject.SetActive(true);
        comboImage.transform.localScale = Vector3.zero;
        
        CanvasGroup cg = comboImage.GetComponent<CanvasGroup>();
        if (cg == null) cg = comboImage.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        if (AudioManager.Instance != null && comboSound != null)
        {
            AudioManager.Instance.PlaySFX(comboSound);
        }

        float elapsed = 0f;
        float fadeInDuration = 0.2f;
        while (elapsed < fadeInDuration)
        {
            if (isGameOver) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            
            float scaleMultiplier = EaseOutBack(t) * 1.1f;
            comboImage.transform.localScale = new Vector3(comboOriginalScale.x * scaleMultiplier, comboOriginalScale.y * scaleMultiplier, comboOriginalScale.z);
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        comboImage.transform.localScale = comboOriginalScale;
        cg.alpha = 1f;

        yield return new WaitForSeconds(0.8f);

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

        comboImage.gameObject.SetActive(false);
        comboImage.transform.localPosition = startPos;
        comboImage.transform.localScale = comboOriginalScale;
    }

    private IEnumerator AnimatePanelButtons(Button[] buttons)
    {
        float delayBetween = 0.2f;

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

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
