using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct QuestionData
{
    [TextArea(2, 3)]
    public string questionText; 
    public string answerA;      
    public string answerB;      
    public string answerC;      
    public string answerD;      
    public string correctAnswer; 
}

public class QuizManager : MonoBehaviour
{
    [Header("Stars UI")]
    public GameObject[] starFails; 
    private int currentFails = 0;
    private int maxFails = 3;

    [Header("Lists Component (Kéo thả 8 cái từ Hierarchy)")]
    public GameObject[] cardButtons;   // 8 cái Card_Btn
    public TextMeshProUGUI[] questionTexts; // 8 cái Text câu hỏi TMP
    public GameObject[] pieceLocks;    // 8 cái Piece_Lock_img
    public GameObject[] pieceUnlocks;  // 8 cái Piece_UnLock_img
    
    [Header("Sprite Ảnh Chiến Thắng Để Thay Thế Gốc (Mới Tối Ưu)")]
    public Sprite[] correctSprites; // Kéo thả trực tiếp 8 file ảnh chiến thắng (Sprite) từ Project vào đây!

    [Header("Khung Đáp Án Dùng Chung (Kéo từ Inspector)")]
    public GameObject answerPanel;     
    public Button btnA;                
    public Button btnB;                
    public Button btnC;                
    public Button btnD;                

    [Header("Data Câu Hỏi (Lớp 1 - Lớp 3)")]
    public QuestionData[] questionDataList = new QuestionData[8]
    {
        new QuestionData { questionText = "Lớp 1: 5 + 4 bằng bao nhiêu?", answerA = "7", answerB = "8", answerC = "9", answerD = "10", correctAnswer = "C" },
        new QuestionData { questionText = "Lớp 1: Hình nào sau đây có 3 cạnh?", answerA = "Hình vuông", answerB = "Hình tam giác", answerC = "Hình tròn", answerD = "Hình chữ nhật", correctAnswer = "B" },
        new QuestionData { questionText = "Lớp 2: 5 x 4 bằng bao nhiêu?", answerA = "20", answerB = "25", answerC = "15", answerD = "30", correctAnswer = "A" },
        new QuestionData { questionText = "Lớp 2: Số liền sau của số 99 là số nào?", answerA = "98", answerB = "100", answerC = "101", answerD = "90", correctAnswer = "B" },
        new QuestionData { questionText = "Lớp 3: Số gồm 3 nghìn, 5 trăm và 2 đơn vị viết là:", answerA = "352", answerB = "3520", answerC = "3052", answerD = "3502", correctAnswer = "D" },
        new QuestionData { questionText = "Lớp 3: Một hình vuông có cạnh 5cm. Chu vi hình vuông đó là?", answerA = "20cm", answerB = "25cm", answerC = "15cm", answerD = "10cm", correctAnswer = "A" },
        new QuestionData { questionText = "Lớp 2: Có 14 quả táo chia đều cho 2 bạn. Mỗi bạn được mấy quả?", answerA = "6 quả", answerB = "7 quả", answerC = "8 quả", answerD = "9 quả", correctAnswer = "B" },
        new QuestionData { questionText = "Lớp 3: Tháng nào sau đây luôn có 28 hoặc 29 ngày?", answerA = "Tháng 1", answerB = "Tháng 2", answerC = "Tháng 3", answerD = "Tháng 12", correctAnswer = "B" }
    };

    private int completedQuestions = 0;
    private int totalQuestions = 8;
    private int currentOpeningCardIndex = -1; 
    
    // Mảng lưu trạng thái câu hỏi đã xong để tránh việc nhấn lại vào thẻ đã làm đúng
    private bool[] isCardCompleted = new bool[8];

