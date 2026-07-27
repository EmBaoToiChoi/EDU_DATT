using UnityEngine;
using UnityEngine.UI;

public class BackgroundMusicController : MonoBehaviour
{
    public static BackgroundMusicController Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.25f;

    [Header("UI Button")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;

    private bool isMusicEnabled = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = musicVolume;
        audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleMusic);
        }

        if (playOnStart)
        {
            SetMusicEnabled(true);
        }
        else
        {
            SetMusicEnabled(false);
        }

        UpdateButtonLabel();
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleMusic);
        }
    }

    public void SetMusicClip(AudioClip clip)
    {
        backgroundMusic = clip;
        ApplyMusicClip();
    }

    public void ToggleMusic()
    {
        SetMusicEnabled(!isMusicEnabled);
    }

    public void SetMusicEnabled(bool enabled)
    {
        isMusicEnabled = enabled;

        if (audioSource == null)
        {
            return;
        }

        if (!isMusicEnabled)
        {
            audioSource.Pause();
        }
        else
        {
            ApplyMusicClip();
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        UpdateButtonLabel();
    }

    private void ApplyMusicClip()
    {
        if (audioSource == null)
        {
            return;
        }

        if (backgroundMusic != null)
        {
            if (audioSource.clip != backgroundMusic)
            {
                audioSource.clip = backgroundMusic;
            }

            audioSource.volume = musicVolume;
        }
    }

    private void UpdateButtonLabel()
    {
        if (buttonImage != null)
        {
            buttonImage.sprite = isMusicEnabled ? musicOnSprite : musicOffSprite;
        }
    }
}
