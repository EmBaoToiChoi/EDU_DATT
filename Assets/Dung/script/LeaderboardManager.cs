using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const string LeaderboardKey = "LeaderboardScores";

    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private TextMeshProUGUI leaderboardText;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    private struct LeaderboardEntry
    {
        public int playNumber;
        public int score;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        AttachListeners();
        RefreshLeaderboardText();
        SetLeaderboardVisibility(false);
    }

    private void OnEnable()
    {
        AttachListeners();
    }

    private void OnDisable()
    {
        DetachListeners();
    }

    private void OnDestroy()
    {
        DetachListeners();
    }

    private void AttachListeners()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(ShowLeaderboard);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideLeaderboard);
        }
    }

    private void DetachListeners()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(ShowLeaderboard);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideLeaderboard);
        }
    }

    public void SubmitScore(int score)
    {
        if (score <= 0)
        {
            return;
        }

        List<LeaderboardEntry> entries = LoadEntries();
        entries.Add(new LeaderboardEntry { playNumber = entries.Count + 1, score = score });
        entries.Sort((a, b) => b.score.CompareTo(a.score));
        
        if (entries.Count > 5)
        {
            entries = entries.GetRange(0, 5);
        }

        SaveEntries(entries);
        RefreshLeaderboardText();
    }

    public void ShowLeaderboard()
    {
        SetLeaderboardVisibility(true);
        RefreshLeaderboardText();
    }

    public void HideLeaderboard()
    {
        SetLeaderboardVisibility(false);
    }

    private void SetLeaderboardVisibility(bool visible)
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(visible);
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(visible);
        }

        if (leaderboardText != null)
        {
            leaderboardText.gameObject.SetActive(visible);
        }
    }

    private void RefreshLeaderboardText()
    {
        if (leaderboardText == null)
        {
            return;
        }

        List<LeaderboardEntry> entries = LoadEntries();
        if (entries.Count == 0)
        {
            leaderboardText.text = "Chưa có điểm nào được lưu.\nHãy chơi một ván để tạo bảng xếp hạng.";
            return;
        }

        string text = string.Empty;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            text += $"#{i + 1}. Lượt {entry.playNumber}: {entry.score} điểm\n";
        }

        leaderboardText.text = text;
    }

    private List<LeaderboardEntry> LoadEntries()
    {
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        string data = PlayerPrefs.GetString(LeaderboardKey, string.Empty);
        
        if (string.IsNullOrWhiteSpace(data))
        {
            return entries;
        }

        string[] parts = data.Split('|');
        foreach (string part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            string[] values = part.Split(':');
            if (values.Length == 2 && int.TryParse(values[0], out int playNumber) && int.TryParse(values[1], out int score))
            {
                entries.Add(new LeaderboardEntry { playNumber = playNumber, score = score });
            }
        }

        return entries;
    }

    private void SaveEntries(List<LeaderboardEntry> entries)
    {
        List<string> values = new List<string>();
        foreach (LeaderboardEntry entry in entries)
        {
            values.Add($"{entry.playNumber}:{entry.score}");
        }

        PlayerPrefs.SetString(LeaderboardKey, string.Join("|", values));
        PlayerPrefs.Save();
    }
}
