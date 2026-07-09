using UnityEngine;
using UnityEngine.UI;

public class GalleryManager : MonoBehaviour
{
    [Header("Danh sách ảnh trong Bộ sưu tập")]
    public GameObject[] photoItems; // Các Object chứa ảnh thật
    public GameObject[] lockIcons;  // Các Object hình Ổ khóa che lên trên ảnh thật

    [Header("Hiển thị ảnh lớn khi click")]
    public GameObject fullImagePanel; // Panel chứa ảnh lớn
    public Image fullImageDisplay;   // Image component để hiện ảnh lớn
    public Sprite[] fullPhotoSprites; // Sprite ảnh full tương ứng với mỗi photoItem
    [Header("Debug (dev only)")]
    public bool debugForceReparent = false;

    private void Start()
    {
        Debug.Log($"GalleryManager Start: gameObject activeInHierarchy={gameObject.activeInHierarchy}");
        Debug.Log($"photoItems count={photoItems.Length}, lockIcons count={lockIcons.Length}");

        if (debugForceReparent)
        {
            ForceReparentLocksToCanvas();
        }
        UpdateGalleryUnlocks();

        if (fullImagePanel != null)
        {
            fullImagePanel.SetActive(false);
        }
    }

    private void UpdateGalleryUnlocks()
    {
        if (photoItems == null || lockIcons == null)
        {
            Debug.LogWarning("GalleryManager: UpdateGalleryUnlocks called but photoItems or lockIcons is null.");
            return;
        }

        int count = Mathf.Min(photoItems.Length, lockIcons.Length);
        for (int i = 0; i < count; i++)
        {
            // Use either Unlocked_Photo_n OR Level_n_Completed to decide
            int photoKey = PlayerPrefs.GetInt($"Unlocked_Photo_{i + 1}", -1);
            int levelKey = PlayerPrefs.GetInt($"Level_{i + 1}_Completed", 0);
            bool unlocked = (photoKey == 1) || (levelKey == 1);

            if (photoItems[i] != null)
            {
                // ensure thumbnail object active so lock icon can overlay it
                photoItems[i].SetActive(true);

                // if thumbnail Image has no sprite, try assign full sprite for visibility
                Image thumbImg = photoItems[i].GetComponent<Image>();
                if (thumbImg != null && thumbImg.sprite == null && fullPhotoSprites != null && i < fullPhotoSprites.Length)
                {
                    if (fullPhotoSprites[i] != null)
                    {
                        thumbImg.sprite = fullPhotoSprites[i];
                        thumbImg.enabled = true;
                    }
                }
            }

            if (lockIcons[i] != null)
            {
                lockIcons[i].SetActive(!unlocked);

                // bring lock icon forward if same parent as thumbnail
                if (lockIcons[i].transform.parent == photoItems[i]?.transform)
                {
                    lockIcons[i].transform.SetAsLastSibling();
                }
            }

            string photoParent = photoItems[i] != null && photoItems[i].transform.parent != null
                ? photoItems[i].transform.parent.name
                : "null";
            string lockParent = lockIcons[i] != null && lockIcons[i].transform.parent != null
                ? lockIcons[i].transform.parent.name
                : "null";

            Debug.Log($"Gallery item {i + 1}: unlocked={unlocked}, photoItems active={(photoItems[i] != null ? photoItems[i].activeSelf.ToString() : "null")}, photoParent={photoParent}, lockIcon active={(lockIcons[i] != null ? lockIcons[i].activeSelf.ToString() : "null")}, lockParent={lockParent}");
        }
    }

