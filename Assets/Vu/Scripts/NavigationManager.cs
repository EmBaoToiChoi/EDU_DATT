using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class NavigationManager : MonoBehaviour
{
    // Singleton để Manager không bị xóa khi đổi Scene
    public static NavigationManager Instance { get; private set; }

    [Header("Cấu hình Scene")]
    public string homeSceneName = "HomeScene"; // Bạn có thể đổi tên này trên Inspector

    // Stack lưu trữ lịch sử các Scene đã đi qua
    private Stack<string> sceneHistory = new Stack<string>();

    private void Awake()
    {
        // Kiểm tra Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ Manager tồn tại mãi mãi
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- HÀM HOME_BTN ---
    public void GoHome()
    {
        // Lưu lại scene hiện tại vào lịch sử trước khi đi
        SaveHistory();
        SceneManager.LoadScene(homeSceneName);
    }

    // --- HÀM PLAY_BTN (Chuyển đến Scene tiếp theo trong Build Settings) ---
    public void PlayNextLevel()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        
        // Kiểm tra xem có còn scene tiếp theo không
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SaveHistory();
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.LogWarning("Đã hết Level để chơi!");
        }
    }

    // --- HÀM BACK_BTN (Quay về scene trước đó) ---
    public void GoBack()
    {
        if (sceneHistory.Count > 0)
        {
            string lastScene = sceneHistory.Pop();
            SceneManager.LoadScene(lastScene);
        }
        else
        {
            Debug.Log("Không còn lịch sử để quay lại!");
            // Nếu không có lịch sử, mặc định về Home
            SceneManager.LoadScene(homeSceneName);
        }
    }

    // Hàm phụ trợ lưu lịch sử
    private void SaveHistory()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        sceneHistory.Push(currentScene);
    }

    // Hàm Load một scene bất kỳ (dùng cho các mục đích khác)
    public void LoadSpecificScene(string sceneName)
    {
        SaveHistory();
        SceneManager.LoadScene(sceneName);
    }
}

// ### 2. Cách thiết lập trên Unity để Manager chạy đúng

// 1. **Tạo Object Manager:** Trong Scene đầu tiên (ví dụ Scene Menu), tạo một GameObject trống tên là `NavigationManager` và gắn script trên vào.
// 2. **Cấu hình Inspector:** Tại ô `Home Scene Name`, hãy nhập đúng tên Scene chính của bạn (ví dụ: `MainMenu`, `Lobby`,...).
// 3. **Gán sự kiện cho các Button:**
//    - Chọn nút **Home**, kéo Object `NavigationManager` vào ô `OnClick()`, chọn hàm `NavigationManager -> GoHome`.
//    - Chọn nút **Back**, chọn hàm `NavigationManager -> GoBack`.
//    - Chọn nút **Play**, chọn hàm `NavigationManager -> PlayNextLevel`.
// 4. **Build Settings:** Đừng quên nhấn `Ctrl + Shift + B` và kéo tất cả các Scene của bạn vào danh sách Build, sắp xếp thứ tự các Level từ trên xuống dưới để nút **Play** hoạt động chính xác.

// Bộ khung này cực kỳ mạnh mẽ vì nó sử dụng **Stack**, giúp người chơi có thể bấm Back liên tục nhiều lần để quay lại đúng lộ trình họ đã đi. Hy vọng các slide và code này giúp dự án của bạn chuyên nghiệp hơn!