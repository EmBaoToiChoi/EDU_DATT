using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider vfxSlider;

    void Start()
    {
        // Đồng bộ slider với cài đặt hiện tại trong AudioManager
        if (AudioManager.Instance != null)
        {
            if (musicSlider != null)
            {
                // Quy đổi âm lượng (0 -> 1) thành giá trị của Slider (ví dụ 0 -> 100)
                musicSlider.value = AudioManager.Instance.GetMusicVolume() * musicSlider.maxValue;
                musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }

            if (vfxSlider != null)
            {
                vfxSlider.value = AudioManager.Instance.GetSFXVolume() * vfxSlider.maxValue;
                vfxSlider.onValueChanged.AddListener(OnVFXSliderChanged);
            }
        }
        else
        {
            Debug.LogWarning("SettingsManager: Không tìm thấy AudioManager.Instance!");
        }
    }

    private void OnMusicSliderChanged(float value)
    {
        if (AudioManager.Instance != null && musicSlider != null)
        {
            // Quy đổi từ giá trị Slider (0 -> 100) về âm lượng chuẩn (0 -> 1)
            float normalizedVolume = value / musicSlider.maxValue;
            AudioManager.Instance.SetMusicVolume(normalizedVolume);
        }
    }

    private void OnVFXSliderChanged(float value)
    {
        if (AudioManager.Instance != null && vfxSlider != null)
        {
            float normalizedVolume = value / vfxSlider.maxValue;
            AudioManager.Instance.SetSFXVolume(normalizedVolume);
        }
    }
}
