using UnityEngine;

public class AudioManagers : MonoBehaviour
{
    public static AudioManagers Instance;

    [Header("Audio Sources (Các nguồn phát độc lập)")]
    [SerializeField] private AudioSource musicSource; 
    [SerializeField] private AudioSource sfxSource;   

    [Header("--- ĐIỀU CHỈNH ÂM LƯỢNG TỪNG ÂM THANH (0 = Tắt, 1 = Tối đa) ---")]
    [Range(0f, 1f)] public float volumeBackgroundMusic = 0.5f;
    [Range(0f, 1f)] public float volumeClickSound = 1f;
    [Range(0f, 1f)] public float volumeHoverSound = 0.8f;
    [Range(0f, 1f)] public float volumeCorrectSound = 1f;
    [Range(0f, 1f)] public float volumeWrongSound = 1f;
    [Range(0f, 1f)] public float volumeWinSound = 1f;

    [Header("Audio Clips")]
    public AudioClip backgroundMusic; 
    public AudioClip clickSound;      
    public AudioClip hoverSound;      
    public AudioClip correctSound;    
    public AudioClip wrongSound;      
    public AudioClip winSound;        

    private void Awake()
    {
        // Khởi tạo Singleton để quản lý xuyên suốt các Scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
    }

    private void Start()
    {
        // Kích hoạt phát nhạc nền với âm lượng riêng biệt của nó
        PlayMusic(backgroundMusic);
    }

    // Cập nhật âm lượng thời gian thực ngay khi bạn kéo thanh trượt trên Inspector lúc đang Play game
    private void OnValidate()
    {
        if (musicSource != null)
        {
            musicSource.volume = volumeBackgroundMusic;
        }
    }

    // --- HÀM PHÁT NHẠC NỀN ---
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.volume = volumeBackgroundMusic; // Gán đúng âm lượng được chỉnh của Nhạc nền
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // --- HÀM PHÁT HIỆU ỨNG ÂM THANH ĐỘC LẬP TỪNG LOẠI ---
    // Thay vì PlayOneShot mặc định, hàm này sẽ lấy chính xác mức Volume của từng Clip cụ thể từ Inspector
    public void PlaySFX(AudioClip clip, float customVolume)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, customVolume); // Ép AudioSource phát đúng mức âm lượng thiết lập riêng cho Clip đó
    }

    // --- CÁC HÀM TIỆN ÍCH ĐỂ CODE KHÁC GỌI NGẮN GỌN ---
    public void PlayClick() { PlaySFX(clickSound, volumeClickSound); }
    public void PlayHover() { PlaySFX(hoverSound, volumeHoverSound); }
    public void PlayCorrect() { PlaySFX(correctSound, volumeCorrectSound); }
    public void PlayWrong() { PlaySFX(wrongSound, volumeWrongSound); }
    public void PlayWin() { PlaySFX(winSound, volumeWinSound); }
}