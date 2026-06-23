using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking; // Thư viện mạng bắt buộc để kết nối API Swagger

// --- CẤU TRÚC ĐỊNH DẠNG CHUẨN ĐỂ ĐỌC DỮ LIỆU JSON TỪ SWAGGER ---

[System.Serializable]
public struct APIAnswerData
{
    public string id;
    public string answerName; // Nội dung câu trả lời (Ví dụ: "6", "7", "11")
    public bool isAnswer;     // true nếu đây là đáp án đúng
}

[System.Serializable]
public struct APIQuestionItem
{
    public int id;
    public string questionName;      // Nội dung câu hỏi
    public int typeQuestion;         // 100: Trắc nghiệm, 200: Ô trống
    public string status;            // Trạng thái câu hỏi (basic, level, advanced...)
    public List<APIAnswerData> answers; // Mảng danh sách các câu trả lời động
}

[System.Serializable]
public class SwaggerResponseWrapper
{
    public List<APIQuestionItem> data; // Mảng "data" chứa danh sách câu hỏi ở ngoài cùng JSON
}

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance;

    [Header("Stars UI")]
    public GameObject[] starFails; 
    private int currentFails = 0;
    private int maxFails = 3;

    [Header("Lists Component (Kéo thả 8 cái từ Hierarchy)")]
    public GameObject[] cardButtons;   // 8 cái Card_Btn
    public TextMeshProUGUI[] questionTexts; // 8 cái Text câu hỏi TMP
    public GameObject[] pieceLocks;    // 8 cái Piece_Lock_img
    public GameObject[] pieceUnlocks;  // 8 cái Piece_UnLock_img
    
    [Header("Sprite Ảnh Chiến Thắng Để Thay Thế Gốc")]
    public Sprite[] correctSprites; // Kéo thả trực tiếp 8 file ảnh chiến thắng (Sprite) từ Project vào đây

    [Header("Khung Đáp Án Dùng Chung (Kéo từ Inspector)")]
    public GameObject answerPanel;     
    public Button btnA;                
    public Button btnB;                
    public Button btnC;                
    public Button btnD;                

    [Header("Cấu Hình Hướng Dẫn / Gợi Ý Tự Động")]
    [Tooltip("Thời gian không tương tác (giây) để kích hoạt gợi ý")]
    public float idleHintTime = 15f; 
    private float currentIdleTime = 0f;
    private bool isCountingIdle = false;
    private Coroutine hintCoroutine = null;

    [Header("Cấu Hình Kết Nối API Swagger")]
    [Tooltip("Đường link API lấy danh sách câu hỏi")]
    public string swaggerApiURL = "https://class-edu-v1.onrender.com/questions?page=1&limit=30&sortBy=createdAt&sortOrder=ASC";
    
    [Tooltip("Nhập chính xác chữ status bạn muốn lọc cho màn này (Ví dụ: basic, level, advanced...)")]
    public string statusToFilter = "basic"; 

    // Danh sách lưu trữ câu hỏi sau khi tải từ API và lọc sạch
    private List<APIQuestionItem> activeQuestionList = new List<APIQuestionItem>();

    private int completedQuestions = 0;
    private int totalQuestions = 8; // Mặc định map với 8 thẻ bài trong game của bạn
    private int currentOpeningCardIndex = -1; 
    private bool[] isCardCompleted = new bool[8];

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Khởi tạo trạng thái ẩn/hiện ban đầu cho các thành phần UI
        foreach (GameObject star in starFails) star.SetActive(false);
        foreach (GameObject piece in pieceUnlocks) piece.SetActive(false);
        foreach (GameObject lockImg in pieceLocks) lockImg.SetActive(true);

        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] != null && cardButtons[i].transform.childCount > 0)
            {
                cardButtons[i].transform.GetChild(0).gameObject.SetActive(false);
            }
        }

        if (answerPanel != null) answerPanel.SetActive(false);

        // Kích hoạt tiến trình tải câu hỏi từ API trực tuyến
        StartCoroutine(FetchQuestionsFromSwagger());
    }

    private IEnumerator FetchQuestionsFromSwagger()
    {
        Debug.Log("Đang tải dữ liệu câu hỏi từ API Swagger...");
        using (UnityWebRequest webRequest = UnityWebRequest.Get(swaggerApiURL))
        {
            // Chờ phản hồi từ server mạng
            yield return webRequest.SendWebRequest();

            // Kiểm tra nếu xảy ra lỗi kết nối internet hoặc lỗi từ phía server
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Không thể kết nối đến API Swagger hoặc lỗi Server. Lỗi: " + webRequest.error);
                // ĐÃ BỎ HOÀN TOÀN CÂU HỎI OFFLINE THEO YÊU CẦU
            }
            else
            {
                string jsonResponse = webRequest.downloadHandler.text;
                try
                {
                    // Giải mã chuỗi JSON nhận về đổ vào Wrapper Class
                    SwaggerResponseWrapper wrapper = JsonUtility.FromJson<SwaggerResponseWrapper>(jsonResponse);
                    
                    if (wrapper != null && wrapper.data != null)
                    {
                        activeQuestionList.Clear();
                        
                        // LỌC DỮ LIỆU: Chỉ lấy câu hỏi Trắc nghiệm (100) VÀ có status trùng khớp với Inspector
                        foreach (var item in wrapper.data)
                        {
                            bool matchStatus = !string.IsNullOrEmpty(item.status) && 
                                               item.status.ToLower().Trim() == statusToFilter.ToLower().Trim();

                            // Điều kiện: Là câu trắc nghiệm, khớp status và phải có ít nhất 1 đáp án trở lên
                            if (item.typeQuestion == 100 && matchStatus && item.answers != null && item.answers.Count > 0)
                            {
                                activeQuestionList.Add(item);
                            }
                        }

                        Debug.Log($"Đã tải thành công và lọc được {activeQuestionList.Count} câu trắc nghiệm có status '{statusToFilter}'!");
                        
                        // Nếu số câu hỏi lấy về từ API ít hơn số lượng thẻ bài ban đầu, cập nhật lại tổng số màn
                        if (activeQuestionList.Count < totalQuestions)
                        {
                            totalQuestions = activeQuestionList.Count;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Lỗi phân rã chuỗi JSON từ Swagger: " + e.Message);
                }
            }
        }
    }

    private void Update()
    {
        // Bộ đếm thời gian chờ để tự động hiển thị gợi ý hướng dẫn
        if (isCountingIdle && currentOpeningCardIndex != -1)
        {
            currentIdleTime += Time.deltaTime;
            if (currentIdleTime >= idleHintTime)
            {
                isCountingIdle = false; 
                ShowAnswerHint();       
            }
        }
    }

    public void OnClickCard(int cardIndex)
    {
        if (cardIndex >= activeQuestionList.Count) return;
        if (isCardCompleted[cardIndex]) return;
        if (currentOpeningCardIndex == cardIndex) return;

        if (AudioManagers.Instance != null) AudioManagers.Instance.PlayClick();
        ResetAllButtonColors(); 

        if (currentOpeningCardIndex != -1)
        {
            cardButtons[currentOpeningCardIndex].transform.GetChild(0).gameObject.SetActive(false);
        }

        currentOpeningCardIndex = cardIndex;
        cardButtons[cardIndex].transform.GetChild(0).gameObject.SetActive(true);
        answerPanel.SetActive(true);

        // Áp dụng dữ liệu câu hỏi động từ API Swagger lên UI giao diện
        APIQuestionItem currentQuestion = activeQuestionList[cardIndex];
        questionTexts[cardIndex].text = currentQuestion.questionName;

        // BƯỚC TỐI ƯU ẨN NÚT: Tạm thời ẩn TẤT CẢ các nút đáp án đi trước
        btnA.gameObject.SetActive(false);
        btnB.gameObject.SetActive(false);
        btnC.gameObject.SetActive(false);
        btnD.gameObject.SetActive(false);

        // Đổ chữ và CHỈ BẬT lại những nút thực sự có đáp án trả về từ API (Hỗ trợ câu 2 hoặc 3 đáp án)
        if (currentQuestion.answers.Count > 0) SetupSingleAnswerButton(btnA, currentQuestion.answers[0], 0);
        if (currentQuestion.answers.Count > 1) SetupSingleAnswerButton(btnB, currentQuestion.answers[1], 1);
        if (currentQuestion.answers.Count > 2) SetupSingleAnswerButton(btnC, currentQuestion.answers[2], 2);
        if (currentQuestion.answers.Count > 3) SetupSingleAnswerButton(btnD, currentQuestion.answers[3], 3);

        // Khởi động lại bộ đếm thời gian chờ tính năng hướng dẫn
        currentIdleTime = 0f;
        isCountingIdle = true;
    }

    private void SetupSingleAnswerButton(Button btn, APIAnswerData answerInfo, int indexInList)
    {
        // Kích hoạt hiển thị nút này lên màn hình UI vì nó có dữ liệu hợp lệ
        btn.gameObject.SetActive(true);
        
        string prefix = indexInList == 0 ? "A. " : indexInList == 1 ? "B. " : indexInList == 2 ? "C. " : "D. ";
        btn.GetComponentInChildren<TextMeshProUGUI>().text = prefix + answerInfo.answerName;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => CheckAnswer(answerInfo.isAnswer));
    }

    public void CheckAnswer(bool isCorrect)
    {
        ResetAllButtonColors(); 

        if (isCorrect)
        {
            AnswerCorrect(currentOpeningCardIndex);
        }
        else
        {
            AnswerWrong();
            currentIdleTime = 0f;
            isCountingIdle = true;
        }
    }

    // --- LOGIC TỰ ĐỘNG GỢI Ý / HƯỚNG DẪN ĐÁP ÁN ĐÚNG ---
    private void ShowAnswerHint()
    {
        if (currentOpeningCardIndex == -1 || currentOpeningCardIndex >= activeQuestionList.Count) return;

        APIQuestionItem currentQuestion = activeQuestionList[currentOpeningCardIndex];
        Button targetButton = null;

        // Duyệt mảng tìm xem nút nào chứa thuộc tính câu trả lời đúng (isAnswer == true)
        for (int i = 0; i < currentQuestion.answers.Count; i++)
        {
            if (currentQuestion.answers[i].isAnswer)
            {
                if (i == 0) targetButton = btnA;
                else if (i == 1) targetButton = btnB;
                else if (i == 2) targetButton = btnC;
                else if (i == 3) targetButton = btnD;
                break;
            }
        }

        // Chỉ cho nhấp nháy nếu nút đúng đó đang được bật hoạt động (Active) trên UI
        if (targetButton != null && targetButton.gameObject.activeSelf)
        {
            if (hintCoroutine != null) StopCoroutine(hintCoroutine);
            hintCoroutine = StartCoroutine(BlinkHintRoutine(targetButton));
        }
    }

    private IEnumerator BlinkHintRoutine(Button targetBtn)
    {
        Image btnImage = targetBtn.GetComponent<Image>();
        if (btnImage == null) yield break;

        Color originalColor = btnImage.color;
        Color hintColor = new Color(0.4f, 1f, 0.4f, 1f); 

        while (true)
        {
            btnImage.color = hintColor;
            yield return new WaitForSeconds(0.4f);
            btnImage.color = originalColor;
            yield return new WaitForSeconds(0.4f);
        }
    }

    private void ResetAllButtonColors()
    {
        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
            hintCoroutine = null;
        }

        if (btnA != null) btnA.GetComponent<Image>().color = Color.white;
        if (btnB != null) btnB.GetComponent<Image>().color = Color.white;
        if (btnC != null) btnC.GetComponent<Image>().color = Color.white;
        if (btnD != null) btnD.GetComponent<Image>().color = Color.white;

        isCountingIdle = false;
        currentIdleTime = 0f;
    }

    private void AnswerCorrect(int cardIndex)
    {
        Debug.Log("Trả lời CHÍNH XÁC!");
        if (AudioManagers.Instance != null) AudioManagers.Instance.PlayCorrect();

        isCardCompleted[cardIndex] = true;
        cardButtons[cardIndex].transform.GetChild(0).gameObject.SetActive(false);

        Image cardMainImage = cardButtons[cardIndex].GetComponent<Image>();
        if (cardMainImage != null && correctSprites != null && cardIndex < correctSprites.Length && correctSprites[cardIndex] != null)
        {
            cardMainImage.sprite = correctSprites[cardIndex]; 
        }

        answerPanel.SetActive(false);
        currentOpeningCardIndex = -1; 

        pieceLocks[cardIndex].SetActive(false);
        pieceUnlocks[cardIndex].SetActive(true);

        completedQuestions++;

        if (completedQuestions >= totalQuestions)
        {
            Invoke("SwitchToXepHinhMode", 1f); 
        }
    }

    private void AnswerWrong()
    {
        if (currentFails < maxFails)
        {
            if (AudioManagers.Instance != null) AudioManagers.Instance.PlayWrong();

            starFails[currentFails].SetActive(true);
            currentFails++;

            if (currentFails >= maxFails)
            {
                UIManager.Instance.ShowGameOverPanel();
            }
        }
    }

    private void SwitchToXepHinhMode()
    {
        UIManager.Instance.ShowXepHinhPanel();
    }

    private int correctPiecesCount = 0; 
    public void OnPiecePlacedCorrectly()
    {
        correctPiecesCount++;
        if (AudioManagers.Instance != null) AudioManagers.Instance.PlayCorrect();

        if (correctPiecesCount >= totalQuestions)
        {
            Invoke("TriggerWinUI", 0.5f);
        }
    }

    private void TriggerWinUI()
    {
        PlayerPrefs.SetInt("Level_1_Completed", 1);
        PlayerPrefs.SetInt("Unlocked_Photo_1", 1);
        PlayerPrefs.Save();
        UIManager.Instance.ShowYouWinPanel();
    }
}