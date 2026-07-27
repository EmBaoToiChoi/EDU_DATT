using UnityEngine;
using UnityEngine.SceneManagement;

public class ReplayLevel : MonoBehaviour
{
    [Header("Optional: use GamePlay if available")]
    public GamePlay gamePlay;

    [Header("Fallback: reload the current scene if GamePlay is not assigned")]
    public bool reloadSceneWhenNoGamePlay = true;

    public void Replay()
    {
        if (gamePlay != null)
        {
            gamePlay.StartGame();
            return;
        }

        if (reloadSceneWhenNoGamePlay)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }
}
