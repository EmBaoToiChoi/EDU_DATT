using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Main Panels")]
    public GameObject chooseCardPanel;
    public GameObject xepHinhPanel;
    public GameObject gameOverPanel;
    public GameObject youWinPanel; 

    [Header("Cấu hình thời gian đếm ngược")]
    public TextMeshProUGUI timerText; 
    public float timeRemaining = 60f; 
    
    private bool isTimerRunning = false;

    [Header("UI Bật/Tắt Âm Thanh (Mới)")]
    [Tooltip("Kéo thả Object audioOFF_Btn (ảnh OFF) vào đây")]
    public GameObject audioOffButtonObject; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ShowChooseCardPanel();
        
        // Cập nhật giao diện hiển thị nút âm thanh đúng theo trạng thái đã lưu từ trước khi vừa vào game
        UpdateAudioButtonUI();
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay(timeRemaining);
            }
            else
            {
                timeRemaining = 0;
                isTimerRunning = false;
                UpdateTimerDisplay(0);
                ShowGameOverPanel(); 
            }
        }
    }

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
        isTimerRunning = false; 
    }

    public void ShowXepHinhPanel()
    {
        chooseCardPanel.SetActive(false);
        xepHinhPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        youWinPanel.SetActive(false); 
        isTimerRunning = true; 
    }

    public void ShowGameOverPanel()
    {
        gameOverPanel.SetActive(true);
        isTimerRunning = false; 

        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayWrong();
        }
    }

    public void ShowYouWinPanel()
    {
        youWinPanel.SetActive(true);
        isTimerRunning = false; 

        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayWin();
        }
    }

    // CHỨC NĂNG: Dành riêng cho nút Bật / Tắt toàn bộ âm thanh game
    public void Btn_ToggleAudio()
    {
        if (AudioManagers.Instance != null)
        {
            // Tiến hành đảo trạng thái âm thanh âm thanh (Mute <-> Unmute)
            AudioManagers.Instance.ToggleMute();
            
            // Cập nhật ngay lập tức việc ẩn/hiện hình ảnh nút OFF đè lên nút ON
            UpdateAudioButtonUI();

            // Nếu sau khi nhấn mà game không bị Mute -> Phát thử tiếng click phản hồi
            if (!AudioManagers.Instance.IsMuted())
            {
                AudioManagers.Instance.PlayClick();
            }
        }
    }

    // Hàm phụ trợ tự động đồng bộ giao diện nút bấm âm thanh
    private void UpdateAudioButtonUI()
    {
        if (audioOffButtonObject != null && AudioManagers.Instance != null)
        {
            // Nếu game đang bị Mute (Tắt tiếng) -> Hiện nút Audio OFF lên để che ảnh ON
            // Nếu game đang bật tiếng -> Ẩn nút Audio OFF đi để lộ ảnh ON ra
            audioOffButtonObject.SetActive(AudioManagers.Instance.IsMuted());
        }
    }

    public void Btn_Again()
    {
        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayClick();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Btn_Home()
    {
        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayClick();
        }

        SceneManager.LoadScene("choose level"); 
    }
}