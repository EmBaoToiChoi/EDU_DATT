using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text promptText;
    public Button[] choiceButtons;

    Action<int> answerCallback;

    public void ShowQuestion(Question q, Action<int> onAnswer)
    {
        if (panel != null) panel.SetActive(true);
        if (promptText != null) promptText.text = q.prompt;
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
        if (panel != null) panel.SetActive(false);
        answerCallback?.Invoke(idx);
    }
}
