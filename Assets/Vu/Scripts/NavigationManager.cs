using UnityEngine;
using UnityEngine.SceneManagement;

public class NavigationManager : MonoBehaviour
{
    // Singleton đơn giản để các nút ở Scene hiện tại truy cập nhanh
    public static NavigationManager Instance { get; private set; }

    [Header("Cấu hình Scene")]
    public string homeSceneName = "choose level"; // Đã đổi mặc định thành "choose level" giống UIManager của bạn

    private void Awake()
    {
        // Gán Instance trực tiếp cho đối tượng trong Scene hiện tại
        Instance = this;
    }

    // --- HÀM HOME_BTN ---
    public void GoHome()
    {
        // Phát tiếng click chuột nếu có AudioManagers trong màn
        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayClick();
        }

        SaveHistory();
        SceneManager.LoadScene(homeSceneName);
    }

    // --- HÀM PLAY_BTN (Chuyển đến Scene tiếp theo trong Build Settings) ---
    public void PlayNextLevel()
    {
        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayClick();
        }

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        
        // Kiểm tra xem có còn scene tiếp theo không
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SaveHistory();
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("Đã hết Level để chơi!");
        }
    }

    // --- HÀM BACK_BTN (Quay về scene trước đó) ---
    public void GoBack()
    {
        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayClick();
        }

        // Lấy tên Scene trước đó từ PlayerPrefs
        string lastScene = PlayerPrefs.GetString("LastActiveScene", "");

        if (!string.IsNullOrEmpty(lastScene))
        {
            // Trước khi quay lại, xóa dữ liệu cũ để tránh việc bấm Back vô hạn giữa 2 Scene
            PlayerPrefs.SetString("LastActiveScene", "");
            PlayerPrefs.Save();

            SceneManager.LoadScene(lastScene);
        }
        else
        {
            Debug.Log("Không có lịch sử! Mặc định quay về màn chọn Level.");
            SceneManager.LoadScene(homeSceneName);
        }
    }

    // Hàm Load một scene bất kỳ dựa vào tên cụ thể
    public void LoadSpecificScene(string sceneName)
    {
        if (AudioManagers.Instance != null)
        {
            AudioManagers.Instance.PlayClick();
        }

        SaveHistory();
        SceneManager.LoadScene(sceneName);
    }

    // Hàm phụ trợ lưu tên Scene hiện tại xuống thiết bị trước khi chuyển đi
    private void SaveHistory()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("LastActiveScene", currentScene);
        PlayerPrefs.Save();
    }
}