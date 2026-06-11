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
    [SerializeField] private Button gameplaySettingsButton; // Nút cài đặt trong màn chơi (mới thêm)

    private bool isScrolling = false;
    private Vector3 playButtonOriginalScale = Vector3.one;
    private Vector3 settingsButtonOriginalScale = Vector3.one;
    private Vector3 exitButtonOriginalScale = Vector3.one;

    void Awake()
    {
        // 1. Tự động kiểm tra xem có EventSystem nào trong Scene không, nếu không có sẽ tự tạo
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var existingEventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem == null)
            {
                Debug.LogWarning("[LobbyMenuManager UI Debug] Không tìm thấy EventSystem trong Scene! Đã tự động tạo EventSystem_AutoCreated mới để nút bấm có thể hoạt động.");
                GameObject eventSystemGo = new GameObject("EventSystem_AutoCreated");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        // 2. Kiểm tra GraphicRaycaster trên Canvas chứa script này
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogError($"[LobbyMenuManager UI Debug] LỖI: Canvas '{canvas.name}' không có component GraphicRaycaster! Các nút bấm sẽ KHÔNG THỂ click/hover được.");
            }
            else if (!raycaster.enabled)
            {
                Debug.LogWarning($"[LobbyMenuManager UI Debug] CẢNH BÁO: Component GraphicRaycaster trên Canvas '{canvas.name}' đang bị TẮT! Hãy bật nó lên trong Inspector.");
            }
        }
    }

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

        // 2. Lưu lại scale thiết kế ban đầu trong Editor để tránh lỗi ghi đè Scale 0
        if (playButton != null) playButtonOriginalScale = playButton.transform.localScale;
        if (settingsButton != null) settingsButtonOriginalScale = settingsButton.transform.localScale;
        if (exitButton != null) exitButtonOriginalScale = exitButton.transform.localScale;

        // Đặt kích thước các nút về 0 để chuẩn bị hiệu ứng xuất hiện sau đó
        if (playButton != null) playButton.transform.localScale = Vector3.zero;
        if (settingsButton != null) settingsButton.transform.localScale = Vector3.zero;
        if (exitButton != null) exitButton.transform.localScale = Vector3.zero;

        // 3. Đăng ký sự kiện click cho các nút
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);
        if (gameplaySettingsButton != null) gameplaySettingsButton.onClick.AddListener(OnSettingsClicked);

        // 4. Bắt đầu cuộn camera từ trái sang phải
        if (targetCamera != null)
        {
            StartCoroutine(ScrollCameraRoutine());
        }
        else
        {
            Debug.LogError("LobbyMenuManager: Không tìm thấy Camera để cuộn!");
            if (lobbyMenuUI != null)
            {
                lobbyMenuUI.SetActive(true);
                StartCoroutine(AnimateAllButtonsEntrance());
            }
        }
    }

    void Update()
    {
        // Log chẩn đoán kiểm tra va chạm UI (Raycast) khi người dùng click chuột
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
        {
            LogRaycastUnderMouse(UnityEngine.InputSystem.Pointer.current.position.ReadValue());
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            LogRaycastUnderMouse(Input.mousePosition);
        }
#endif
    }

    private void LogRaycastUnderMouse(Vector2 screenPosition)
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogError("[LobbyMenuManager UI Debug] Không thể click vì không tìm thấy EventSystem trong Scene!");
            return;
        }

        var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = screenPosition
        };

        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            Debug.Log($"[LobbyMenuManager UI Debug] Bạn đã click chuột. Vật thể UI nhận click trên cùng là: <color=yellow><b>{results[0].gameObject.name}</b></color> (Canvas: {results[0].gameObject.GetComponentInParent<Canvas>()?.name})");
            for (int i = 1; i < results.Count; i++)
            {
                Debug.Log($"   -> Phía sau nó có: {results[i].gameObject.name}");
            }
        }
        else
        {
            Debug.Log("[LobbyMenuManager UI Debug] Bạn đã click chuột, nhưng không trúng bất kỳ vật thể UI nào (không trúng Raycast). Hãy kiểm tra xem nút bấm có bật 'Raycast Target' trong component Image không.");
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

        Debug.Log("LobbyMenuManager: Nút cài đặt đã được click!");
        
        if (settingsUI != null)
        {
            settingsUI.SetActive(true);
            
            // Log chẩn đoán kiểm tra phân cấp UI
            Debug.Log($"LobbyMenuManager: Bật SettingsUI. Active Self: {settingsUI.activeSelf}, Active In Hierarchy (Có thực sự hiển thị): {settingsUI.activeInHierarchy}");
            
            if (!settingsUI.activeInHierarchy)
            {
                Debug.LogWarning("CẢNH BÁO: SettingsUI đã được SetActive(true) nhưng không hiển thị! Có thể do cha của nó (ví dụ: LobbyMenuUI) đang bị ẩn. Hãy kéo SettingsUI ra làm con trực tiếp của Canvas chính (ngang hàng với LobbyMenuUI và GamePlayCanvas).");
            }
        }
        else
        {
            Debug.LogError("LobbyMenuManager: Chưa gán SettingsUI trong Inspector của GameMenuManager!");
        }
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
            StartCoroutine(AnimateButtonEntrance(playButton.GetComponent<RectTransform>(), playButtonOriginalScale));
            yield return new WaitForSeconds(delayBetween);
        }

        if (settingsButton != null)
        {
            StartCoroutine(AnimateButtonEntrance(settingsButton.GetComponent<RectTransform>(), settingsButtonOriginalScale));
            yield return new WaitForSeconds(delayBetween);
        }

        if (exitButton != null)
        {
            StartCoroutine(AnimateButtonEntrance(exitButton.GetComponent<RectTransform>(), exitButtonOriginalScale));
        }
    }

    private IEnumerator AnimateButtonEntrance(RectTransform btnTransform, Vector3 targetScale)
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
            float scaleMultiplier = EaseOutBack(t);
            btnTransform.localScale = new Vector3(targetScale.x * scaleMultiplier, targetScale.y * scaleMultiplier, targetScale.z);
            
            yield return null;
        }

        btnTransform.localScale = targetScale;
    }

    // Hàm EaseOutBack tạo chuyển động phóng to quá đà một chút rồi nảy nhẹ lại
    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
