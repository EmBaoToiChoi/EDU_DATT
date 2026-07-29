using UnityEngine;

public class Menu : MonoBehaviour
{
    [Header("Menu References")]
    [Tooltip("The GameObject of the Menu Canvas or Panel to hide when clicking Play.")]
    public GameObject menuCanvas;

    [Header("In-game UI References")]
    [Tooltip("Level panel root that should be hidden while the menu is visible.")]
    public GameObject levelPanelRoot;
    [Tooltip("Time panel root that should be hidden while the menu is visible.")]
    public GameObject timePanelRoot;
    [Tooltip("Health panel root that should be hidden while the menu is visible.")]
    public GameObject healthPanelRoot;
    [Tooltip("Score panel root that should be hidden while the menu is visible.")]
    public GameObject scorePanelRoot;
    [Tooltip("Additional UI objects that should also be hidden while the menu is visible.")]
    public GameObject[] additionalInfoUIObjects;
    [Tooltip("If enabled, the script will try to find the panel roots by name at runtime when they are not assigned in the Inspector.")]
    public bool autoFindPanelRoots = true;

    private void Reset()
    {
        if (!Application.isPlaying && autoFindPanelRoots)
        {
            TryAutoFindPanelRoots();
        }
    }

    private void Awake()
    {
        if (!Application.isPlaying)
            return;

        if (autoFindPanelRoots)
        {
            TryAutoFindPanelRoots();
        }

        InitializeMenuAndUI();
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            InitializeMenuAndUI();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (menuCanvas == null)
            menuCanvas = gameObject;

        if (menuCanvas.activeInHierarchy)
        {
            if (autoFindPanelRoots)
                TryAutoFindPanelRoots();

            SetInfoUIVisible(false);
        }
    }

    private void InitializeMenuAndUI()
    {
        if (menuCanvas == null)
        {
            menuCanvas = gameObject;
        }

        bool menuIsVisible = menuCanvas.activeSelf;
        SetInfoUIVisible(!menuIsVisible);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (autoFindPanelRoots)
        {
            TryAutoFindPanelRoots();
        }

        SetObjectActive(levelPanelRoot, true);
        SetObjectActive(timePanelRoot, true);
        SetObjectActive(healthPanelRoot, true);
        SetObjectActive(scorePanelRoot, true);

        if (additionalInfoUIObjects != null)
        {
            for (int i = 0; i < additionalInfoUIObjects.Length; i++)
            {
                SetObjectActive(additionalInfoUIObjects[i], true);
            }
        }
    }

    private void TryAutoFindPanelRoots()
    {
        if (levelPanelRoot == null)
            levelPanelRoot = GameObject.Find("LevelPanelRoot");
        if (timePanelRoot == null)
            timePanelRoot = GameObject.Find("TimePanelRoot");
        if (healthPanelRoot == null)
            healthPanelRoot = GameObject.Find("HealthPanelRoot");
        if (scorePanelRoot == null)
            scorePanelRoot = GameObject.Find("ScorePanelRoot");
    }

    private void SetInfoUIVisible(bool visible)
    {
        SetObjectActive(levelPanelRoot, visible);
        SetObjectActive(timePanelRoot, visible);
        SetObjectActive(healthPanelRoot, visible);
        SetObjectActive(scorePanelRoot, visible);

        if (additionalInfoUIObjects != null)
        {
            for (int i = 0; i < additionalInfoUIObjects.Length; i++)
            {
                SetObjectActive(additionalInfoUIObjects[i], visible);
            }
        }
    }

    private void SetObjectActive(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }

    /// <summary>
    /// Hides the main menu to show the game screen.
    /// </summary>
    public void PlayGame()
    {
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        SetInfoUIVisible(true);
    }

    /// <summary>
    /// Exits the game application.
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("Exiting game...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}


