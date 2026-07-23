using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestionResult
{
    Correct,
    Incorrect,
    Timeout
}

public enum QuestionPresentationMode
{
    ImageGrid,
    CardSwipe
}

[Serializable]
public class Question
{
    [Tooltip("Image shown at top of card")]
    public Sprite cardImage;

    [Tooltip("Vocabulary/text shown in middle of card (e.g. 'dog')")]
    public string prompt;

    [Tooltip("Optional: sprite shown in middle of card instead of text (e.g. an object icon)")]
    public Sprite promptSprite;
    
    [Tooltip("Optional RectTransform anchor describing where to place the prompt image on the card. Drag a UI RectTransform from the scene.")]
    public RectTransform promptAnchor;

    [Tooltip("If true: image and text match. Player should swipe right when the card is correct, left when it is wrong.")]
    public bool isMatch = true;

    [Tooltip("Text of the correct answer to show when the player answers incorrectly.")]
    public string correctAnswerText;

    [Tooltip("Sprite of the correct answer to show when the player answers incorrectly.")]
    public Sprite correctAnswerSprite;

    [Tooltip("Optional animation prefab to show when this card is answered correctly.")]
    public GameObject correctResultAnimationPrefab;

    [Tooltip("Optional animation prefab to show when this card is answered incorrectly.")]
    public GameObject incorrectResultAnimationPrefab;

    [Tooltip("Sprites used for multiple-choice options (includes correct sprite).")]
    public Sprite[] multipleChoiceOptions;

    [Tooltip("Index in multipleChoiceOptions that is the correct answer.")]
    public int correctOptionIndex;
}

[Serializable]
public class CardData
{
    [Tooltip("Image asset for this card")]
    public Sprite cardImage;

    [Tooltip("Correct vocabulary word for this image")]
    public string correctWord;

    [Tooltip("Sprite used as the label/prompt for this image. If set, UI will show this sprite instead of text.")]
    public Sprite labelSprite;

    [Tooltip("Optional RectTransform in the scene to specify prompt position/size/pivot. Drag your desired UI object here.")]
    public RectTransform labelAnchor;

    [Tooltip("Optional per-card animation prefab to show when this card is swiped.")]
    public GameObject cardAnimationPrefab;

    [Tooltip("Optional animation prefab to show when this card is answered correctly.")]
    public GameObject correctResultAnimationPrefab;

    [Tooltip("Optional animation prefab to show when this card is answered incorrectly.")]
    public GameObject incorrectResultAnimationPrefab;
}

public class QuestionManager : MonoBehaviour
{
    [Tooltip("If CardData list is filled, questions are generated randomly from image-word pairs.")]
    public List<CardData> cardData = new List<CardData>();

    [Tooltip("Optional manual questions. Used only when CardData is empty.")]
    public List<Question> questions = new List<Question>();

    public float questionTimeLimit = 8f;

    [Tooltip("Animation object to show when the swipe answer is correct.")]
    public GameObject correctSwipeAnimation;

    [Tooltip("Animation object to show when the swipe answer is incorrect.")]
    public GameObject incorrectSwipeAnimation;

    [Tooltip("Prefab used to display the correct answer when the player answers incorrectly.")]
    public GameObject correctAnswerDisplayPrefab;

    [Tooltip("Single animation object to show for either correct or incorrect swipe. If set, it will be used as a fallback for both.")]
    public GameObject swipeAnimationObject;

    System.Random rnd = new System.Random();

    void OnValidate()
    {
        if (questions == null) questions = new List<Question>();
        if (cardData == null) cardData = new List<CardData>();
    }

