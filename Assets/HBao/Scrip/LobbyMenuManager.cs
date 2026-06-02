using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LobbyMenuManager : MonoBehaviour
{
    [Header("Camera Scrolling Settings")]
    [SerializeField] private Transform targetCamera;       // Camera cần cuộn (nếu trống sẽ tự lấy Camera.main)
    [SerializeField] private float startX = -10f;          // Tọa độ X bắt đầu
    [SerializeField] private float endX = 0f;              // Tọa độ X kết thúc
    [SerializeField] private float scrollDuration = 3f;     // Thời gian cuộn (giây)
    [SerializeField] private AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Easing chuyển động

    [Header("UI Panels")]
    [SerializeField] private GameObject lobbyMenuUI;       // Menu Lobby (chứa các nút Chơi, Cài đặt, Thoát)
    [SerializeField] private GameObject gamePlayCanvas;     // Canvas chơi game (chứa BoardManager...)
    [SerializeField] private GameObject settingsUI;         // UI Cài đặt (đè lên Lobby)

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button closeSettingsButton;    // Nút đóng cài đặt (X hoặc Back)

    private bool isScrolling = false;

    void Start()
    {
        // Tự động tìm Main Camera nếu chưa gán
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }

        // 1. Ẩn tất cả UI lúc bắt đầu để tập trung vào hiệu ứng cuộn camera
        if (lobbyMenuUI != null) lobbyMenuUI.SetActive(false);
        if (gamePlayCanvas != null) gamePlayCanvas.SetActive(false);
        if (settingsUI != null) settingsUI.SetActive(false);

        // Đặt kích thước các nút về 0 để chuẩn bị hiệu ứng xuất hiện sau đó
        if (playButton != null) playButton.transform.localScale = Vector3.zero;
        if (settingsButton != null) settingsButton.transform.localScale = Vector3.zero;
        if (exitButton != null) exitButton.transform.localScale = Vector3.zero;

        // 2. Đăng ký sự kiện click cho các nút
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);

        // 3. Bắt đầu cuộn camera từ trái sang phải
        if (targetCamera != null)
        {
            StartCoroutine(ScrollCameraRoutine());
        }
        else
        {
            Debug.LogError("LobbyMenuManager: Không tìm thấy Camera để cuộn!");
            if (lobbyMenuUI != null) lobbyMenuUI.SetActive(true);
        }
    }

    private IEnumerator ScrollCameraRoutine()
    {
        isScrolling = true;
        float elapsed = 0f;

        // Đặt camera về vị trí bắt đầu (giữ nguyên Y và Z ban đầu)
        Vector3 camPos = targetCamera.position;
        camPos.x = startX;
        targetCamera.position = camPos;

        while (elapsed < scrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scrollDuration);
            float evaluatedT = scrollCurve.Evaluate(t);

            // Nội suy vị trí X
            camPos.x = Mathf.Lerp(startX, endX, evaluatedT);
            targetCamera.position = camPos;

            yield return null;
        }

        // Đảm bảo camera dừng chính xác tại điểm cuối
        camPos.x = endX;
        targetCamera.position = camPos;
        isScrolling = false;

        // Hiển thị Menu Lobby sau khi cuộn xong
        if (lobbyMenuUI != null)
        {
            lobbyMenuUI.SetActive(true);
            // Chạy hiệu ứng hiển thị từng nút tuần tự
            StartCoroutine(AnimateAllButtonsEntrance());
        }
    }

    private void OnPlayClicked()
    {
        if (isScrolling) return;

        // Tắt Menu game
        if (lobbyMenuUI != null) lobbyMenuUI.SetActive(false);
        
        // Hiển thị Canva chơi (Bảng game sẽ tự động tạo vì BoardManager nằm trên Canvas này sẽ chạy Start())
        if (gamePlayCanvas != null) gamePlayCanvas.SetActive(true);
    }

    private void OnSettingsClicked()
    {
        if (isScrolling) return;

        // Bật UI cài đặt đè lên
        if (settingsUI != null) settingsUI.SetActive(true);
    }

    private void OnCloseSettingsClicked()
    {
        // Tắt UI cài đặt quay lại Lobby
        if (settingsUI != null) settingsUI.SetActive(false);
    }

    private void OnExitClicked()
    {
        if (isScrolling) return;

        Debug.Log("LobbyMenuManager: Thoát game!");
        Application.Quit();
    }

    private IEnumerator AnimateAllButtonsEntrance()
    {
        float delayBetween = 0.2f; // Thời gian giãn cách xuất hiện giữa các nút

        if (playButton != null)
        {
            StartCoroutine(AnimateButtonEntrance(playButton.GetComponent<RectTransform>()));
            yield return new WaitForSeconds(delayBetween);
        }

        if (settingsButton != null)
        {
            StartCoroutine(AnimateButtonEntrance(settingsButton.GetComponent<RectTransform>()));
            yield return new WaitForSeconds(delayBetween);
        }

        if (exitButton != null)
        {
            StartCoroutine(AnimateButtonEntrance(exitButton.GetComponent<RectTransform>()));
        }
    }

    private IEnumerator AnimateButtonEntrance(RectTransform btnTransform)
    {
        btnTransform.localScale = Vector3.zero;
        btnTransform.gameObject.SetActive(true);

        float elapsed = 0f;
        float duration = 0.5f; // Thời gian phóng to của nút

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Sử dụng hàm EaseOutBack để tạo hiệu ứng nảy nhẹ khi phóng to
            float scale = EaseOutBack(t);
            btnTransform.localScale = new Vector3(scale, scale, 1f);
            
            yield return null;
        }

        btnTransform.localScale = Vector3.one;
    }

    // Hàm EaseOutBack tạo chuyển động phóng to quá đà một chút rồi nảy nhẹ lại
    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
