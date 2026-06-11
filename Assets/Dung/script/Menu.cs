using UnityEngine;

public class Menu : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The GameObject of the Menu Canvas or Panel to hide when clicking Play.")]
    public GameObject menuCanvas;

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
            // If menuCanvas is not assigned, hide this GameObject itself
            gameObject.SetActive(false);
        }
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


