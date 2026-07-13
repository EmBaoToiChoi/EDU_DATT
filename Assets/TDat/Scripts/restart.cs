using UnityEngine;
using UnityEngine.SceneManagement;

public class restart : MonoBehaviour
{
    [Header("Reload current scene")]
    [SerializeField] private bool reloadOnClick = true;

    private void OnMouseDown()
    {
        if (reloadOnClick)
            ReloadCurrentScene();
    }

    public void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