    Question CreateRandomQuestionFromCardData()
    {
        if (cardData == null || cardData.Count == 0)
            return null;

        int idx = rnd.Next(cardData.Count);
        CardData selected = cardData[idx];

        bool isMatch = rnd.Next(2) == 0;
        Sprite promptSprite = null;
        string promptText = null;

        if (isMatch)
        {
            promptSprite = selected.labelSprite;
            promptText = selected.correctWord;
        }
        else
        {
            // pick a different card's label as incorrect prompt
            CardData other = GetRandomDifferentCardData(selected);
            if (other != null)
            {
                promptSprite = other.labelSprite;
                promptText = other.correctWord;
            }
            else
            {
                promptSprite = selected.labelSprite;
                promptText = selected.correctWord;
            }
        }

        // Build multiple-choice options: include the correct sprite and up to 3 random other sprites
        var options = new System.Collections.Generic.List<Sprite>();
        Sprite correctSprite = selected.labelSprite != null ? selected.labelSprite : selected.cardImage;
        options.Add(correctSprite);

        // gather candidate sprites from other CardData
        var spriteCandidates = new System.Collections.Generic.List<Sprite>();
        foreach (var c in cardData)
        {
            if (c == selected) continue;
            if (c.labelSprite != null) spriteCandidates.Add(c.labelSprite);
            else if (c.cardImage != null) spriteCandidates.Add(c.cardImage);
        }

        // shuffle and take up to 3
        for (int i = 0; i < 3 && spriteCandidates.Count > 0; i++)
        {
            int pick = rnd.Next(spriteCandidates.Count);
            options.Add(spriteCandidates[pick]);
            spriteCandidates.RemoveAt(pick);
        }

        // If not enough wrong options, duplicate placeholders (will still show something)
        while (options.Count < 4)
        {
            options.Add(correctSprite);
        }

        // Shuffle options and record index of correct one
        for (int i = 0; i < options.Count; i++)
        {
            int j = rnd.Next(i, options.Count);
            var tmp = options[i];
            options[i] = options[j];
            options[j] = tmp;
        }

        int correctIndex = 0;
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] == correctSprite)
            {
                correctIndex = i;
                break;
            }
        }

        return new Question
        {
            cardImage = selected.cardImage,
            prompt = promptText,
            promptSprite = promptSprite,
            promptAnchor = selected.labelAnchor,
            isMatch = isMatch,
            correctAnswerText = selected.correctWord,
            correctAnswerSprite = correctSprite,
            correctResultAnimationPrefab = selected.correctResultAnimationPrefab,
            incorrectResultAnimationPrefab = selected.incorrectResultAnimationPrefab,
            multipleChoiceOptions = options.ToArray(),
            correctOptionIndex = correctIndex
        };
    }

    CardData GetRandomDifferentCardData(CardData exclude)
    {
        if (cardData == null || cardData.Count <= 1) return null;
        var candidates = new List<CardData>();
        foreach (var c in cardData)
        {
            if (c != exclude) candidates.Add(c);
        }
        if (candidates.Count == 0) return null;
        return candidates[rnd.Next(candidates.Count)];
    }

    string GetRandomIncorrectWord(string correctWord)
    {
        if (cardData == null || cardData.Count <= 1)
            return correctWord;

        var candidates = new System.Collections.Generic.List<string>();
        foreach (var pair in cardData)
        {
            if (!string.IsNullOrWhiteSpace(pair.correctWord) && pair.correctWord != correctWord)
            {
                candidates.Add(pair.correctWord);
            }
        }

        if (candidates.Count == 0)
            return correctWord;

        return candidates[rnd.Next(candidates.Count)];
    }

    // Present a random card-swipe question (always card mode)
    public void ShowRandomQuestion(Action<QuestionResult> resultCallback)
    {
        Question q = null;

        if (cardData != null && cardData.Count > 0)
        {
            q = CreateRandomQuestionFromCardData();
        }
        else if (questions != null && questions.Count > 0)
        {
            int idx = rnd.Next(questions.Count);
            q = questions[idx];
        }
        else
        {
            Debug.LogWarning("QuestionManager: no questions assigned. Defaulting to correct.");
            resultCallback?.Invoke(QuestionResult.Correct);
            return;
        }

        SimpleUI ui = UnityEngine.Object.FindAnyObjectByType<SimpleUI>();
        if (ui == null)
        {
            GameObject uiObject = new GameObject("SimpleUI_Root");
            ui = uiObject.AddComponent<SimpleUI>();
        }

        if (ui != null)
        {
            ui.correctAnimationObject = correctSwipeAnimation;
            ui.incorrectAnimationObject = incorrectSwipeAnimation;
            ui.correctAnswerDisplayPrefab = correctAnswerDisplayPrefab;
            ui.swipeAnimationObject = swipeAnimationObject;
        }

        ui.ShowQuestion(q, questionTimeLimit, resultCallback, QuestionPresentationMode.CardSwipe);
    }
}
