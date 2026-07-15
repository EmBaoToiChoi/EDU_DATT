using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text promptText;
    public TMP_Text timerText;
    public Button[] choiceButtons;

    Action<int> answerCallback;
    float questionTimeLimit;
    float questionTimeRemaining;
    bool questionActive;

    void Update()
    {
        if (!questionActive) return;

        questionTimeRemaining -= Time.deltaTime;
        if (timerText != null)
        {
            timerText.text = $"Thời gian: {Mathf.Ceil(questionTimeRemaining)}s";
        }

        if (questionTimeRemaining <= 0f)
        {
            if (!questionActive) return;
            questionActive = false;
            if (panel != null) panel.SetActive(false);
            answerCallback?.Invoke(-1);
        }
    }

    public void ShowQuestion(Question q, float timeLimit, Action<int> onAnswer)
    {
        if (panel != null) panel.SetActive(true);
        if (promptText != null) promptText.text = q.prompt;
        questionTimeLimit = timeLimit;
        questionTimeRemaining = timeLimit;
        questionActive = true;
        answerCallback = onAnswer;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < q.choices.Length)
            {
                var btn = choiceButtons[i];
                btn.gameObject.SetActive(true);
                var txt = btn.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = q.choices[i];
                int idx = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnChoice(idx));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnChoice(int idx)
    {
        if (!questionActive) return;
        questionActive = false;
        if (panel != null) panel.SetActive(false);
        answerCallback?.Invoke(idx);
    }
}
