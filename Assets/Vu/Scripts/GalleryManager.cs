using UnityEngine;
using UnityEngine.UI;

public class GalleryManager : MonoBehaviour
{
    [Header("Danh sách ảnh trong Bộ sưu tập")]
    public GameObject[] photoItems; // Các Object chứa ảnh thật
    public GameObject[] lockIcons;  // Các Object hình Ổ khóa che lên trên ảnh thật

    private void Start()
    {
        // Kiểm tra trạng thái của ảnh 1
        if (PlayerPrefs.GetInt("Unlocked_Photo_1", 0) == 1)
        {
            // Đã mở khóa: Hiện ảnh thật, ẩn ổ khóa đi
            photoItems[0].SetActive(true);
            lockIcons[0].SetActive(false);
        }
        else
        {
            // Chưa mở khóa: Ẩn ảnh thật (hoặc làm mờ), hiện ổ khóa che lên
            photoItems[0].SetActive(false); // Hoặc bạn có thể đổi màu tối đi tùy ý
            lockIcons[0].SetActive(true);
        }

        // Bạn có thể làm tương tự với "Unlocked_Photo_2", "Unlocked_Photo_3" bằng vòng lặp sau này...
    }

    // Hàm hỗ trợ xóa dữ liệu để bạn test game lại từ đầu (Gắn vào 1 nút Cheat nếu muốn)
    public void ResetAllData()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Đã xóa hết dữ liệu game để test lại!");
    }
}