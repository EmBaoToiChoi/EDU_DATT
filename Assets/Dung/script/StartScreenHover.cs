using UnityEngine;
using UnityEngine.EventSystems;

public class StartScreenHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Start Screen Visibility")]
    public GameObject startScreen;
    public GameObject targetObjectToShow;
    public bool hideWhileStartScreenActive = true;
    public bool showAfterStartScreenClosed = true;

    [Header("Hover Animation")]
    public Animator hoverAnimator;
    public string hoverTrigger = "Hover";
    public string normalTrigger = "Normal";
    public bool resetOnExit = true;

    private GameObject currentTarget;

    void Awake()
    {
        currentTarget = targetObjectToShow != null ? targetObjectToShow : gameObject;
    }

    void Start()
    {
        UpdateVisibility();
    }

    void Update()
    {
        if (startScreen == null) return;

        if (hideWhileStartScreenActive && currentTarget.activeSelf != !startScreen.activeSelf)
        {
            currentTarget.SetActive(!startScreen.activeSelf);
        }

        if (showAfterStartScreenClosed && !startScreen.activeSelf && !currentTarget.activeSelf)
        {
            currentTarget.SetActive(true);
        }
    }

    void UpdateVisibility()
    {
        if (startScreen == null)
        {
            currentTarget.SetActive(true);
            return;
        }

        if (hideWhileStartScreenActive)
        {
            currentTarget.SetActive(!startScreen.activeSelf);
        }
        else
        {
            currentTarget.SetActive(true);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverAnimator != null)
        {
            hoverAnimator.SetTrigger(hoverTrigger);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverAnimator != null && resetOnExit)
        {
            hoverAnimator.SetTrigger(normalTrigger);
        }
    }
}
