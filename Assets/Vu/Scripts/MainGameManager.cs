using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MainGameManager : MonoBehaviour 
{
    [Header("Giao diện Câu hỏi & Điểm số")]
    public GameObject quizPanel;
    public TextMeshProUGUI questionText;
    public TMP_InputField inputField;
    public TextMeshProUGUI scoreText;

    [Header("Hệ thống Mạng (Trái tim)")]
    public List<GameObject> heartImages; 
    private int lives = 3; 

    [Header("Giao diện Kết quả (Bảng xếp xong)")]
    public GameObject resultPanel;         
    public TextMeshProUGUI resultTitleText; 
    [Tooltip("Ô Text này sẽ hiển thị cả Thời gian và Tổng Điểm khi kết thúc")]
    public TextMeshProUGUI timeResultText;  

    [Header("Auto Setup Matcher")]
    public List<RectTransform> allSlots; 

    private int score = 0;
    private PuzzlePiece currentPiece;
    private string currentAnswer;

    private float startTime;
    private bool isGameFinished = false;
    private List<PuzzlePiece> allPiecesInLevel = new List<PuzzlePiece>();

    // Kho câu hỏi gốc
    private List<(string q, string a)> englishQuizMaster = new List<(string, string)>() {
        ("What color is the SUN? (blue / yellow / red)", "yellow"),
        ("A small animal that loves cheese is a ___.", "mouse"),
        ("How many legs does an ant have? (Write number)", "6"),
        ("Opposite of 'HOT' is ___?", "cold"),
        ("What is the first letter of 'APPLE'?", "a"),
        ("How many days are there in a week? (Write number)", "7")
    };

    private List<(string q, string a)> activeQuizList;

    void Start() {
        if(quizPanel != null) quizPanel.SetActive(false);
        if(resultPanel != null) resultPanel.SetActive(false); 
        
        activeQuizList = new List<(string, string)>(englishQuizMaster);
        
        startTime = Time.time;
        isGameFinished = false;
        lives = 3;

        foreach (var heart in heartImages) {
            if(heart != null) heart.SetActive(true);
        }

        AutoAssignSlotsToPieces();
        UpdateUI();
    }

    void AutoAssignSlotsToPieces() {
        PuzzlePiece[] pieces = FindObjectsByType<PuzzlePiece>(FindObjectsSortMode.None);
        allPiecesInLevel.Clear();
        allPiecesInLevel.AddRange(pieces); 

        foreach (var piece in pieces) {
            bool foundMatch = false;
            foreach (var slot in allSlots) {
                if (slot.name.Contains(piece.pieceID.ToString())) {
                    piece.targetSlot = slot;
                    piece.manager = this; 
                    foundMatch = true;
                    break;
                }
            }
        }
    }

    public void ShowQuiz(PuzzlePiece piece) {
        if (isGameFinished) return; 
        currentPiece = piece;
        GetRandomQuestion();
    }

    void GetRandomQuestion() {
        if (activeQuizList.Count == 0) {
            activeQuizList = new List<(string, string)>(englishQuizMaster);
        }

        int randomIndex = Random.Range(0, activeQuizList.Count);
        var selectedPair = activeQuizList[randomIndex];

        questionText.text = selectedPair.q;
        currentAnswer = selectedPair.a.ToLower().Trim();
        inputField.text = "";
        quizPanel.SetActive(true);

        activeQuizList.RemoveAt(randomIndex);
    }

    public void OnSubmitAnswer() {
        if (isGameFinished) return;

        if (inputField.text.ToLower().Trim() == currentAnswer) {
            score += 10;
            currentPiece.Unlock();
            quizPanel.SetActive(false);
            UpdateUI();
        } else {
            lives--; 

            if (heartImages != null && lives >= 0 && lives < heartImages.Count) {
                if (heartImages[lives] != null) {
                    heartImages[lives].SetActive(false); 
                }
            }

            if (lives <= 0) {
                GameOver();
            } else {
                GetRandomQuestion(); 
            }
        }
    }

    public void CheckGameComplete() {
        foreach (var piece in allPiecesInLevel) {
            if (!piece.IsSnappedPiece()) return; 
        }

        // ----------------------------------------------------
        // TRƯỜNG HỢP: CHIẾN THẮNG (Hiện cả thời gian và điểm)
        // ----------------------------------------------------
        isGameFinished = true;
        quizPanel.SetActive(false);
        
        float totalTime = Time.time - startTime; 
        
        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if(resultTitleText != null) resultTitleText.text = "CHIẾN THẮNG! 🎉";
            
            // Thêm dòng hiển thị Điểm số vào UI kết quả
            if (timeResultText != null) {
                timeResultText.text = "Thời gian: " + FormatTime(totalTime) + "\nTổng điểm: " + score;
            }
        }
    }

    // ----------------------------------------------------
    // TRƯỜNG HỢP: THUA CUỘC (Hết mạng - Vẫn hiện điểm số đã đạt được)
    // ----------------------------------------------------
    void GameOver() {
        isGameFinished = true;
        quizPanel.SetActive(false); 

        float totalTime = Time.time - startTime;

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if(resultTitleText != null) resultTitleText.text = "GAME OVER! 😵";
            
            // Hiển thị điểm số hiện tại của bé trước khi bị hết tim
            if (timeResultText != null) {
                timeResultText.text = "Thời gian: " + FormatTime(totalTime) + "\nĐiểm đạt được: " + score;
            }
        }
    }

    string FormatTime(float timeInSeconds) {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void UpdateUI() {
        if(scoreText != null) scoreText.text = "English Score: " + score;
    }

    public void OnRestartGameButton() {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void OnExitGameButton() {
        Application.Quit(); 
    }
}