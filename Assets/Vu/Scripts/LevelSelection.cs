using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelection : MonoBehaviour
{
    [Header("Danh sách 5 Nút bấm chơi LEVEL")]
    public Button[] levelButtons;   // Kéo thả Level1_Btn, Level2_Btn... vào đây từ Element 0 đến 4

    private void Start()
    {
        ResetProgressOnLaunch();

        // 1. ĐẢM BẢO NÚT LEVEL 1 LUÔN LUÔN BẬT (Index 0)
        if (levelButtons.Length > 0 && levelButtons[0] != null) 
        {
            levelButtons[0].gameObject.SetActive(true); 
        }

        // 2. KIỂM TRA TỪ NÚT LEVEL 2 TRỞ ĐI (Bắt đầu từ i = 1)
        for (int i = 1; i < levelButtons.Length; i++)
        {
            bool unlockAllowed = true;

            // Yêu cầu: tất cả level nhỏ hơn i phải hoàn thành trước khi mở level i+1.
            for (int prev = 1; prev <= i; prev++)
            {
                string prevKey = "Level_" + prev + "_Completed";
                if (PlayerPrefs.GetInt(prevKey, 0) != 1)
                {
                    unlockAllowed = false;
                    break;
                }
            }

            if (levelButtons[i] != null)
            {
                levelButtons[i].gameObject.SetActive(unlockAllowed);
            }

            if (unlockAllowed)
            {
                Debug.Log($"Màn {i + 1} đã mở khóa -> Bật nút bấm.");
            }
            else
            {
                Debug.Log($"Màn {i + 1} vẫn khóa -> Ẩn nút bấm.");
            }
        }
    }

    private void ResetProgressOnLaunch()
    {
        bool hasLaunchedBefore = PlayerPrefs.GetInt("HasLaunchedBefore", 0) == 1;
        if (!hasLaunchedBefore)
        {
            PlayerPrefs.DeleteKey("Level_1_Completed");
            PlayerPrefs.DeleteKey("Level_2_Completed");
            PlayerPrefs.DeleteKey("Level_3_Completed");
            PlayerPrefs.DeleteKey("Unlocked_Photo_1");
            PlayerPrefs.DeleteKey("Unlocked_Photo_2");
            PlayerPrefs.DeleteKey("Unlocked_Photo_3");
            PlayerPrefs.SetInt("HasLaunchedBefore", 1);
            PlayerPrefs.Save();
            Debug.Log("Reset progress: game mới khởi chạy, chỉ mở Level 1.");
        }
        else
        {
            Debug.Log("Game đã khởi chạy trước đó, giữ progress hiện có.");
        }
    }

    [ContextMenu("Reset Level Progress")]
    private void ResetLevelProgressFromInspector()
    {
        PlayerPrefs.DeleteKey("Level_1_Completed");
        PlayerPrefs.DeleteKey("Level_2_Completed");
        PlayerPrefs.DeleteKey("Level_3_Completed");
        PlayerPrefs.DeleteKey("Unlocked_Photo_1");
        PlayerPrefs.DeleteKey("Unlocked_Photo_2");
        PlayerPrefs.DeleteKey("Unlocked_Photo_3");
        PlayerPrefs.Save();
        Debug.Log("Đã xóa tiến trình level trong PlayerPrefs.");
    }

    // Hàm chuyển Scene khi bấm nút Level
    public void SelectLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}