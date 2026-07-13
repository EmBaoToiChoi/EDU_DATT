using UnityEngine;

public class handclick : MonoBehaviour
{
    [Header("Panel xếp hình")]
    [SerializeField] private GameObject targetPanel;

    private bool hasBeenDismissed;

    private void Awake()
    {
        if (targetPanel == null)
        {
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
                targetPanel = uiManager.xepHinhPanel;
        }
    }

    private void Start()
    {
        if (targetPanel != null)
            gameObject.SetActive(targetPanel.activeSelf);
        else
            gameObject.SetActive(false);
    }

    private void Update()
    {
        if (hasBeenDismissed)
            return;

        if (targetPanel != null)
        {
            bool shouldShow = targetPanel.activeSelf;
            if (gameObject.activeSelf != shouldShow)
                gameObject.SetActive(shouldShow);
        }
    }

    public void HideTutorial()
    {
        hasBeenDismissed = true;
        gameObject.SetActive(false);
    }
}
