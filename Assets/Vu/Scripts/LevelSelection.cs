using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelection : MonoBehaviour
{
    [Header("Danh sách 5 Nút bấm chơi LEVEL")]
    public Button[] levelButtons;   // Kéo thả Level1_Btn, Level2_Btn... vào đây từ Element 0 đến 4

    private void Start()
    {
        // 1. ĐẢM BẢO NÚT LEVEL 1 LUÔN LUÔN BẬT (Index 0)
        if (levelButtons.Length > 0 && levelButtons[0] != null) 
        {
            levelButtons[0].gameObject.SetActive(true); 
        }

        // 2. KIỂM TRA TỪ NÚT LEVEL 2 TRỞ ĐI (Bắt đầu từ i = 1)
        for (int i = 1; i < levelButtons.Length; i++)
        {
            // Kiểm tra xem màn chơi ngay phía trước nó đã hoàn thành chưa?
            // i = 1 (Màn 2) -> kiểm tra key "Level_1_Completed"
            // i = 2 (Màn 3) -> kiểm tra key "Level_2_Completed"
            string keyCheck = "Level_" + i + "_Completed";

            if (PlayerPrefs.GetInt(keyCheck, 0) == 1)
            {
                // ---- ĐÃ MỞ KHÓA: Bật nút bấm lên để người chơi click chơi ----
                if (levelButtons[i] != null) levelButtons[i].gameObject.SetActive(true);
                Debug.Log($"Màn {i + 1} đã mở khóa -> Bật nút bấm.");
            }
            else
            {
                // ---- VẪN BỊ KHÓA: Ẩn nút bấm đi (Hình khóa bên dưới tự lộ ra) ----
                if (levelButtons[i] != null) levelButtons[i].gameObject.SetActive(false);
                Debug.Log($"Màn {i + 1} vẫn khóa -> Ẩn nút bấm.");
            }
        }
    }

    // Hàm chuyển Scene khi bấm nút Level
    public void SelectLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}