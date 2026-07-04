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

    private bool isMuted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Tải trạng thái tắt/bật âm thanh đã lưu từ trước (0: Bật, 1: Tắt)
            isMuted = PlayerPrefs.GetInt("GameMuted", 0) == 1;
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
        PlayMusic(backgroundMusic);
        ApplyMuteState(); // Áp dụng trạng thái âm thanh ngay khi vào game
    }

    private void OnValidate()
    {
        if (musicSource != null && !isMuted)
        {
            musicSource.volume = volumeBackgroundMusic;
        }
    }

    // --- HÀM BẬT/TẮT TOÀN BỘ ÂM THANH (Sẽ gọi từ Button) ---
    public void ToggleMute()
    {
        isMuted = !isMuted;
        
        // Lưu lại cấu hình vào thiết bị (0: Bật âm, 1: Tắt âm)
        PlayerPrefs.SetInt("GameMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();

        ApplyMuteState();
    }

    // Hàm trả về trạng thái hiện tại để UI (nếu có) thay đổi icon Loa bật/tắt
    public bool IsMuted()
    {
        return isMuted;
    }

    private void ApplyMuteState()
    {
        if (isMuted)
        {
            musicSource.volume = 0f;
            sfxSource.mute = true; // Khóa nguồn phát hiệu ứng âm thanh
        }
        else
        {
            musicSource.volume = volumeBackgroundMusic;
            sfxSource.mute = false; // Mở khóa nguồn phát hiệu ứng âm thanh
        }
    }

    // --- HÀM PHÁT NHẠC NỀN ---
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.volume = isMuted ? 0f : volumeBackgroundMusic; 
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // --- HÀM PHÁT HIỆU ỨNG ÂM THANH ĐỘC LẬP TỪNG LOẠI ---
    public void PlaySFX(AudioClip clip, float customVolume)
    {
        if (clip == null || isMuted) return; // Nếu đang tắt âm thì không phát SFX
        sfxSource.PlayOneShot(clip, customVolume); 
    }

    // --- CÁC HÀM TIỆN ÍCH ĐỂ CODE KHÁC GỌI NGẮN GỌN ---
    public void PlayClick() { PlaySFX(clickSound, volumeClickSound); }
    public void PlayHover() { PlaySFX(hoverSound, volumeHoverSound); }
    public void PlayCorrect() { PlaySFX(correctSound, volumeCorrectSound); }
    public void PlayWrong() { PlaySFX(wrongSound, volumeWrongSound); }
    public void PlayWin() { PlaySFX(winSound, volumeWinSound); }
}