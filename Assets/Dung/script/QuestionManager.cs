using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Question
{
    public string prompt;
    public string[] choices;
    public int correctIndex = 0;
}

public class QuestionManager : MonoBehaviour
{
    public List<Question> questions = new List<Question>();

    System.Random rnd = new System.Random();

    // Nếu Inspector chưa có câu hỏi, tạo ví dụ để thuận tiện thử nghiệm
    void OnValidate()
    {
        if (questions == null) questions = new List<Question>();
        if (questions.Count == 0)
        {
            questions.Add(new Question() { prompt = "Con gì kêu meo meo?", choices = new string[] { "Chó", "Mèo", "Gà" }, correctIndex = 1 });
            questions.Add(new Question() { prompt = "Từ 'apple' nghĩa là gì?", choices = new string[] { "Táo", "Cam", "Cây" }, correctIndex = 0 });
        }
    }

    // Hiển thị câu hỏi ngẫu nhiên, trả về true/false qua callback
    public void ShowRandomQuestion(Action<bool> resultCallback)
    {
        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("No questions assigned. Default to correct.");
            resultCallback?.Invoke(true);
            return;
        }

        int idx = rnd.Next(questions.Count);
        Question q = questions[idx];

        SimpleUI ui = FindObjectOfType<SimpleUI>();
        if (ui != null)
        {
            ui.ShowQuestion(q, (choice) => {
                bool ok = (choice == q.correctIndex);
                resultCallback?.Invoke(ok);
            });
        }
        else
        {
            Debug.LogWarning("SimpleUI not found in scene. Assuming correct.");
            resultCallback?.Invoke(true);
        }
    }
}
