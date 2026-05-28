using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MainGameManager : MonoBehaviour 
{
    public GameObject quizPanel;
    public TextMeshProUGUI questionText;
    public TMP_InputField inputField;
    public TextMeshProUGUI scoreText;

    private int score = 0;
    private PuzzlePiece currentPiece;
    private string currentAnswer;

    // Kho câu hỏi gốc
    private List<(string q, string a)> englishQuizMaster = new List<(string, string)>() {
        ("What color is the SUN? (blue / yellow / red)", "yellow"),
        ("A small animal that loves cheese is a ___.", "mouse"),
        ("How many legs does an ant have? (Write number)", "6"),
        ("Opposite of 'HOT' is ___?", "cold"),
        ("What is the first letter of 'APPLE'?", "a"),
        ("How many days are there in a week? (Write number)", "7")
    };

    // Danh sách dùng để chạy trong màn chơi (sẽ bị trừ dần đi để không lặp)
    private List<(string q, string a)> activeQuizList;

    void Start() {
        if(quizPanel != null) quizPanel.SetActive(false);
        
        // Sao chép toàn bộ câu hỏi từ kho gốc vào danh sách hoạt động khi bắt đầu game
        activeQuizList = new List<(string, string)>(englishQuizMaster);
        
        UpdateUI();
    }

    public void ShowQuiz(PuzzlePiece piece) {
        currentPiece = piece;

        // Nếu lỡ dùng hết câu hỏi, tự động nạp lại kho gốc để tránh lỗi treo game
        if (activeQuizList.Count == 0) {
            activeQuizList = new List<(string, string)>(englishQuizMaster);
        }

        // 1. Chọn ngẫu nhiên một chỉ số (Index) trong danh sách hiện tại
        int randomIndex = Random.Range(0, activeQuizList.Count);
        var selectedPair = activeQuizList[randomIndex];

        // 2. Hiển thị câu hỏi lên màn hình
        questionText.text = selectedPair.q;
        currentAnswer = selectedPair.a.ToLower().Trim();
        inputField.text = "";
        quizPanel.SetActive(true);

        // 3. XÓA LUÔN câu hỏi này khỏi danh sách hoạt động để không bao giờ bị lặp lại
        activeQuizList.RemoveAt(randomIndex);
    }

    public void OnSubmitAnswer() {
        if (inputField.text.ToLower().Trim() == currentAnswer) {
            score += 10;
            currentPiece.Unlock();
            quizPanel.SetActive(false);
            UpdateUI();
        } else {
            inputField.text = ""; // Trả lời sai thì xóa chữ để nhập lại
        }
    }

    void UpdateUI() {
        if(scoreText != null) scoreText.text = "English Score: " + score;
    }
}