    public void ShowFullPhoto(int index)
    {
        if (index < 0 || index >= photoItems.Length)
        {
            Debug.LogWarning($"GalleryManager: index {index} ngoài phạm vi.");
            return;
        }

        bool unlocked = PlayerPrefs.GetInt($"Unlocked_Photo_{index + 1}", 0) == 1;
        if (!unlocked)
        {
            Debug.Log("Ảnh chưa mở khóa, không thể xem full.");
            return;
        }

        if (fullImagePanel == null || fullImageDisplay == null)
        {
            Debug.LogWarning("GalleryManager: Thiếu fullImagePanel hoặc fullImageDisplay.");
            return;
        }

        if (index >= 0 && index < fullPhotoSprites.Length && fullPhotoSprites[index] != null)
        {
            fullImageDisplay.sprite = fullPhotoSprites[index];
        }
        else
        {
            Debug.LogWarning($"GalleryManager: Chưa gán fullPhotoSprites[{index}] hoặc index vượt quá.");
        }

        fullImagePanel.SetActive(true);
    }

    public void CloseFullPhoto()
    {
        if (fullImagePanel != null)
        {
            fullImagePanel.SetActive(false);
        }
    }

    [ContextMenu("Force Reparent Locks To Canvas (Debug)")]
    private void ForceReparentLocksToCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("GalleryManager: Không tìm thấy Canvas để reparent lock icons.");
            return;
        }

        int count = Mathf.Min(photoItems.Length, lockIcons.Length);
        for (int i = 0; i < count; i++)
        {
            if (lockIcons[i] == null) continue;
            lockIcons[i].transform.SetParent(canvas.transform, false);
            lockIcons[i].transform.SetAsLastSibling();
            Debug.Log($"Reparented lockIcon {i+1} to Canvas; activeInHierarchy={lockIcons[i].activeInHierarchy}");
        }
    }

    [ContextMenu("Print Unlocked Photo Keys (Debug)")]
    private void PrintUnlockedPhotoKeys()
    {
        int count = photoItems != null ? photoItems.Length : 0;
        for (int i = 0; i < count; i++)
        {
            int val = PlayerPrefs.GetInt($"Unlocked_Photo_{i + 1}", 0);
            Debug.Log($"PlayerPrefs: Unlocked_Photo_{i + 1} = {val}");
        }
    }

    // Hàm hỗ trợ xóa dữ liệu để bạn test game lại từ đầu (Gắn vào 1 nút Cheat nếu muốn)
    public void ResetAllData()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Đã xóa hết dữ liệu game để test lại!");
    }

    [ContextMenu("Apply Full Sprites To Thumbnails (Debug)")]
    private void ApplyFullSpritesToThumbnails()
    {
        if (photoItems == null || fullPhotoSprites == null)
        {
            Debug.LogWarning("GalleryManager: photoItems hoặc fullPhotoSprites chưa gán.");
            return;
        }

        int count = Mathf.Min(photoItems.Length, fullPhotoSprites.Length);
        for (int i = 0; i < count; i++)
        {
            if (photoItems[i] == null) continue;
            Image img = photoItems[i].GetComponent<Image>();
            if (img == null)
            {
                Debug.LogWarning($"Thumbnail {i+1} không có Image component.");
                continue;
            }

            if (fullPhotoSprites[i] != null)
            {
                img.sprite = fullPhotoSprites[i];
                img.enabled = true;
                Debug.Log($"Applied fullPhotoSprites[{i}] to thumbnail {i+1}.");
            }
            else
            {
                Debug.LogWarning($"fullPhotoSprites[{i}] chưa gán.");
            }
        }
    }

    [ContextMenu("Show Lock Objects (Debug)")]
    private void ShowLockObjectsDebug()
    {
        int count = Mathf.Min(photoItems.Length, lockIcons.Length);
        for (int i = 0; i < count; i++)
        {
            if (photoItems[i] != null)
            {
                // đảm bảo photo hiển thị
                photoItems[i].SetActive(true);
            }

            if (lockIcons[i] != null)
            {
                // bật lock icon để dễ kiểm tra
                lockIcons[i].SetActive(true);
                // đưa lên trên cùng nếu cùng parent
                if (lockIcons[i].transform.parent == photoItems[i]?.transform)
                {
                    lockIcons[i].transform.SetAsLastSibling();
                }
                Debug.Log($"[Debug] Force show lockIcon for item {i+1}: parent={(lockIcons[i].transform.parent!=null?lockIcons[i].transform.parent.name:"null")}");
            }
        }
    }
}