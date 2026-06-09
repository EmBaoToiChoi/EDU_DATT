using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Bắt buộc dùng TextMeshPro để hiển thị thời gian sắc nét

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Main Panels")]
    public GameObject chooseCardPanel;
    public GameObject xepHinhPanel;
    public GameObject gameOverPanel;
    public GameObject youWinPanel; 

    [Header("Cấu hình thời gian đếm ngược")]
    public TextMeshProUGUI timerText; // Kéo thả Text (TMP) hiển thị thời gian vào đây
    public float timeRemaining = 60f; // Thời gian giới hạn màn chơi (giây), chỉnh được trên Inspector
    
    private bool isTimerRunning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Khởi đầu game: Hiện màn hình chọn Card, ẩn tất cả các màn hình khác
        ShowChooseCardPanel();
    }

    private void Update()
    {
        // Logic chạy đồng hồ đếm ngược mỗi frame
        if (isTimerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay(timeRemaining);
            }
            else
            {
                // Khi thời gian hết (về 0)
                timeRemaining = 0;
                isTimerRunning = false;
                UpdateTimerDisplay(0);
                ShowGameOverPanel(); // Kích hoạt Game Over
            }
        }
    }

    // Hàm định dạng thời gian thành dạng Phút:Giây (Ví dụ: 01:30)
    private void UpdateTimerDisplay(float timeToDisplay)
    {
        if (timerText == null) return;

        float minutes = Mathf.FloorToInt(timeToDisplay / 60); 
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void ShowChooseCardPanel()
    {
        chooseCardPanel.SetActive(true);
        xepHinhPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        youWinPanel.SetActive(false); 
        
        // Dừng đếm thời gian khi đang ở màn hình chọn bài
        isTimerRunning = false; 
    }

    public void ShowXepHinhPanel()
    {
        chooseCardPanel.SetActive(false);
        xepHinhPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        youWinPanel.SetActive(false); 

        // BẮT ĐẦU ĐẾM NGƯỢC khi người chơi vào màn xếp hình
        isTimerRunning = true; 
    }

    public void ShowGameOverPanel()
    {
        gameOverPanel.SetActive(true);
        isTimerRunning = false; // Dừng đồng hồ khi thua

        // TỰ ĐỘNG PHÁT ÂM THANH THUA CUỘC VỚI VOL ĐỘC LẬP
        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayWrong();
        }
    }

    public void ShowYouWinPanel()
    {
        youWinPanel.SetActive(true);
        isTimerRunning = false; // Dừng đồng hồ khi thắng

        // TỰ ĐỘNG PHÁT ÂM THANH CHIẾN THẮNG VỚI VOL ĐỘC LẬP
        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayWin();
        }
    }

    // Chức năng nút "Again" (Chơi lại)
    public void Btn_Again()
    {
        // Phát tiếng click chuột độc lập trước khi load lại
        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayClick();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Chức năng nút "Home" (Về màn hình chọn level)
    public void Btn_Home()
    {
        // Phát tiếng click chuột độc lập
        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayClick();
        }

        SceneManager.LoadScene("choose level"); 
    }
}