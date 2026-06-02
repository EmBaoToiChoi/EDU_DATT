using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip clickSound;

    [Header("Default Volumes")]
    [Range(0f, 1f)] [SerializeField] private float defaultMusicVolume = 0.15f; // Giảm nhạc nền mặc định xuống 15% cho dịu hơn
    [Range(0f, 1f)] [SerializeField] private float defaultSFXVolume = 0.8f;    // Âm lượng click mặc định (80%)

    private const string BGM_VOL_KEY = "BGMVolume";
    private const string SFX_VOL_KEY = "SFXVolume";

    void Awake()
    {
        // Kiểm tra xem có gán nhầm trùng AudioSource không
        if (musicSource != null && sfxSource != null && musicSource == sfxSource)
        {
            Debug.LogError("WARNING: Cả Music Source và Sfx Source của AudioManager đang trỏ chung vào 1 AudioSource! Hãy tạo và gán 2 AudioSource khác nhau nhé.");
        }

        // Cấu hình Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        // Tạo các AudioSource tự động nếu chưa được gán trong Inspector
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // Tải âm lượng từ PlayerPrefs (mặc định từ biến cấu hình nếu chưa lưu lần nào)
        float savedBGM = PlayerPrefs.GetFloat(BGM_VOL_KEY, defaultMusicVolume);
        float savedSFX = PlayerPrefs.GetFloat(SFX_VOL_KEY, defaultSFXVolume);

        SetMusicVolume(savedBGM);
        SetSFXVolume(savedSFX);

        // Tự động phát nhạc nền nếu có clip được gán
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void PlayClickSound()
    {
        if (sfxSource != null && clickSound != null)
        {
            sfxSource.PlayOneShot(clickSound);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            // Áp dụng bình phương để tạo cảm nhận âm lượng tuyến tính theo tai người (logarithmic curve)
            musicSource.volume = volume * volume;
        }
        PlayerPrefs.SetFloat(BGM_VOL_KEY, volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            // Áp dụng tương tự cho SFX
            sfxSource.volume = volume * volume;
        }
        PlayerPrefs.SetFloat(SFX_VOL_KEY, volume);
        PlayerPrefs.Save();
    }

    public float GetMusicVolume()
    {
        // Trả về giá trị tuyến tính gốc để Slider hiển thị chính xác
        return PlayerPrefs.GetFloat(BGM_VOL_KEY, defaultMusicVolume);
    }

    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(SFX_VOL_KEY, defaultSFXVolume);
    }
}
