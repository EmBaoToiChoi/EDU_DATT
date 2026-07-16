using UnityEngine;

public class DeadEndQuestionTrigger : MonoBehaviour
{
    private GamePlay gamePlay;
    private bool isUsed;

    public void Initialize(GamePlay gp)
    {
        gamePlay = gp;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isUsed) return;
        if (gamePlay == null) gamePlay = FindAnyObjectByType<GamePlay>();
        if (gamePlay == null) return;

        if (other.gameObject == gamePlay.gameObject || other.transform.IsChildOf(gamePlay.transform))
        {
            TriggerQuestion();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isUsed) return;
        if (gamePlay == null) gamePlay = FindAnyObjectByType<GamePlay>();
        if (gamePlay == null) return;

        if (other.gameObject == gamePlay.gameObject || other.transform.IsChildOf(gamePlay.transform))
        {
            TriggerQuestion();
        }
    }

    public void TriggerQuestion()
    {
        if (isUsed) return;
        if (gamePlay == null) return;
        if (gamePlay.waitingForAnswer) return;

        isUsed = true;
        gamePlay.waitingForAnswer = true;
        gamePlay.StopMovement();

        if (gamePlay.questionManager != null)
        {
            gamePlay.questionManager.ShowRandomQuestion((result) =>
            {
                if (result == QuestionResult.Timeout)
                {
                    Debug.Log("Dead-end trigger timed out. Player loses 1 HP.");
                    gamePlay.TakeDamage(1);
                }
                else if (result == QuestionResult.Correct)
                {
                    Debug.Log("Dead-end trigger answered correctly. No damage.");
                }
                else if (result == QuestionResult.Incorrect)
                {
                    Debug.Log("Dead-end trigger answered incorrectly. Player loses 1 HP.");
                    gamePlay.TakeDamage(1);
                }

                if (gameObject != null)
                {
                    Destroy(gameObject);
                }

                if (gamePlay.currentHealth > 0)
                {
                    gamePlay.waitingForAnswer = false;
                }
            });
        }
        else
        {
            Debug.LogWarning("QuestionManager not found. Treating dead-end trigger as correct.");
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
            gamePlay.waitingForAnswer = false;
        }
    }
}
