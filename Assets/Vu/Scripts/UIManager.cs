using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Main Panels")]
    public GameObject chooseCardPanel;
    public GameObject xepHinhPanel;
    public GameObject gameOverPanel;
    public GameObject youWinPanel; // Kéo Panel You Win của bạn vào đây

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

    public void ShowChooseCardPanel()
    {
        chooseCardPanel.SetActive(true);
        xepHinhPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        youWinPanel.SetActive(false); // Cập nhật: Ẩn panel win khi về màn chọn bài
    }

    public void ShowXepHinhPanel()
    {
        chooseCardPanel.SetActive(false);
        xepHinhPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        youWinPanel.SetActive(false); // Cập nhật: Ẩn panel win khi vào màn xếp hình
    }

    public void ShowGameOverPanel()
    {
        // Giữ nguyên màn hình hiện tại nhưng bật đè Game Over Panel lên
        gameOverPanel.SetActive(true);
    }

    // ĐÃ BỔ SUNG: Hàm bật màn hình chiến thắng khi xếp đúng hết các mảnh
    public void ShowYouWinPanel()
    {
        youWinPanel.SetActive(true);
    }

    // Chức năng nút "Again" (Chơi lại)
    public void Btn_Again()
    {
        // Load lại Scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Chức năng nút "Home" (Về màn hình chọn level)
    public void Btn_Home()
    {
        // Thay "ChooseLevelScene" bằng tên Scene chọn level thực tế của bạn
        SceneManager.LoadScene("ChooseLevelScene"); 
    }
}