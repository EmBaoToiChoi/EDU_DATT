using UnityEngine;
using UnityEngine.UI;
using TMPro; // Khai báo thư viện để sử dụng TextMesh Pro

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
    public TextMeshProUGUI[] questionTexts; // Đã đổi sang kiểu TextMeshProUGUI cho 8 câu hỏi
    public GameObject[] pieceLocks;    // 8 cái Piece_Lock_img
    public GameObject[] pieceUnlocks;  // 8 cái Piece_UnLock_img

    [Header("Khung Đáp Án Dùng Chung (Kéo từ Inspector)")]
    public GameObject answerPanel;     // Kéo Object AnswerPanel vào đây
    public Button btnA;                // Kéo Object Btn_A vào đây
    public Button btnB;                // Kéo Object Btn_B vào đây
    public Button btnC;                // Kéo Object Btn_C vào đây
    public Button btnD;                // Kéo Object Btn_D vào đây

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

    private void Start()
    {
        foreach (GameObject star in starFails) star.SetActive(false);
        foreach (GameObject piece in pieceUnlocks) piece.SetActive(false);
        foreach (GameObject lockImg in pieceLocks) lockImg.SetActive(true);

        // Ban đầu ẩn toàn bộ các khung Image câu hỏi của từng Card
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] != null && cardButtons[i].transform.childCount > 0)
            {
                cardButtons[i].transform.GetChild(0).gameObject.SetActive(false);
            }
        }

        // Ban đầu ẩn luôn cả AnswerPanel dùng chung
        if (answerPanel != null) answerPanel.SetActive(false);
    }

    public void OnClickCard(int cardIndex)
    {
        if (currentOpeningCardIndex == cardIndex) return;

        if (currentOpeningCardIndex != -1)
        {
            cardButtons[currentOpeningCardIndex].transform.GetChild(0).gameObject.SetActive(false);
        }

        currentOpeningCardIndex = cardIndex;

        // 1. Bật Khung Image câu hỏi của Card_Btn này lên
        GameObject questionImage = cardButtons[cardIndex].transform.GetChild(0).gameObject;
        questionImage.SetActive(true);

        // 2. Bật AnswerPanel dùng chung
        answerPanel.SetActive(true);

        // 3. Lấy Data câu hỏi tương ứng
        QuestionData data = questionDataList[cardIndex];

        // 4. Gán chữ vào QuetionText (TextMeshPro)
        questionTexts[cardIndex].text = data.questionText;

        // 5. Gán chữ vào các nút A, B, C, D dùng chung (Tìm Component TextMeshProUGUI nằm bên trong nút)
        btnA.GetComponentInChildren<TextMeshProUGUI>().text = "A. " + data.answerA;
        btnB.GetComponentInChildren<TextMeshProUGUI>().text = "B. " + data.answerB;
        btnC.GetComponentInChildren<TextMeshProUGUI>().text = "C. " + data.answerC;
        btnD.GetComponentInChildren<TextMeshProUGUI>().text = "D. " + data.answerD;

        // 6. Cài đặt sự kiện click cho các nút trả lời
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

        cardButtons[cardIndex].transform.GetChild(0).gameObject.SetActive(false);
        answerPanel.SetActive(false);
        currentOpeningCardIndex = -1; 

        cardButtons[cardIndex].GetComponent<Button>().interactable = false;

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

    private int correctPiecesCount = 0; // Biến đếm số mảnh đã xếp đúng

    // Hàm này sẽ được gọi từ script DragDropPiece mỗi khi có 1 mảnh xếp đúng
    public void OnPiecePlacedCorrectly()
    {
        correctPiecesCount++;
        Debug.Log($"Số mảnh xếp đúng hiện tại: {correctPiecesCount} / {totalQuestions}");

        // Nếu xếp đúng đủ cả 8 mảnh -> CHIẾN THẮNG!
        if (correctPiecesCount >= totalQuestions)
        {
            Debug.Log("CHÚC MỪNG! BẠN ĐÃ CHIẾN THẮNG GAME!");
            // Đợi 0.5 giây cho hiệu ứng mượt rồi bật Panel Win
            Invoke("TriggerWinUI", 0.5f);
        }
    }

    private void TriggerWinUI()
    {
        // 1. Lưu trạng thái: Đã hoàn thành Màn 1 (Giá trị 1 nghĩa là True)
        PlayerPrefs.SetInt("Level_1_Completed", 1);

        // 2. Lưu trạng thái: Đã mở khóa Ảnh 1 trong Bộ sưu tập
        PlayerPrefs.SetInt("Unlocked_Photo_1", 1);

        // Lưu lại dữ liệu xuống ổ cứng thiết bị ngay lập tức
        PlayerPrefs.Save();

        Debug.Log("Đã lưu tiến trình: Hoàn thành màn 1 & Mở khóa ảnh bộ sưu tập!");

        // Hiện Panel Win như cũ
        UIManager.Instance.ShowYouWinPanel();
    }
}