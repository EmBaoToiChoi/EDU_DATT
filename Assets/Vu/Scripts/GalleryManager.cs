using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GalleryManager : MonoBehaviour
{
    [Header("Danh sách ảnh trong Bộ sưu tập")]
    public GameObject[] photoItems; // Các Object chứa ảnh thật
    public GameObject[] lockIcons;  // Các Object hình Ổ khóa che lên trên ảnh thật

    [Header("Hiển thị ảnh lớn khi click")]
    public GameObject fullImagePanel; // Legacy: Panel chứa ảnh lớn nếu chỉ dùng 1 panel chung
    public Image fullImageDisplay;   // Legacy: Image component để hiện ảnh lớn nếu chỉ dùng 1 display chung
    public GameObject[] fullImagePanels; // Nhiều panel full image cho mỗi level
    public Image[] fullImageDisplays;   // Nhiều Image component cho mỗi level
    public Sprite[] fullPhotoSprites; // Sprite ảnh full tương ứng với mỗi photoItem

    [Header("Char theo mỗi ảnh lớn")]
    public GameObject[] fullPhotoCharacterObjects; // object char riêng cho mỗi full photo

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

        if (fullImagePanels != null)
        {
            for (int i = 0; i < fullImagePanels.Length; i++)
            {
                if (fullImagePanels[i] != null)
                {
                    fullImagePanels[i].SetActive(false);
                }
            }
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

        Debug.Log($"GalleryManager: ShowFullPhoto index={index}, level={index + 1}");
        HideAllFullPhotoCharacters();
        ShowFullPhotoCharacter(index);

        if (fullImagePanels != null && index >= 0 && index < fullImagePanels.Length && fullImagePanels[index] != null)
        {
            HideAllFullImagePanels();
            HideAllFullImageDisplays();
            if (index < fullPhotoSprites.Length && fullPhotoSprites[index] != null)
            {
                if (fullImageDisplays != null && index < fullImageDisplays.Length && fullImageDisplays[index] != null)
                {
                    fullImageDisplays[index].sprite = fullPhotoSprites[index];
                    fullImageDisplays[index].gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"GalleryManager: fullImageDisplays[{index}] chưa gán dù dùng fullImagePanels.");
                }
            }
            else
            {
                Debug.LogWarning($"GalleryManager: Chưa gán fullPhotoSprites[{index}] hoặc index vượt quá.");
            }
            fullImagePanels[index].SetActive(true);
        }
        else if (fullImageDisplays != null && index >= 0 && index < fullImageDisplays.Length && fullImageDisplays[index] != null)
        {
            HideAllFullImageDisplays();
            fullImageDisplays[index].sprite = null;
            if (index < fullPhotoSprites.Length && fullPhotoSprites[index] != null)
            {
                fullImageDisplays[index].sprite = fullPhotoSprites[index];
            }
            else
            {
                Debug.LogWarning($"GalleryManager: Chưa gán fullPhotoSprites[{index}] hoặc index vượt quá.");
            }
            fullImageDisplays[index].gameObject.SetActive(true);
        }
        else if (fullImagePanel != null && fullImageDisplay != null)
        {
            HideAllFullImageDisplays();
            fullImageDisplay.sprite = null;
            if (index >= 0 && index < fullPhotoSprites.Length && fullPhotoSprites[index] != null)
            {
                fullImageDisplay.sprite = fullPhotoSprites[index];
            }
            else
            {
                Debug.LogWarning($"GalleryManager: Chưa gán fullPhotoSprites[{index}] hoặc index vượt quá. fullImageDisplay sẽ bị xóa ảnh cũ.");
            }
            fullImagePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GalleryManager: Thiếu fullImagePanels/fullImageDisplays hoặc fullImagePanel/fullImageDisplay.");
        }
    }

    public void ShowFullPhotoByLevel(int level)
    {
        int index = level - 1;
        Debug.Log($"GalleryManager: ShowFullPhotoByLevel level={level}, index={index}");
        ShowFullPhoto(index);
    }

    public void ShowFullPhotoLevel1()
    {
        ShowFullPhotoByLevel(1);
    }

    public void ShowFullPhotoLevel2()
    {
        ShowFullPhotoByLevel(2);
    }

    public void ShowFullPhotoLevel3()
    {
        ShowFullPhotoByLevel(3);
    }

    public void ShowFullPhotoByButton(GameObject clickedThumbnail)
    {
        if (clickedThumbnail == null)
        {
            Debug.LogWarning("GalleryManager: clickedThumbnail là null.");
            return;
        }

        int index = System.Array.IndexOf(photoItems, clickedThumbnail);
        if (index < 0)
        {
            Debug.LogWarning($"GalleryManager: clickedThumbnail không tìm thấy trong photoItems: {clickedThumbnail.name}");
            return;
        }

        Debug.Log($"GalleryManager: ShowFullPhotoByButton clickedThumbnail={clickedThumbnail.name}, index={index}");
        ShowFullPhoto(index);
    }

    private void ShowFullPhotoCharacter(int index)
    {
        if (fullPhotoCharacterObjects == null)
        {
            Debug.LogWarning("GalleryManager: fullPhotoCharacterObjects chưa gán.");
            return;
        }
        if (index < 0 || index >= fullPhotoCharacterObjects.Length)
        {
            Debug.LogWarning($"GalleryManager: ShowFullPhotoCharacter index {index} ngoài phạm vi.");
            return;
        }
        if (fullPhotoCharacterObjects[index] == null)
        {
            Debug.LogWarning($"GalleryManager: fullPhotoCharacterObjects[{index}] là null.");
            return;
        }

        Debug.Log($"GalleryManager: Activate fullPhotoCharacterObjects[{index}] = {fullPhotoCharacterObjects[index].name}");
        fullPhotoCharacterObjects[index].SetActive(true);
    }

    private void HideAllFullPhotoCharacters()
    {
        if (fullPhotoCharacterObjects == null) return;

        for (int i = 0; i < fullPhotoCharacterObjects.Length; i++)
        {
            if (fullPhotoCharacterObjects[i] != null)
            {
                fullPhotoCharacterObjects[i].SetActive(false);
            }
        }
    }

    private void HideAllFullImageDisplays()
    {
        if (fullImageDisplays != null)
        {
            for (int i = 0; i < fullImageDisplays.Length; i++)
            {
                if (fullImageDisplays[i] != null)
                {
                    fullImageDisplays[i].gameObject.SetActive(false);
                }
            }
        }

        if (fullImagePanel != null)
        {
            fullImagePanel.SetActive(false);
        }
    }

    private void HideAllFullImagePanels()
    {
        if (fullImagePanels != null)
        {
            for (int i = 0; i < fullImagePanels.Length; i++)
            {
                if (fullImagePanels[i] != null)
                {
                    fullImagePanels[i].SetActive(false);
                }
            }
        }
    }

    public void CloseFullPhoto()
    {
        if (fullImagePanel != null)
        {
            fullImagePanel.SetActive(false);
        }

        HideAllFullImagePanels();
        HideAllFullImageDisplays();
        HideAllFullPhotoCharacters();
    }

    [ContextMenu("Force Reparent Locks To Canvas (Debug)")]
    private void ForceReparentLocksToCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            if (canvases != null && canvases.Length > 0)
            {
                canvas = canvases[0];
            }
        }

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
            Debug.Log($"Reparented lockIcon {i+1} to Canvas '{canvas.name}'; activeInHierarchy={lockIcons[i].activeInHierarchy}");
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