    private void Start()
    {
        foreach (GameObject star in starFails) star.SetActive(false);
        foreach (GameObject piece in pieceUnlocks) piece.SetActive(false);
        foreach (GameObject lockImg in pieceLocks) lockImg.SetActive(true);

        // Ban đầu ẩn toàn bộ các khung Image câu hỏi (con đầu tiên) của từng Card
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] != null && cardButtons[i].transform.childCount > 0)
            {
                cardButtons[i].transform.GetChild(0).gameObject.SetActive(false);
            }
        }

        if (answerPanel != null) answerPanel.SetActive(false);
    }

    public void OnClickCard(int cardIndex)
    {
        // Nếu thẻ này đã làm đúng rồi thì khóa hoàn toàn không phản hồi Click nữa
        if (isCardCompleted[cardIndex]) return;
        if (currentOpeningCardIndex == cardIndex) return;

        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayClick();
        }

        if (currentOpeningCardIndex != -1)
        {
            cardButtons[currentOpeningCardIndex].transform.GetChild(0).gameObject.SetActive(false);
        }

        currentOpeningCardIndex = cardIndex;

        GameObject questionImage = cardButtons[cardIndex].transform.GetChild(0).gameObject;
        questionImage.SetActive(true);

        answerPanel.SetActive(true);

        QuestionData data = questionDataList[cardIndex];

        questionTexts[cardIndex].text = data.questionText;

        btnA.GetComponentInChildren<TextMeshProUGUI>().text = "A. " + data.answerA;
        btnB.GetComponentInChildren<TextMeshProUGUI>().text = "B. " + data.answerB;
        btnC.GetComponentInChildren<TextMeshProUGUI>().text = "C. " + data.answerC;
        btnD.GetComponentInChildren<TextMeshProUGUI>().text = "D. " + data.answerD;

        SetButtonAnswerEvent(btnA, "A", cardIndex);
        SetButtonAnswerEvent(btnB, "B", cardIndex);
        SetButtonAnswerEvent(btnC, "C", cardIndex);
        SetButtonAnswerEvent(btnD, "D", cardIndex);
    }

    private void SetButtonAnswerEvent(Button btn, string choice, int cardIndex)
    {
        btn.onClick.RemoveAllListeners(); 
        btn.onClick.AddListener(() => CheckAnswer(choice, cardIndex));
    }

    public void CheckAnswer(string userChoice, int cardIndex)
    {
        string correctAns = questionDataList[cardIndex].correctAnswer;

        if (userChoice == correctAns)
        {
            AnswerCorrect(cardIndex);
        }
        else
        {
            AnswerWrong();
        }
    }

    private void AnswerCorrect(int cardIndex)
    {
        Debug.Log("Trả lời CHÍNH XÁC!");

        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayCorrect();
        }

        // 1. Đánh dấu thẻ bài này đã hoàn thành
        isCardCompleted[cardIndex] = true;

        // 2. Ẩn Khung chứa nội dung câu hỏi (con thứ nhất) đi để lộ bề mặt Card
        cardButtons[cardIndex].transform.GetChild(0).gameObject.SetActive(false);

        // 3. THAY THẾ trực tiếp Sprite gốc của Card_Btn bằng Sprite chiến thắng mới
        Image cardMainImage = cardButtons[cardIndex].GetComponent<Image>();
        if (cardMainImage != null && correctSprites != null && cardIndex < correctSprites.Length && correctSprites[cardIndex] != null)
        {
            cardMainImage.sprite = correctSprites[cardIndex]; // Đổi hẳn ruột ảnh gốc
        }

        // Ẩn bảng đáp án dùng chung
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
            if (AudioManagers.Instance != null)
            {
                AudioManagers.Instance.PlayWrong();
            }

            starFails[currentFails].SetActive(true);
            currentFails++;
            Debug.Log("Trả lời SAI RỒI!");

            if (currentFails >= maxFails)
            {
                Debug.Log("GAME OVER!");
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
        Debug.Log($"Số mảnh xếp đúng hiện tại: {correctPiecesCount} / {totalQuestions}");

        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayCorrect();
        }

        if (correctPiecesCount >= totalQuestions)
        {
            Debug.Log("CHÚC MỪNG! BẠN ĐÃ CHIẾN THẮNG GAME!");
            Invoke("TriggerWinUI", 0.5f);
        }
    }

    private void TriggerWinUI()
    {
        PlayerPrefs.SetInt("Level_1_Completed", 1);
        PlayerPrefs.SetInt("Unlocked_Photo_1", 1);
        PlayerPrefs.Save();

        Debug.Log("Đã lưu tiến trình: Hoàn thành màn 1 & Mở khóa ảnh bộ sưu tập!");
        UIManager.Instance.ShowYouWinPanel();
    }
}